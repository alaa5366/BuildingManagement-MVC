using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Extracts payment fields from OCR text produced by bank, wallet and instant-transfer receipts.
/// Values remain guesses: this parser never authorizes or posts a payment.
/// </summary>
public class TransactionParser
{
    private static readonly Regex AmountLabelRegex = new(
        @"(?:amount|transfer\s+amount|payment\s+amount|total\s+amount|paid\s+amount|المبلغ(?:\s+المحول)?|قيمة\s+التحويل|مبلغ\s+التحويل|إجمالي\s+المبلغ|اجمالي\s+المبلغ)\s*[:：\-]?\s*(?<amount>[0-9٠-٩۰-۹][0-9٠-٩۰-۹\s,٬.٫]*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CurrencyAmountRegex = new(
        @"(?<amount>[0-9٠-٩۰-۹](?:[0-9٠-٩۰-۹\s,٬.٫]*[0-9٠-٩۰-۹])?)(?:\s*)(?<![\p{L}])(?:EGP|LE\b|L\.E\b|ج\.?م\.?|جنيه(?:\s+مصري)?|Egyptian\s+Pounds?)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex AmountCurrencyPrefixRegex = new(
        @"(?<![\p{L}])(?:EGP|LE\b|L\.E\b|ج\.?م\.?|جنيه(?:\s+مصري)?|Egyptian\s+Pounds?)\s*[:：\-]?\s*(?<amount>[0-9٠-٩۰-۹][0-9٠-٩۰-۹\s,٬.٫]*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex ReferenceLabelRegex = new(
        @"(?:reference(?:\s*(?:no\.?|number|id))?|ref\.?\s*(?:no\.?|number)?|transaction\s*(?:id|no\.?|number)|txn\s*(?:id|no\.?)|payment\s*id|رقم\s*(?:العملية|المرجع|التحويل|الإيصال)|رقم\s*العمليه|مرجع\s*العملية)\s*[:：#\-]?\s*(?<reference>[A-Z0-9][A-Z0-9\-/]{5,})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DateRegex = new(
        @"(?<date>\b(?:[0-3]?\d[/-][01]?\d[/-](?:20)?\d{2}|(?:20\d{2})[/-][01]?\d[/-][0-3]?\d|[0-3]?\d\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+20\d{2}(?:\s+\d{1,2}:\d{2}\s*(?:AM|PM)?)?)\b)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex SuccessRegex = new(
        @"transaction\s+successful|payment\s+successful|transfer\s+successful|successfully\s+(?:sent|transferred|paid)|\bsuccess\b|\bcompleted\b|\bpaid\b|\bapproved\b|تم\s+(?:التحويل|الدفع|تنفيذ\s+العملية)\s+بنجاح|تمت\s+(?:العملية|عملية\s+التحويل)\s+بنجاح|ناجح(?:ة)?|مكتمل(?:ة)?",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex FailureRegex = new(
        @"transaction\s+failed|payment\s+failed|transfer\s+failed|\bfailed\b|\bdeclined\b|\brejected\b|\bunsuccessful\b|\bnot\s+completed\b|\bnot\s+successful\b|فشل(?:ت)?|غير\s+ناجح(?:ة)?|مرفوض(?:ة)?|تعذر\s+(?:التحويل|الدفع|إتمام)|لم\s+تتم\s+(?:العملية|عملية\s+التحويل)|لم\s+يكتمل",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex PendingRegex = new(
        @"\bpending\b|\bprocessing\b|\bin\s+progress\b|\binitiated\b|under\s+review|قيد\s+التنفيذ|جار(?:ي|ية)\s+التنفيذ|تحت\s+المعالجة|معلقة|قيد\s+المراجعة",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Parses OCR text without treating an uncertain status as a successful payment.</summary>
    public TransactionData? Parse(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return null;

        var normalized = NormalizeDigits(ocrText).Replace("\r", "");
        var data = new TransactionData { RawText = ocrText };

        var amountMatch = AmountLabelRegex.Match(normalized);
        if (!amountMatch.Success)
            amountMatch = CurrencyAmountRegex.Match(normalized);
        if (!amountMatch.Success)
            amountMatch = AmountCurrencyPrefixRegex.Match(normalized);
        if (!amountMatch.Success)
        {
            // Decimal-only fallback is intentionally restricted. Unlabelled integers
            // are too easily confused with dates, references and account numbers.
            amountMatch = Regex.Match(normalized,
                @"(?<![\d])(?<amount>\d{1,7}[.,٫]\d{1,2})(?![\d])",
                RegexOptions.CultureInvariant);
        }

        if (amountMatch.Success && TryParseAmount(amountMatch.Groups["amount"].Value, out var amount))
            data.Amount = amount;

        var referenceMatch = ReferenceLabelRegex.Match(normalized);
        if (referenceMatch.Success)
            data.Reference = referenceMatch.Groups["reference"].Value.Trim();
        else
        {
            var fallback = Regex.Match(normalized, @"(?<!\d)(\d{10,18})(?!\d)");
            if (fallback.Success && fallback.Groups[1].Value != data.Amount.ToString(CultureInfo.InvariantCulture))
                data.Reference = fallback.Groups[1].Value;
        }

        var dateMatch = DateRegex.Match(normalized);
        if (dateMatch.Success)
            data.Date = dateMatch.Groups["date"].Value.Trim();

        // Failure has precedence: a receipt may contain both a generic "success"
        // heading and a later explicit failure/reversal status.
        data.IsFailed = FailureRegex.IsMatch(normalized);
        data.IsPending = !data.IsFailed && PendingRegex.IsMatch(normalized);
        data.IsSuccessful = !data.IsFailed && !data.IsPending && SuccessRegex.IsMatch(normalized);
        data.Status = data.IsFailed ? "FAILED"
            : data.IsPending ? "PENDING"
            : data.IsSuccessful ? "SUCCESS"
            : "UNKNOWN";

        var accountMatch = Regex.Match(normalized, @"(?<![A-Z0-9])(80202\d{10,14})(?!\d)", RegexOptions.IgnoreCase);
        if (accountMatch.Success)
            data.RecipientAccount = accountMatch.Groups[1].Value;
        else
        {
            var ibanMatch = Regex.Match(normalized, @"\bEG\d{2}([0-9]{14,30})\b", RegexOptions.IgnoreCase);
            if (ibanMatch.Success)
                data.RecipientAccount = ibanMatch.Groups[1].Value;
        }

        var fromMatch = Regex.Match(normalized, @"From\s*:?\s*([^\n]+)", RegexOptions.IgnoreCase);
        if (fromMatch.Success)
            data.From = fromMatch.Groups[1].Value.Trim();

        return data;
    }

    private static string NormalizeDigits(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(character switch
            {
                >= '٠' and <= '٩' => (char)('0' + character - '٠'),
                >= '۰' and <= '۹' => (char)('0' + character - '۰'),
                '٫' => '.',
                '٬' => ',',
                '：' => ':',
                _ => character
            });
        }
        return builder.ToString();
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        amount = 0m;
        var text = Regex.Replace(value.Trim(), @"\s+", "");
        if (text.Length == 0)
            return false;

        var lastComma = text.LastIndexOf(',');
        var lastDot = text.LastIndexOf('.');
        var decimalSeparator = Math.Max(lastComma, lastDot);
        var hasDecimal = decimalSeparator >= 0 && text.Length - decimalSeparator - 1 is 1 or 2;

        if (hasDecimal)
        {
            var whole = Regex.Replace(text[..decimalSeparator], @"[,\.]", "");
            var fraction = Regex.Replace(text[(decimalSeparator + 1)..], @"[,\.]", "");
            text = whole + "." + fraction;
        }
        else
        {
            text = Regex.Replace(text, @"[,\.]", "");
        }

        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)
            && amount > 0m;
    }
}

/// <summary>Parsed fields of a transfer receipt. These are OCR suggestions, not payment authorization.</summary>
public class TransactionData
{
    public decimal Amount { get; set; }
    public string Reference { get; set; } = "";
    public string Date { get; set; } = "";
    public bool IsSuccessful { get; set; }
    public bool IsFailed { get; set; }
    public bool IsPending { get; set; }
    public string Status { get; set; } = "UNKNOWN";
    public string RecipientAccount { get; set; } = "";
    public string From { get; set; } = "";
    public string RawText { get; set; } = "";
    public bool IsValid => Amount > 0 && !string.IsNullOrEmpty(Reference);
}
