using System.Text.RegularExpressions;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Reads and validates an Excel building template and turns it into import data.
/// </summary>
public class ExcelImportService
{
    private readonly ExcelTemplateService _templateService;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<ExcelImportService> _logger;
    private const string TemplateId = "tpl_v3";

    /// <summary>
    /// Initializes a new instance of the <see cref="ExcelImportService"/> class.
    /// </summary>
    public ExcelImportService(
        ExcelTemplateService templateService,
        IDbContextFactory<AppDbContext> sqlFactory,
        ILogger<ExcelImportService> logger)
    {
        _templateService = templateService;
        _sqlFactory = sqlFactory;
        _logger = logger;
    }

    /// <summary>
    /// Validates the file.
    /// </summary>
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

            if (data.Apartments.Any(a => !string.IsNullOrWhiteSpace(a.Pin) &&
                (a.Pin.Length != 4 || !a.Pin.All(char.IsDigit))))
                return ImportValidationResult.Fail("كل PIN مدخل يجب أن يتكون من 4 أرقام.");

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

            decimal monthlyFee = 0;
            if (decimal.TryParse(monthlyFeeStr, out var mf))
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

    /// <summary>
    /// Generates the building number.
    /// </summary>
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

/// <summary>
/// Result of validating an Excel import file: errors, warnings and the parsed data.
/// </summary>
public class ImportValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether it is success.
    /// </summary>
    public bool IsSuccess { get; set; }
    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    public BuildingImportData? Data { get; set; }

    /// <summary>
    /// Returns the success.
    /// </summary>
    public static ImportValidationResult Success(BuildingImportData data) =>
        new() { IsSuccess = true, Data = data };

    /// <summary>
    /// Returns the fail.
    /// </summary>
    public static ImportValidationResult Fail(string error) =>
        new() { IsSuccess = false, Error = error };
}

/// <summary>
/// Building information parsed from the Excel template.
/// </summary>
public class BuildingImportData
{
    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = "";
    /// <summary>
    /// Gets or sets the building number.
    /// </summary>
    public string BuildingNumber { get; set; } = "";
    /// <summary>
    /// Gets or sets the admin PIN.
    /// </summary>
    public string AdminPin { get; set; } = "";
    /// <summary>
    /// Gets or sets the admin WhatsApp.
    /// </summary>
    public string AdminWhatsapp { get; set; } = "";
    /// <summary>
    /// Gets or sets the logo URL.
    /// </summary>
    public string LogoUrl { get; set; } = "";
    /// <summary>
    /// Gets or sets the apartments.
    /// </summary>
    public List<ApartmentImportData> Apartments { get; set; } = new();
}

/// <summary>
/// One apartment row parsed from the Excel template.
/// </summary>
public class ApartmentImportData
{
    /// <summary>
    /// Gets or sets the floor label.
    /// </summary>
    public string FloorLabel { get; set; } = "";
    /// <summary>
    /// Gets or sets the floor order.
    /// </summary>
    public int FloorOrder { get; set; }
    /// <summary>
    /// Gets or sets the apartment number.
    /// </summary>
    public int AptNumber { get; set; }
    /// <summary>
    /// Gets or sets the owner.
    /// </summary>
    public string Owner { get; set; } = "";
    /// <summary>
    /// Gets or sets the whats app.
    /// </summary>
    public string WhatsApp { get; set; } = "";
    /// <summary>
    /// Gets or sets the PIN.
    /// </summary>
    public string Pin { get; set; } = "";
    /// <summary>
    /// Gets or sets the monthly fee.
    /// </summary>
    public decimal MonthlyFee { get; set; }
    /// <summary>
    /// Gets or sets the apartment label.
    /// </summary>
    public string AptLabel { get; set; } = "";
    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public string Status { get; set; } = "open";
    /// <summary>
    /// Gets or sets a value indicating whether it is disabled.
    /// </summary>
    public bool IsDisabled { get; set; } = false;
    /// <summary>
    /// Gets or sets the closed reason.
    /// </summary>
    public string ClosedReason { get; set; } = "";
    /// <summary>
    /// Gets or sets the notes.
    /// </summary>
    public string Notes { get; set; } = "";
}