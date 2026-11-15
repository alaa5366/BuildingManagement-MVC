using System.Globalization;
using System.Text.RegularExpressions;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Parses OCR text of a transfer receipt into amount, reference, date, account and sender.
/// </summary>
public class TransactionParser
{
    /// <summary>
    /// Parses OCR text of a transfer receipt. Returns <c>null</c> for empty text; otherwise returns the parsed fields (amount is 0 when none was found).
    /// </summary>
    public TransactionData? Parse(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return null;

        var data = new TransactionData { RawText = ocrText };

        // ========== المبلغ ==========
        // يدور على أول رقم فيه فاصلة عشرية (1,678) أو رقم كبير (1678)
        var amountMatch = Regex.Match(ocrText, @"(\d{1,3}(?:,\d{3})+(?:\.\d{1,2})?)");

        if (!amountMatch.Success)
        {
            // fallback 1: رقم بكسر عشري (250.75)
            amountMatch = Regex.Match(ocrText, @"\b(\d{3,6}\.\d{1,2})\b");
        }

        if (!amountMatch.Success)
        {
            // fallback 2: رقم صحيح (250) — بس لو جنب كلمة مبلغ/عملة،
            // عشان مانلقطش سنة (2026) أو رقم عشوائي من الصورة على إنه المبلغ.
            // لو مفيش تطابق المبلغ يفضل 0 والمستخدم يكتبه بإيده.
            amountMatch = Regex.Match(ocrText,
                @"(?<![\p{L}\d])(?:Amount|المبلغ|مبلغ|EGP|LE|جنيه|ج\.م)(?![\p{L}])\s*[:\-]?\s*(?<n>\d{3,6})(?![\d.,])" +
                @"|(?<![\d.,])(?<n>\d{3,6})(?![\d.,])\s*(?:EGP|LE|جنيه|ج\.م)(?![\p{L}])",
                RegexOptions.IgnoreCase);
        }

        if (amountMatch.Success)
        {
            var amountText = amountMatch.Groups["n"].Success ? amountMatch.Groups["n"].Value : amountMatch.Groups[1].Value;
            var cleaned = amountText.Replace(",", "");
            if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                data.Amount = amount;
        }

        // ========== Reference ==========
        // 1) يدور على كلمة Reference
        var refMatch = Regex.Match(ocrText, @"Reference\s*[:\s]*(\d{6,})",
            RegexOptions.IgnoreCase);

        // 2) لو مش لاقي، يدور على رقم 12 خانة (زي 707714830636)
        if (!refMatch.Success)
        {
            refMatch = Regex.Match(ocrText, @"\b(\d{12})\b");
        }

        if (refMatch.Success)
            data.Reference = refMatch.Groups[1].Value;

        // ========== التاريخ ==========
        // يدور على أي نمط: "01 Sep 2026 09:23 PM" حتى بدون كلمة Date
        var dateMatch = Regex.Match(ocrText,
            @"(\d{1,2}\s+(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{4}\s+\d{1,2}:\d{2}\s*[APap][Mm])",
            RegexOptions.IgnoreCase);

        if (dateMatch.Success)
            data.Date = dateMatch.Groups[1].Value.Trim();

        // ========== الحالة ==========
        data.IsSuccessful = Regex.IsMatch(ocrText, @"Transaction\s+Successful",
            RegexOptions.IgnoreCase);
        data.IsFailed = Regex.IsMatch(ocrText, @"Transaction\s+Details",
            RegexOptions.IgnoreCase) && !data.IsSuccessful;

        // ========== رقم الحساب المستقبل ==========
        // يدور على رقم بنك مصر: 8020220000001299
        var accountMatch = Regex.Match(ocrText, @"(80202\d{10,14})");
        if (accountMatch.Success)
            data.RecipientAccount = accountMatch.Groups[1].Value;
        else
        {
            // fallback: EG + أرقام
            var egMatch = Regex.Match(ocrText, @"EG\d{2}(\d{14,30})");
            if (egMatch.Success)
                data.RecipientAccount = egMatch.Groups[1].Value;
        }

        // ========== من (اسم/إيميل المُحوِّل) ==========
        var fromMatch = Regex.Match(ocrText, @"From\s*\n?\s*([^\n]+)", RegexOptions.IgnoreCase);
        if (fromMatch.Success)
            data.From = fromMatch.Groups[1].Value.Trim();

        return data;
    }
}

/// <summary>
/// Parsed fields of a transfer receipt.
/// </summary>
public class TransactionData
{
    /// <summary>
    /// Gets or sets the amount.
    /// </summary>
    public decimal Amount { get; set; }
    /// <summary>
    /// Gets or sets the reference.
    /// </summary>
    public string Reference { get; set; } = "";
    /// <summary>
    /// Gets or sets the date.
    /// </summary>
    public string Date { get; set; } = "";
    /// <summary>
    /// Gets or sets a value indicating whether it is successful.
    /// </summary>
    public bool IsSuccessful { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether it is failed.
    /// </summary>
    public bool IsFailed { get; set; }
    /// <summary>
    /// Gets or sets the recipient account.
    /// </summary>
    public string RecipientAccount { get; set; } = "";
    /// <summary>
    /// Gets or sets the from.
    /// </summary>
    public string From { get; set; } = "";
    /// <summary>
    /// Gets or sets the raw text.
    /// </summary>
    public string RawText { get; set; } = "";

    /// <summary>
    /// Gets a value indicating whether it is valid.
    /// </summary>
    public bool IsValid => Amount > 0 && !string.IsNullOrEmpty(Reference);
}