using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace BuildingManagementMvc.Services;

public class ExcelImportService
{
    private readonly ExcelTemplateService _templateService;
    private readonly ILogger<ExcelImportService> _logger;
    private const string TemplateId = "tpl_v2";

    public ExcelImportService(ExcelTemplateService templateService, ILogger<ExcelImportService> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    public ImportValidationResult ValidateFile(Stream fileStream)
    {
        try
        {
            using var workbook = new XLWorkbook(fileStream);

            // 1. تحقق من وجود _meta
            if (!workbook.Worksheets.Contains("_meta"))
                return ImportValidationResult.Fail("الملف ده مش من النظام. حمّل القالب من التطبيق.");

            var metaSheet = workbook.Worksheet("_meta");

            var generatedAtStr = metaSheet.Cell(2, 2).Value.ToString();
            var expiresAtStr = metaSheet.Cell(3, 2).Value.ToString();
            var templateId = metaSheet.Cell(4, 2).Value.ToString();
            var buildingId = metaSheet.Cell(5, 2).Value.ToString();
            var issuedBy = metaSheet.Cell(6, 2).Value.ToString();
            var signature = metaSheet.Cell(7, 2).Value.ToString();

            Console.WriteLine($"[ExcelImport] GeneratedAt: '{generatedAtStr}'");
            Console.WriteLine($"[ExcelImport] ExpiresAt: '{expiresAtStr}'");
            Console.WriteLine($"[ExcelImport] BuildingId: '{buildingId}'");
            Console.WriteLine($"[ExcelImport] Signature from file: '{signature}'");

            // 2. تحقق من الـ TemplateId
            if (templateId != TemplateId)
                return ImportValidationResult.Fail("إصدار القالب قديم. حمّل نسخة جديدة.");

            // 3. تحقق من صحة التواريخ
            if (!DateTime.TryParse(expiresAtStr, out var expiresAt))
                return ImportValidationResult.Fail("الملف تالف.");

            // 4. تحقق من انتهاء الصلاحية
            if (DateTime.UtcNow > expiresAt)
            {
                var days = (DateTime.UtcNow - expiresAt).Days;
                return ImportValidationResult.Fail($"⚠️ القالب ده انتهت صلاحيته من {days} يوم. حمّل نسخة جديدة.");
            }

            // 5. تحقق من الـ Signature (باستخدام النصوص الخام)
            var expectedSignature = _templateService.ComputeSignatureRaw(generatedAtStr, expiresAtStr, buildingId);
            Console.WriteLine($"[ExcelImport] Expected Signature: '{expectedSignature}'");

            if (signature != expectedSignature)
                return ImportValidationResult.Fail("الملف اتعُدِّل أو مش أصلي. حمّل نسخة جديدة.");

            // 6. تحقق من الـ Sheets المطلوبة
            var requiredSheets = new[] { "Building Info", "Floors & Apartments" };
            foreach (var sheet in requiredSheets)
            {
                if (!workbook.Worksheets.Contains(sheet))
                    return ImportValidationResult.Fail($"ورقة '{sheet}' مفقودة");
            }

            // 7. استخرج البيانات
            var data = ExtractData(workbook);

            Console.WriteLine($"[ExcelImport] Name: '{data.Name}'");
            Console.WriteLine($"[ExcelImport] BuildingNumber: '{data.BuildingNumber}'");
            Console.WriteLine($"[ExcelImport] AdminPin: '{data.AdminPin}'");
            Console.WriteLine($"[ExcelImport] AdminWhatsapp: '{data.AdminWhatsapp}'");
            Console.WriteLine($"[ExcelImport] Apartments count: {data.Apartments.Count}");

            if (string.IsNullOrWhiteSpace(data.Name))
                return ImportValidationResult.Fail("اسم العمارة مطلوب في ورقة Building Info.");

            if (data.Apartments.Count == 0)
                return ImportValidationResult.Fail("مفيش شقق في الملف. املأ البيانات من الصف 6.");

            return ImportValidationResult.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExcelImport] Validation failed");
            return ImportValidationResult.Fail($"خطأ في قراءة الملف: {ex.Message}");
        }
    }

    private BuildingImportData ExtractData(XLWorkbook workbook)
    {
        var data = new BuildingImportData();

        // ============ Building Info ============
        var infoSheet = workbook.Worksheet("Building Info");
        data.Name = GetValue(infoSheet, 2, 2);
        data.BuildingNumber = GetValue(infoSheet, 3, 2);
        data.AdminPin = GetValue(infoSheet, 4, 2);
        data.AdminWhatsapp = GetValue(infoSheet, 5, 2);
        data.LogoUrl = GetValue(infoSheet, 6, 2);

        // ============ Floors & Apartments ============
        var aptsSheet = workbook.Worksheet("Floors & Apartments");

        foreach (var row in aptsSheet.RowsUsed().Skip(1))
        {
            var notes = row.Cell(11).Value.ToString() ?? "";
            var floorLabel = row.Cell(1).Value.ToString() ?? "";

            // تجاهل الصفوف التوضيحية والأمثلة
            if (notes.Contains("مثال") || notes.Contains("توضيحي") ||
                floorLabel.Contains("مثال") || floorLabel.Contains("توضيحي") ||
                floorLabel.Contains("الصفوف من"))
                continue;

            if (string.IsNullOrWhiteSpace(floorLabel))
                continue;

            if (!int.TryParse(row.Cell(3).Value.ToString(), out var aptNumber) || aptNumber <= 0)
                continue;

            // تحقق من PIN
            var pin = row.Cell(6).Value.ToString() ?? "";
            if (!Regex.IsMatch(pin, @"^\d{4}$"))
            {
                _logger.LogWarning($"[ExcelImport] صف {row.RowNumber()}: PIN غير صحيح: '{pin}'");
                continue;
            }

            // تحقق من WhatsApp
            var whatsapp = row.Cell(5).Value.ToString() ?? "";
            if (!Regex.IsMatch(whatsapp, @"^\+?[\d\s\-]{7,20}$"))
            {
                _logger.LogWarning($"[ExcelImport] صف {row.RowNumber()}: رقم واتساب غير صالح: '{whatsapp}'");
                continue;
            }

            var apt = new ApartmentImportData
            {
                FloorLabel = floorLabel,
                FloorOrder = int.TryParse(row.Cell(2).Value.ToString(), out var order) ? order : 0,
                AptNumber = aptNumber,
                Owner = row.Cell(4).Value.ToString() ?? "",
                WhatsApp = whatsapp,
                Pin = pin,
                MonthlyFee = double.TryParse(row.Cell(7).Value.ToString(), out var fee) ? fee : 0,
                AptLabel = row.Cell(8).Value.ToString() ?? "",
                Status = row.Cell(9).Value.ToString() ?? "open",
                ClosedReason = row.Cell(10).Value.ToString() ?? "",
                Notes = notes
            };

            data.Apartments.Add(apt);
        }

        return data;
    }

    private string GetValue(IXLWorksheet sheet, int row, int col)
    {
        return sheet.Cell(row, col).Value.ToString() ?? "";
    }
}

public class ImportValidationResult
{
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
    public BuildingImportData? Data { get; set; }

    public static ImportValidationResult Success(BuildingImportData data) =>
        new() { IsSuccess = true, Data = data };

    public static ImportValidationResult Fail(string error) =>
        new() { IsSuccess = false, Error = error };
}

public class BuildingImportData
{
    public string Name { get; set; } = "";
    public string BuildingNumber { get; set; } = "";
    public string AdminPin { get; set; } = "";
    public string AdminWhatsapp { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public List<ApartmentImportData> Apartments { get; set; } = new();
}

public class ApartmentImportData
{
    public string FloorLabel { get; set; } = "";
    public int FloorOrder { get; set; }
    public int AptNumber { get; set; }
    public string Owner { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Pin { get; set; } = "";
    public double MonthlyFee { get; set; }
    public string AptLabel { get; set; } = "";
    public string Status { get; set; } = "open";
    public string ClosedReason { get; set; } = "";
    public string Notes { get; set; } = "";
}