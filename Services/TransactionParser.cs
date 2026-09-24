using System.Globalization;
using System.Text.RegularExpressions;

namespace BuildingManagementMvc.Services;

public class TransactionParser
{
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
            // لو مفيش فاصلة، يدور على رقم من 3 لـ 6 أرقام
            amountMatch = Regex.Match(ocrText, @"\b(\d{3,6})\b");
        }

        if (amountMatch.Success)
        {
            var cleaned = amountMatch.Groups[1].Value.Replace(",", "");
            if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
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

public class TransactionData
{
    public double Amount { get; set; }
    public string Reference { get; set; } = "";
    public string Date { get; set; } = "";
    public bool IsSuccessful { get; set; }
    public bool IsFailed { get; set; }
    public string RecipientAccount { get; set; } = "";
    public string From { get; set; } = "";
    public string RawText { get; set; } = "";

    public bool IsValid => Amount > 0 && !string.IsNullOrEmpty(Reference);
}