using System.Text.RegularExpressions;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class ExcelImportService
{
    private readonly ExcelTemplateService _templateService;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<ExcelImportService> _logger;
    private const string TemplateId = "tpl_v3";

    public ExcelImportService(
        ExcelTemplateService templateService,
        IDbContextFactory<AppDbContext> sqlFactory,
        ILogger<ExcelImportService> logger)
    {
        _templateService = templateService;
        _sqlFactory = sqlFactory;
        _logger = logger;
    }

    public ImportValidationResult ValidateFile(Stream fileStream)
    {
        try
        {
            using var workbook = new XLWorkbook(fileStream);

            if (!workbook.Worksheets.Contains("_meta"))
                return ImportValidationResult.Fail(Loc.T("This_File_Is_Not_From_The"));

            var metaSheet = workbook.Worksheet("_meta");
            var generatedAtStr = metaSheet.Cell(2, 2).Value.ToString();
            var expiresAtStr = metaSheet.Cell(3, 2).Value.ToString();
            var templateId = metaSheet.Cell(4, 2).Value.ToString();
            var buildingId = metaSheet.Cell(5, 2).Value.ToString();
            var signature = metaSheet.Cell(7, 2).Value.ToString();

            if (templateId != TemplateId)
                return ImportValidationResult.Fail(Loc.T("The_Template_Version_Is_Outdated_Download"));

            if (!DateTime.TryParse(expiresAtStr, out var expiresAt))
                return ImportValidationResult.Fail(Loc.T("The_File_Is_Corrupted"));

            if (DateTime.UtcNow > expiresAt)
            {
                var days = (DateTime.UtcNow - expiresAt).Days;
                return ImportValidationResult.Fail(Loc.T("This_Template_Expired_N_Days_Ago", days));
            }

            var expectedSignature = _templateService.ComputeSignatureRaw(generatedAtStr, expiresAtStr, buildingId);
            if (signature != expectedSignature)
                return ImportValidationResult.Fail(Loc.T("The_File_Was_Modified_Or_Is"));

            var requiredSheets = new[] { "Building Info", "Floors & Apartments" };
            foreach (var sheet in requiredSheets)
            {
                if (!workbook.Worksheets.Contains(sheet))
                    return ImportValidationResult.Fail(Loc.T("Sheet_N_Is_Missing", sheet));
            }

            var data = ExtractData(workbook);

            if (string.IsNullOrWhiteSpace(data.Name))
                return ImportValidationResult.Fail(Loc.T("Building_Name_Is_Required_In_The"));

            if (data.Apartments.Count == 0)
                return ImportValidationResult.Fail(Loc.T("There_Are_No_Apartments_In_The"));

            return ImportValidationResult.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExcelImport] Validation failed");
            return ImportValidationResult.Fail(Loc.T("Error_Reading_The_File_N", ex.Message));
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

        if (data.BuildingNumber == "(تلقائي)" || string.IsNullOrWhiteSpace(data.BuildingNumber))
            data.BuildingNumber = "";

        // ============ Floors & Apartments ============
        var aptsSheet = workbook.Worksheet("Floors & Apartments");

        int globalAptNumber = 1;

        foreach (var row in aptsSheet.RowsUsed().Skip(5))
        {
            var floorLabel = row.Cell(1).Value.ToString() ?? "";
            var owner = row.Cell(4).Value.ToString() ?? "";
            var whatsapp = row.Cell(5).Value.ToString() ?? "";
            var pin = row.Cell(6).Value.ToString() ?? "";
            var monthlyFeeStr = row.Cell(7).Value.ToString() ?? "";
            var aptLabel = row.Cell(8).Value.ToString() ?? "";
            var status = row.Cell(9).Value.ToString() ?? "open";
            var isDisabledStr = row.Cell(10).Value.ToString() ?? "لا";
            var closedReason = row.Cell(11).Value.ToString() ?? "";
            var notes = row.Cell(12).Value.ToString() ?? "";

            // تجاهل الصفوف التوضيحية
            if (floorLabel.Contains("الصفوف من") || floorLabel.Contains("توضيح"))
                continue;

            if (string.IsNullOrWhiteSpace(floorLabel))
                continue;

            // ✅ Floor Order — استنتجها من Floor Label (حتى لو الـ Formula مشتغلتش)
            int floorOrder = MapFloorLabelToOrder(floorLabel);

            // ✅ Apt Number — عدّاد تصاعدي
            int aptNumber = globalAptNumber++;

            if (string.IsNullOrWhiteSpace(whatsapp))
                whatsapp = "0";

            if (string.IsNullOrWhiteSpace(pin))
                pin = "0000";

            if (pin.Length != 4)
                pin = pin.PadRight(4, '0').Substring(0, 4);

            double monthlyFee = 0;
            if (double.TryParse(monthlyFeeStr, out var mf))
                monthlyFee = mf;

            bool isDisabled = isDisabledStr == "نعم"
                || isDisabledStr.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || isDisabledStr.Equals("true", StringComparison.OrdinalIgnoreCase);

            var apt = new ApartmentImportData
            {
                FloorLabel = floorLabel,
                FloorOrder = floorOrder,
                AptNumber = aptNumber,
                Owner = string.IsNullOrWhiteSpace(owner) ? "0" : owner,
                WhatsApp = whatsapp,
                Pin = pin,
                MonthlyFee = monthlyFee,
                AptLabel = string.IsNullOrWhiteSpace(aptLabel) ? "" : aptLabel,
                Status = string.IsNullOrWhiteSpace(status) ? "open" : status,
                IsDisabled = isDisabled,
                ClosedReason = closedReason,
                Notes = notes
            };

            data.Apartments.Add(apt);
        }

        return data;
    }

    private static int MapFloorLabelToOrder(string floorLabel)
    {
        if (string.IsNullOrWhiteSpace(floorLabel)) return 0;

        return floorLabel.Trim() switch
        {
            "البدروم" => -1,
            "الدور الأرضي" => 0,
            "الدور الأول" => 1,
            "الدور الثاني" => 2,
            "الدور الثالث" => 3,
            "الدور الرابع" => 4,
            "الدور الخامس" => 5,
            "الدور السادس" => 6,
            "الدور السابع" => 7,
            "الدور الثامن" => 8,
            "الدور التاسع" => 9,
            "الدور العاشر" => 10,
            "الدور الحادي عشر" => 11,
            "الدور الثاني عشر" => 12,
            "الدور الثالث عشر" => 13,
            "الدور الرابع عشر" => 14,
            "الدور الخامس عشر" => 15,
            "الروف" => 99,
            _ => 0
        };
    }

    private string GetValue(IXLWorksheet sheet, int row, int col)
    {
        return sheet.Cell(row, col).Value.ToString() ?? "";
    }

    public async Task<string> GenerateBuildingNumberAsync()
    {
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            var existingNumbers = await db.Buildings.Select(b => b.BuildingNumber).ToListAsync();

            var maxNumber = 0;
            foreach (var num in existingNumbers)
            {
                var match = Regex.Match(num ?? "", @"^BLD-(\d+)$");
                if (match.Success && int.TryParse(match.Groups[1].Value, out var n))
                    maxNumber = Math.Max(maxNumber, n);
            }

            return "BLD-" + (maxNumber + 1).ToString("D3");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ExcelImport] GenerateBuildingNumber failed");
            return "BLD-" + DateTime.UtcNow.Ticks.ToString().Substring(10, 3);
        }
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
    public bool IsDisabled { get; set; } = false;
    public string ClosedReason { get; set; } = "";
    public string Notes { get; set; } = "";
}