using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;

namespace BuildingManagementMvc.Services;

public class ExcelTemplateService
{
    private readonly string _secretKey;
    private readonly ILogger<ExcelTemplateService> _logger;
    private const int ValidityDays = 7;
    private const string TemplateId = "tpl_v2";

    public ExcelTemplateService(IConfiguration config, ILogger<ExcelTemplateService> logger)
    {
        _secretKey = config["Excel:SecretKey"] ?? "CHANGE_ME_IN_APPSETTINGS";
        _logger = logger;
    }

    public byte[] GenerateTemplate(string? buildingId = null, string? issuedBy = null)
    {
        using var workbook = new XLWorkbook();

        // ============ 1. ورقة _meta (مخفية) ============
        var metaSheet = workbook.Worksheets.Add("_meta");
        metaSheet.Cell(1, 1).Value = "Field";
        metaSheet.Cell(1, 2).Value = "Value";

        // ✅ استخدم نصوص خام بدل DateTime
        var generatedAtStr = DateTime.UtcNow.ToString("o");
        var expiresAtStr = DateTime.UtcNow.AddDays(ValidityDays).ToString("o");
        var bId = buildingId ?? "new";
        var issuedByStr = issuedBy ?? "unknown";

        metaSheet.Cell(2, 1).Value = "GeneratedAt";
        metaSheet.Cell(2, 2).Value = generatedAtStr;
        metaSheet.Cell(3, 1).Value = "ExpiresAt";
        metaSheet.Cell(3, 2).Value = expiresAtStr;
        metaSheet.Cell(4, 1).Value = "TemplateId";
        metaSheet.Cell(4, 2).Value = TemplateId;
        metaSheet.Cell(5, 1).Value = "BuildingId";
        metaSheet.Cell(5, 2).Value = bId;
        metaSheet.Cell(6, 1).Value = "IssuedBy";
        metaSheet.Cell(6, 2).Value = issuedByStr;
        metaSheet.Cell(7, 1).Value = "Signature";
        metaSheet.Cell(7, 2).Value = ComputeSignatureRaw(generatedAtStr, expiresAtStr, bId);

        metaSheet.Hide();

        // ============ 2. ورقة Building Info ============
        var infoSheet = workbook.Worksheets.Add("Building Info");
        infoSheet.Cell(1, 1).Value = "الحقل";
        infoSheet.Cell(1, 2).Value = "القيمة";
        infoSheet.Cell(1, 3).Value = "ملاحظات";

        infoSheet.Cell(2, 1).Value = "Building Name";
        infoSheet.Cell(2, 3).Value = "اسم العمارة (إلزامي - من 2 إلى 100 حرف)";

        infoSheet.Cell(3, 1).Value = "Building Number";
        infoSheet.Cell(3, 2).Value = "BLD-001";
        infoSheet.Cell(3, 3).Value = "رقم العمارة (إلزامي بصيغة BLD-XXX)";
        infoSheet.Cell(3, 2).Style.Font.Italic = true;
        infoSheet.Cell(3, 2).Style.Font.FontColor = XLColor.FromHtml("#888888");

        infoSheet.Cell(4, 1).Value = "Admin PIN";
        infoSheet.Cell(4, 2).Value = "1234";
        infoSheet.Cell(4, 3).Value = "PIN الأدمن (إلزامي - 4 أرقام بالظبط)";
        infoSheet.Cell(4, 2).Style.Font.Italic = true;
        infoSheet.Cell(4, 2).Style.Font.FontColor = XLColor.FromHtml("#888888");

        infoSheet.Cell(5, 1).Value = "Admin WhatsApp";
        infoSheet.Cell(5, 2).Value = "+201012345678";
        infoSheet.Cell(5, 3).Value = "رقم واتساب الأدمن (إلزامي - مثال: +201012345678)";
        infoSheet.Cell(5, 2).Style.Font.Italic = true;
        infoSheet.Cell(5, 2).Style.Font.FontColor = XLColor.FromHtml("#888888");

        infoSheet.Cell(6, 1).Value = "Logo URL";
        infoSheet.Cell(6, 3).Value = "رابط شعار العمارة (اختياري)";

        StyleHeaderRow(infoSheet, 3);
        AddInfoValidations(infoSheet);
        ProtectSheet(infoSheet, 3, startDataRow: 2);

        // ============ 3. ورقة Floors & Apartments ============
        var aptsSheet = workbook.Worksheets.Add("Floors & Apartments");
        var headers = new[]
        {
            "Floor Label", "Floor Order", "Apt Number", "Owner",
            "WhatsApp", "PIN", "Monthly Fee", "Apt Label",
            "Status", "Closed Reason", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            aptsSheet.Cell(1, i + 1).Value = headers[i];
        }

        StyleHeaderRow(aptsSheet, headers.Length);

        // صف توضيحي (الصف 2)
        var noteRow = aptsSheet.Row(2);
        noteRow.Cell(1).Value = "الصفوف من 2 إلى 5 توضيحية فقط - مش هتتستورد. املأ بياناتك من الصف 6.";
        noteRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
        noteRow.Style.Font.Italic = true;
        noteRow.Style.Font.Bold = true;
        noteRow.Style.Font.FontColor = XLColor.FromHtml("#856404");
        noteRow.Height = 28;
        aptsSheet.Range(2, 1, 2, headers.Length).Merge();

        // صفوف الأمثلة (الصفوف 3-5)
        var examples = new object[][]
        {
            new object[] { "الدور الأرضي", 0, 1, "أحمد علي", "+201012345678", "1111", 200, "شقة أحمد", "open", "", "مثال - احذف الصف" },
            new object[] { "الدور الأرضي", 0, 2, "محمد سيد", "+201112345678", "2222", 200, "", "open", "", "مثال - احذف الصف" },
            new object[] { "الدور الأول", 1, 3, "سارة أحمد", "+201212345678", "3333", 250, "", "open", "", "مثال - احذف الصف" },
        };

        for (int exIdx = 0; exIdx < examples.Length; exIdx++)
        {
            var row = aptsSheet.Row(3 + exIdx);
            var example = examples[exIdx];

            for (int colIdx = 0; colIdx < example.Length; colIdx++)
            {
                row.Cell(colIdx + 1).Value = XLCellValue.FromObject(example[colIdx]);
            }

            row.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F0F0");
            row.Style.Font.FontColor = XLColor.FromHtml("#888888");
            row.Style.Font.Italic = true;
        }

        AddDataValidations(aptsSheet);
        ProtectSheet(aptsSheet, headers.Length, startDataRow: 6);

        // ============ 4. ورقة Expense Categories ============
        var expSheet = workbook.Worksheets.Add("Expense Categories");
        expSheet.Cell(1, 1).Value = "Name";
        expSheet.Cell(1, 2).Value = "Color";
        expSheet.Cell(1, 3).Value = "Active";
        expSheet.Cell(1, 4).Value = "ملاحظات";

        var defaultExpenses = new[]
        {
            ("مياه", "#4A90D9"), ("كهرباء", "#D9B34A"),
            ("نظافة", "#4FB286"), ("صيانة", "#A0432A"), ("أخرى", "#888888")
        };
        int rowIdx = 2;
        foreach (var (name, color) in defaultExpenses)
        {
            expSheet.Cell(rowIdx, 1).Value = name;
            expSheet.Cell(rowIdx, 2).Value = color;
            expSheet.Cell(rowIdx, 3).Value = true;
            rowIdx++;
        }
        StyleHeaderRow(expSheet, 4);

        // ============ 5. ورقة Revenue Categories ============
        var revSheet = workbook.Worksheets.Add("Revenue Categories");
        revSheet.Cell(1, 1).Value = "Name";
        revSheet.Cell(1, 2).Value = "Color";
        revSheet.Cell(1, 3).Value = "Active";
        revSheet.Cell(1, 4).Value = "ملاحظات";

        var defaultRevenues = new[]
        {
            ("تأجير السطح", "#2E7D5B"), ("تأجير محل", "#4FB286"),
            ("بيع خردة", "#8E6BB2"), ("أخرى", "#888888")
        };
        rowIdx = 2;
        foreach (var (name, color) in defaultRevenues)
        {
            revSheet.Cell(rowIdx, 1).Value = name;
            revSheet.Cell(rowIdx, 2).Value = color;
            revSheet.Cell(rowIdx, 3).Value = true;
            rowIdx++;
        }
        StyleHeaderRow(revSheet, 4);

        // ============ 6. ورقة Help ============
        var helpSheet = workbook.Worksheets.Add("Help");
        helpSheet.Cell(1, 1).Value = "دليل استخدام قالب استيراد العمارة";
        helpSheet.Cell(3, 1).Value = "تعليمات مهمة:";
        helpSheet.Cell(4, 1).Value = "1. املأ ورقة Building Info ببيانات العمارة";
        helpSheet.Cell(5, 1).Value = "2. املأ ورقة Floors & Apartments من الصف 6 (الصفوف 2-5 توضيحية)";
        helpSheet.Cell(6, 1).Value = "3. الأعمدة الإلزامية: Building Name, Floor Label, Apt Number, WhatsApp, PIN";
        helpSheet.Cell(7, 1).Value = "4. رقم الواتساب: أرقام فقط، يقبل + في البداية";
        helpSheet.Cell(8, 1).Value = "5. PIN لازم 4 أرقام بالظبط";
        helpSheet.Cell(9, 1).Value = "6. عمود Status: open أو closed";
        helpSheet.Cell(11, 1).Value = $"هذا القالب صالح حتى: {expiresAtStr}";
        helpSheet.Cell(12, 1).Value = "للدعم: alaa5366@gmail.com";

        // ============ Auto-fit ============
        infoSheet.Columns().AdjustToContents();
        aptsSheet.Columns().AdjustToContents();
        expSheet.Columns().AdjustToContents();
        revSheet.Columns().AdjustToContents();
        helpSheet.Columns().AdjustToContents();

        foreach (var sheet in workbook.Worksheets)
        {
            if (sheet.Name == "_meta") continue;
            foreach (var col in sheet.ColumnsUsed())
            {
                if (col.Width < 15) col.Width = 15;
                else if (col.Width > 50) col.Width = 50;
            }
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private void StyleHeaderRow(IXLWorksheet sheet, int cols)
    {
        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#12433C");
        headerRow.Style.Font.FontColor = XLColor.White;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRow.Height = 25;
    }

    private void AddInfoValidations(IXLWorksheet sheet)
    {
        // 1. Building Name (B2)
        var nameValidation = sheet.Range("B2").CreateDataValidation();
        nameValidation.Custom("=AND(ISTEXT(B2),LEN(B2)>=2,LEN(B2)<=100)");
        nameValidation.IgnoreBlanks = false;
        nameValidation.ShowErrorMessage = true;
        nameValidation.ErrorTitle = "اسم العمارة غير صحيح";
        nameValidation.ErrorMessage = "اسم العمارة لازم يكون نص من 2 إلى 100 حرف";
        nameValidation.ErrorStyle = XLErrorStyle.Stop;

        // 2. Building Number (B3)
        var bldNumValidation = sheet.Range("B3").CreateDataValidation();
        bldNumValidation.Custom("=AND(LEFT(B3,4)=\"BLD-\",LEN(B3)=7,ISNUMBER(VALUE(MID(B3,5,3))))");
        bldNumValidation.IgnoreBlanks = false;
        bldNumValidation.ShowErrorMessage = true;
        bldNumValidation.ErrorTitle = "رقم العمارة غير صحيح";
        bldNumValidation.ErrorMessage = "رقم العمارة لازم يكون بصيغة BLD-XXX (مثال: BLD-001)";
        bldNumValidation.ErrorStyle = XLErrorStyle.Stop;

        // 3. Admin PIN (B4)
        var adminPinValidation = sheet.Range("B4").CreateDataValidation();
        adminPinValidation.Custom("=AND(ISNUMBER(B4),LEN(B4)=4)");
        adminPinValidation.IgnoreBlanks = false;
        adminPinValidation.ShowErrorMessage = true;
        adminPinValidation.ErrorTitle = "PIN الأدمن غير صحيح";
        adminPinValidation.ErrorMessage = "PIN الأدمن لازم يكون 4 أرقام بالظبط";
        adminPinValidation.ErrorStyle = XLErrorStyle.Stop;

        // 4. Admin WhatsApp (B5)
        var adminWhatsValidation = sheet.Range("B5").CreateDataValidation();
        adminWhatsValidation.Custom("=AND(ISNUMBER(VALUE(SUBSTITUTE(B5,\"+\",\"\"))),LEN(SUBSTITUTE(B5,\"+\",\"\"))>=7)");
        adminWhatsValidation.IgnoreBlanks = false;
        adminWhatsValidation.ShowErrorMessage = true;
        adminWhatsValidation.ErrorTitle = "رقم الواتساب غير صحيح";
        adminWhatsValidation.ErrorMessage = "رقم الواتساب لازم يكون أرقام فقط (7 خانات على الأقل)";
        adminWhatsValidation.ErrorStyle = XLErrorStyle.Stop;

        // 5. Logo URL (B6)
        var logoValidation = sheet.Range("B6").CreateDataValidation();
        logoValidation.Custom("=OR(ISBLANK(B6),LEFT(B6,7)=\"http://\",LEFT(B6,8)=\"https://\")");
        logoValidation.IgnoreBlanks = true;
        logoValidation.ShowErrorMessage = true;
        logoValidation.ErrorTitle = "رابط الشعار غير صحيح";
        logoValidation.ErrorMessage = "الرابط لازم يبدأ بـ http:// أو https://";
        logoValidation.ErrorStyle = XLErrorStyle.Stop;
    }

    private void AddDataValidations(IXLWorksheet sheet)
    {
        // 1. Status dropdown
        var statusValidation = sheet.Range("I6:I1000").CreateDataValidation();
        statusValidation.List("\"open,closed\"", true);
        statusValidation.IgnoreBlanks = true;
        statusValidation.InCellDropdown = true;
        statusValidation.ShowErrorMessage = true;
        statusValidation.ErrorTitle = "قيمة غير صحيحة";
        statusValidation.ErrorMessage = "اختر open أو closed فقط";
        statusValidation.ErrorStyle = XLErrorStyle.Stop;

        // 2. PIN: 4 أرقام بالظبط
        var pinValidation = sheet.Range("F6:F1000").CreateDataValidation();
        pinValidation.Custom("=AND(ISNUMBER(F6),LEN(F6)=4)");
        pinValidation.IgnoreBlanks = true;
        pinValidation.ShowErrorMessage = true;
        pinValidation.ErrorTitle = "PIN غير صحيح";
        pinValidation.ErrorMessage = "PIN لازم 4 أرقام بالظبط";
        pinValidation.ErrorStyle = XLErrorStyle.Stop;

        // 3. Monthly Fee > 0
        var feeValidation = sheet.Range("G6:G1000").CreateDataValidation();
        feeValidation.Custom("=AND(ISNUMBER(G6),G6>0)");
        feeValidation.IgnoreBlanks = true;
        feeValidation.ShowErrorMessage = true;
        feeValidation.ErrorTitle = "قيمة غير صحيحة";
        feeValidation.ErrorMessage = "الرسم الشهري لازم يكون رقم موجب";
        feeValidation.ErrorStyle = XLErrorStyle.Stop;

        // 4. Apt Number > 0
        var aptValidation = sheet.Range("C6:C1000").CreateDataValidation();
        aptValidation.Custom("=AND(ISNUMBER(C6),C6>0)");
        aptValidation.IgnoreBlanks = true;
        aptValidation.ShowErrorMessage = true;
        aptValidation.ErrorTitle = "قيمة غير صحيحة";
        aptValidation.ErrorMessage = "رقم الشقة لازم يكون رقم موجب";
        aptValidation.ErrorStyle = XLErrorStyle.Stop;

        // 5. Floor Order - لازم رقم
        var floorOrderValidation = sheet.Range("B6:B1000").CreateDataValidation();
        floorOrderValidation.Custom("=ISNUMBER(B6)");
        floorOrderValidation.IgnoreBlanks = true;
        floorOrderValidation.ShowErrorMessage = true;
        floorOrderValidation.ErrorTitle = "رقم الدور غير صحيح";
        floorOrderValidation.ErrorMessage = "رقم الدور لازم يكون رقم (0، 1، 2، ...)";
        floorOrderValidation.ErrorStyle = XLErrorStyle.Stop;

        // 6. WhatsApp - أرقام فقط
        var phoneValidation = sheet.Range("E6:E1000").CreateDataValidation();
        phoneValidation.Custom("=AND(ISNUMBER(VALUE(SUBSTITUTE(E6,\"+\",\"\"))),LEN(SUBSTITUTE(E6,\"+\",\"\"))>=7)");
        phoneValidation.IgnoreBlanks = true;
        phoneValidation.ShowErrorMessage = true;
        phoneValidation.ErrorTitle = "رقم الواتساب غير صحيح";
        phoneValidation.ErrorMessage = "رقم الواتساب لازم يكون أرقام فقط (7 خانات على الأقل)";
        phoneValidation.ErrorStyle = XLErrorStyle.Stop;
    }

    private void ProtectSheet(IXLWorksheet sheet, int cols, int startDataRow = 2)
    {
        sheet.Row(1).Style.Protection.SetLocked(true);

        for (int row = 2; row < startDataRow; row++)
        {
            for (int col = 1; col <= cols; col++)
            {
                sheet.Cell(row, col).Style.Protection.SetLocked(true);
            }
        }

        for (int row = startDataRow; row <= 1000; row++)
        {
            for (int col = 1; col <= cols; col++)
            {
                sheet.Cell(row, col).Style.Protection.SetLocked(false);
            }
        }

        sheet.Protect()
            .AllowElement(XLSheetProtectionElements.InsertRows)
            .AllowElement(XLSheetProtectionElements.DeleteRows);
    }

    // ✅ دالة جديدة: تستخدم نصوص خام بدل DateTime
    internal string ComputeSignatureRaw(string generatedAtStr, string expiresAtStr, string buildingId)
    {
        var data = $"{generatedAtStr}|{expiresAtStr}|{TemplateId}|{buildingId}";
        return ComputeHmac(data);
    }

    // ⚠️ للتوافق مع الإصدارات القديمة
    internal string ComputeSignature(DateTime generatedAt, DateTime expiresAt, string buildingId, string issuedBy)
    {
        var generatedAtStr = generatedAt.ToString("o");
        var expiresAtStr = expiresAt.ToString("o");
        return ComputeSignatureRaw(generatedAtStr, expiresAtStr, buildingId);
    }

    private string ComputeHmac(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}