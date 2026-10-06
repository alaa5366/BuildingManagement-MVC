using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;

namespace BuildingManagementMvc.Services;

public class ExcelTemplateService
{
    private readonly string _secretKey;
    private readonly ILogger<ExcelTemplateService> _logger;
    private const int ValidityDays = 7;
    private const string TemplateId = "tpl_v3";

    public ExcelTemplateService(IConfiguration config, ILogger<ExcelTemplateService> logger)
    {
        _secretKey = ConfigSecrets.Require(config, "Excel:SecretKey");
        _logger = logger;
    }

    public byte[] GenerateTemplate(string? buildingId = null, string? issuedBy = null)
    {
        using var workbook = new XLWorkbook();

        // ============ 1. _meta ============
        var metaSheet = workbook.Worksheets.Add("_meta");
        metaSheet.Cell(1, 1).Value = "Field";
        metaSheet.Cell(1, 2).Value = "Value";

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

        // ============ 2. Building Info ============
        var infoSheet = workbook.Worksheets.Add("Building Info");
        infoSheet.Cell(1, 1).Value = "الحقل";
        infoSheet.Cell(1, 2).Value = "القيمة";
        infoSheet.Cell(1, 3).Value = "ملاحظات";

        infoSheet.Cell(2, 1).Value = "Building Name";
        infoSheet.Cell(2, 3).Value = "اسم العمارة (إلزامي - من 2 إلى 100 حرف)";

        infoSheet.Cell(3, 1).Value = "Building Number";
        infoSheet.Cell(3, 2).Value = "(تلقائي)";
        infoSheet.Cell(3, 3).Value = "⚠️ اتركه فاضي — النظام هيولّد رقم تلقائياً (BLD-XXX)";
        infoSheet.Cell(3, 2).Style.Font.Italic = true;
        infoSheet.Cell(3, 2).Style.Font.FontColor = XLColor.FromHtml("#888888");
        infoSheet.Cell(3, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        infoSheet.Cell(4, 1).Value = "Admin PIN";
        infoSheet.Cell(4, 2).Value = "1234";
        infoSheet.Cell(4, 3).Value = "PIN الأدمن (إلزامي - 4 أرقام بالظبط)";
        infoSheet.Cell(4, 2).Style.Font.Italic = true;
        infoSheet.Cell(4, 2).Style.Font.FontColor = XLColor.FromHtml("#888888");

        infoSheet.Cell(5, 1).Value = "Admin WhatsApp";
        infoSheet.Cell(5, 2).Value = "+201012345678";
        infoSheet.Cell(5, 3).Value = "رقم واتساب الأدمن (إلزامي)";
        infoSheet.Cell(5, 2).Style.Font.Italic = true;
        infoSheet.Cell(5, 2).Style.Font.FontColor = XLColor.FromHtml("#888888");

        infoSheet.Cell(6, 1).Value = "Logo URL";
        infoSheet.Cell(6, 3).Value = "رابط شعار العمارة (اختياري)";

        StyleHeaderRow(infoSheet, 3);
        AddInfoValidations(infoSheet);
        ProtectSheet(infoSheet, 3, startDataRow: 2);

        infoSheet.Column(1).Width = 25;
        infoSheet.Column(2).Width = 30;
        infoSheet.Column(3).Width = 60;

        // ============ 3. Floors & Apartments ============
        var aptsSheet = workbook.Worksheets.Add("Floors & Apartments");

        var headers = new[]
        {
            "Floor Label", "Floor Order", "Apt Number", "Owner",
            "WhatsApp", "PIN", "Monthly Fee", "Apt Label",
            "Status", "Is Disabled?", "Closed Reason", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
            aptsSheet.Cell(1, i + 1).Value = headers[i];

        StyleHeaderRow(aptsSheet, headers.Length);

        // ============ صف توضيحي (الصف 2) ============
        var noteRow = aptsSheet.Row(2);
        noteRow.Cell(1).Value = "الصفوف من 2 إلى 5 توضيحية فقط - مش هتتستورد. املأ بياناتك من الصف 6.";
        noteRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD");
        noteRow.Style.Font.Italic = true;
        noteRow.Style.Font.Bold = true;
        noteRow.Style.Font.FontColor = XLColor.FromHtml("#856404");
        noteRow.Height = 28;
        aptsSheet.Range(2, 1, 2, headers.Length).Merge();

        // ============ أمثلة (3، 4، 5) ============
        // Values ثابتة للعرض
        var examples = new object[][]
        {
            new object[] { "الدور الأرضي", 0, 1, "أحمد علي",   "+201012345678", "1111", 200, "شقة أحمد", "open", "لا", "", "" },
            new object[] { "الدور الأرضي", 0, 2, "محمد سيد",   "+201112345678", "2222", 200, "",         "open", "لا", "", "" },
            new object[] { "الدور الأول",  1, 3, "سارة أحمد",  "+201212345678", "3333", 250, "",         "open", "لا", "", "" },
        };

        for (int exIdx = 0; exIdx < examples.Length; exIdx++)
        {
            var excelRow = 3 + exIdx;
            var row = aptsSheet.Row(excelRow);
            var example = examples[exIdx];

            for (int colIdx = 0; colIdx < example.Length; colIdx++)
                row.Cell(colIdx + 1).Value = XLCellValue.FromObject(example[colIdx]);

            row.Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F0F0");
            row.Style.Font.FontColor = XLColor.FromHtml("#888888");
            row.Style.Font.Italic = true;
        }

        // ============ Formulas للصفوف الحقيقية (6-1000) ============
        for (int r = 6; r <= 1000; r++)
        {
            // B: Floor Order — يقبل "الدور الأرضي" = 0، إلخ
            aptsSheet.Cell(r, 2).FormulaA1 = BuildFloorOrderFormula(r);

            // C: Apt Number — COUNTA من الصف 6 (يبدأ من جديد)
            aptsSheet.Cell(r, 3).FormulaA1 = $"IF(A{r}=\"\",\"\",COUNTA($A$6:A{r}))";
        }

        AddDataValidations(aptsSheet);
        ProtectSheet(aptsSheet, headers.Length, startDataRow: 6);

        // ✅ B و C مقفولين (Formulas، مش للكتابة)
        for (int r = 6; r <= 1000; r++)
        {
            aptsSheet.Cell(r, 2).Style.Protection.SetLocked(true);
            aptsSheet.Cell(r, 3).Style.Protection.SetLocked(true);
        }

        // عرض الأعمدة
        aptsSheet.Column(1).Width = 18;
        aptsSheet.Column(2).Width = 14;
        aptsSheet.Column(3).Width = 14;
        aptsSheet.Column(4).Width = 40;
        aptsSheet.Column(5).Width = 18;
        aptsSheet.Column(6).Width = 10;
        aptsSheet.Column(7).Width = 14;
        aptsSheet.Column(8).Width = 35;
        aptsSheet.Column(9).Width = 12;
        aptsSheet.Column(10).Width = 14;
        aptsSheet.Column(11).Width = 30;
        aptsSheet.Column(12).Width = 40;

        aptsSheet.SheetView.FreezeRows(1);

        // ============ 4. Expense Categories ============
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
        expSheet.Column(1).Width = 25;
        expSheet.Column(2).Width = 15;
        expSheet.Column(3).Width = 12;
        expSheet.Column(4).Width = 30;

        // ============ 5. Revenue Categories ============
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
        revSheet.Column(1).Width = 25;
        revSheet.Column(2).Width = 15;
        revSheet.Column(3).Width = 12;
        revSheet.Column(4).Width = 30;

        // ============ 6. Help ============
        var helpSheet = workbook.Worksheets.Add("Help");
        helpSheet.Cell(1, 1).Value = "دليل استخدام قالب استيراد العمارة";
        helpSheet.Cell(3, 1).Value = "تعليمات مهمة:";
        helpSheet.Cell(4, 1).Value = "1. املأ ورقة Building Info ببيانات العمارة";
        helpSheet.Cell(5, 1).Value = "2. املأ ورقة Floors & Apartments من الصف 6 (الصفوف 2-5 توضيحية)";
        helpSheet.Cell(6, 1).Value = "3. اختر الدور من الـ ComboBox (العمود A)";
        helpSheet.Cell(7, 1).Value = "4. Floor Order و Apt Number بيتولّدوا تلقائياً";
        helpSheet.Cell(8, 1).Value = "5. رقم الواتساب: أرقام فقط، يقبل + في البداية";
        helpSheet.Cell(9, 1).Value = "6. PIN لازم 4 أرقام بالظبط";
        helpSheet.Cell(10, 1).Value = "7. عمود Status: open أو closed";
        helpSheet.Cell(11, 1).Value = "8. عمود Is Disabled?: لا (متاح) أو نعم (معطل)";
        helpSheet.Cell(12, 1).Value = "9. ⚠️ Building Number بيتولّد تلقائياً — ماتكتبش فيه";
        helpSheet.Cell(13, 1).Value = "10. Floor Order: الأرضي = 0، الأول = 1، الثاني = 2، وهكذا";
        helpSheet.Cell(14, 1).Value = "11. Apt Number: 1، 2، 3، ... (متسلسل عبر كل الأدوار)";
        helpSheet.Cell(16, 1).Value = $"هذا القالب صالح حتى: {expiresAtStr}";
        helpSheet.Cell(17, 1).Value = "للدعم: alaa5366@gmail.com";
        helpSheet.Column(1).Width = 90;

        infoSheet.Columns().AdjustToContents();
        expSheet.Columns().AdjustToContents();
        revSheet.Columns().AdjustToContents();
        helpSheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    // ============================================================
    // ✅ Excel Formula لـ Floor Order
    // ============================================================
    private static string BuildFloorOrderFormula(int row)
    {
        return $@"IF(A{row}="""",""""," +
               $@"IF(A{row}=""البدروم"",-1," +
               $@"IF(A{row}=""الدور الأرضي"",0," +
               $@"IF(A{row}=""الدور الأول"",1," +
               $@"IF(A{row}=""الدور الثاني"",2," +
               $@"IF(A{row}=""الدور الثالث"",3," +
               $@"IF(A{row}=""الدور الرابع"",4," +
               $@"IF(A{row}=""الدور الخامس"",5," +
               $@"IF(A{row}=""الدور السادس"",6," +
               $@"IF(A{row}=""الدور السابع"",7," +
               $@"IF(A{row}=""الدور الثامن"",8," +
               $@"IF(A{row}=""الدور التاسع"",9," +
               $@"IF(A{row}=""الدور العاشر"",10," +
               $@"IF(A{row}=""الدور الحادي عشر"",11," +
               $@"IF(A{row}=""الدور الثاني عشر"",12," +
               $@"IF(A{row}=""الدور الثالث عشر"",13," +
               $@"IF(A{row}=""الدور الرابع عشر"",14," +
               $@"IF(A{row}=""الدور الخامس عشر"",15," +
               $@"IF(A{row}=""الروف"",99,0)))))))))))))))))))";
    }

    private void StyleHeaderRow(IXLWorksheet sheet, int cols)
    {
        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#12433C");
        headerRow.Style.Font.FontColor = XLColor.White;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRow.Style.Alignment.WrapText = true;
        headerRow.Height = 35;
    }

    private void AddInfoValidations(IXLWorksheet sheet)
    {
        var nameValidation = sheet.Range("B2").CreateDataValidation();
        nameValidation.Custom("=AND(ISTEXT(B2),LEN(B2)>=2,LEN(B2)<=100)");
        nameValidation.IgnoreBlanks = false;
        nameValidation.ShowErrorMessage = true;
        nameValidation.ErrorTitle = "اسم العمارة غير صحيح";
        nameValidation.ErrorMessage = "اسم العمارة لازم يكون نص من 2 إلى 100 حرف";
        nameValidation.ErrorStyle = XLErrorStyle.Stop;

        var bldNumValidation = sheet.Range("B3").CreateDataValidation();
        bldNumValidation.Custom("=OR(ISBLANK(B3),B3=\"(تلقائي)\")");
        bldNumValidation.IgnoreBlanks = true;
        bldNumValidation.ShowErrorMessage = true;
        bldNumValidation.ErrorTitle = "رقم العمارة";
        bldNumValidation.ErrorMessage = "اتركه فاضي أو اكتب (تلقائي)";
        bldNumValidation.ErrorStyle = XLErrorStyle.Warning;

        var adminPinValidation = sheet.Range("B4").CreateDataValidation();
        adminPinValidation.Custom("=AND(ISNUMBER(B4),LEN(B4)=4)");
        adminPinValidation.IgnoreBlanks = false;
        adminPinValidation.ShowErrorMessage = true;
        adminPinValidation.ErrorTitle = "PIN الأدمن غير صحيح";
        adminPinValidation.ErrorMessage = "PIN الأدمن لازم يكون 4 أرقام بالظبط";
        adminPinValidation.ErrorStyle = XLErrorStyle.Stop;

        var adminWhatsValidation = sheet.Range("B5").CreateDataValidation();
        adminWhatsValidation.Custom("=AND(ISNUMBER(VALUE(SUBSTITUTE(B5,\"+\",\"\"))),LEN(SUBSTITUTE(B5,\"+\",\"\"))>=7)");
        adminWhatsValidation.IgnoreBlanks = false;
        adminWhatsValidation.ShowErrorMessage = true;
        adminWhatsValidation.ErrorTitle = "رقم الواتساب غير صحيح";
        adminWhatsValidation.ErrorMessage = "رقم الواتساب لازم أرقام فقط (7 خانات على الأقل)";
        adminWhatsValidation.ErrorStyle = XLErrorStyle.Stop;

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
        // Status (I)
        var statusValidation = sheet.Range("I6:I1000").CreateDataValidation();
        statusValidation.List("\"open,closed\"", true);
        statusValidation.IgnoreBlanks = true;
        statusValidation.InCellDropdown = true;
        statusValidation.ShowErrorMessage = true;
        statusValidation.ErrorTitle = "قيمة غير صحيحة";
        statusValidation.ErrorMessage = "اختر open أو closed فقط";
        statusValidation.ErrorStyle = XLErrorStyle.Stop;

        // Is Disabled? (J)
        var disabledValidation = sheet.Range("J6:J1000").CreateDataValidation();
        disabledValidation.List("\"لا,نعم\"", true);
        disabledValidation.IgnoreBlanks = true;
        disabledValidation.InCellDropdown = true;
        disabledValidation.ShowErrorMessage = true;
        disabledValidation.ErrorTitle = "قيمة غير صحيحة";
        disabledValidation.ErrorMessage = "اختر (لا) أو (نعم)";
        disabledValidation.ErrorStyle = XLErrorStyle.Warning;

        // Floor Label (A) — ComboBox
        var floorLabelValidation = sheet.Range("A6:A1000").CreateDataValidation();
        floorLabelValidation.List(
            "\"البدروم,الدور الأرضي,الدور الأول,الدور الثاني,الدور الثالث," +
            "الدور الرابع,الدور الخامس,الدور السادس,الدور السابع,الدور الثامن," +
            "الدور التاسع,الدور العاشر,الدور الحادي عشر,الدور الثاني عشر," +
            "الدور الثالث عشر,الدور الرابع عشر,الدور الخامس عشر,الروف\"",
            true);
        floorLabelValidation.IgnoreBlanks = true;
        floorLabelValidation.InCellDropdown = true;
        floorLabelValidation.ShowErrorMessage = true;
        floorLabelValidation.ErrorTitle = "قيمة غير صحيحة";
        floorLabelValidation.ErrorMessage = "اختر اسم الدور من القائمة";
        floorLabelValidation.ErrorStyle = XLErrorStyle.Warning;

        // PIN (F)
        var pinValidation = sheet.Range("F6:F1000").CreateDataValidation();
        pinValidation.Custom("=AND(ISNUMBER(F6),LEN(F6)=4)");
        pinValidation.IgnoreBlanks = true;
        pinValidation.ShowErrorMessage = true;
        pinValidation.ErrorTitle = "PIN غير صحيح";
        pinValidation.ErrorMessage = "PIN لازم 4 أرقام بالظبط";
        pinValidation.ErrorStyle = XLErrorStyle.Stop;

        // Monthly Fee (G)
        var feeValidation = sheet.Range("G6:G1000").CreateDataValidation();
        feeValidation.Custom("=OR(ISBLANK(G6),AND(ISNUMBER(G6),G6>=0))");
        feeValidation.IgnoreBlanks = true;
        feeValidation.ShowErrorMessage = true;
        feeValidation.ErrorTitle = "قيمة غير صحيحة";
        feeValidation.ErrorMessage = "الرسم الشهري لازم رقم موجب أو صفر";
        feeValidation.ErrorStyle = XLErrorStyle.Stop;

        // WhatsApp (E)
        var phoneValidation = sheet.Range("E6:E1000").CreateDataValidation();
        phoneValidation.Custom("=OR(ISBLANK(E6),AND(ISNUMBER(VALUE(SUBSTITUTE(E6,\"+\",\"\"))),LEN(SUBSTITUTE(E6,\"+\",\"\"))>=7))");
        phoneValidation.IgnoreBlanks = true;
        phoneValidation.ShowErrorMessage = true;
        phoneValidation.ErrorTitle = "رقم الواتساب غير صحيح";
        phoneValidation.ErrorMessage = "رقم الواتساب لازم أرقام فقط (7 خانات على الأقل)";
        phoneValidation.ErrorStyle = XLErrorStyle.Stop;
    }

    private void ProtectSheet(IXLWorksheet sheet, int cols, int startDataRow = 2)
    {
        sheet.Row(1).Style.Protection.SetLocked(true);

        for (int row = 2; row < startDataRow; row++)
            for (int col = 1; col <= cols; col++)
                sheet.Cell(row, col).Style.Protection.SetLocked(true);

        for (int row = startDataRow; row <= 1000; row++)
            for (int col = 1; col <= cols; col++)
                sheet.Cell(row, col).Style.Protection.SetLocked(false);

        sheet.Protect()
            .AllowElement(XLSheetProtectionElements.InsertRows)
            .AllowElement(XLSheetProtectionElements.DeleteRows);
    }

    internal string ComputeSignatureRaw(string generatedAtStr, string expiresAtStr, string buildingId)
    {
        var data = $"{generatedAtStr}|{expiresAtStr}|{TemplateId}|{buildingId}";
        return ComputeHmac(data);
    }

    private string ComputeHmac(string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}