namespace BuildingManagementMvc.Services;

/// <summary>Protects spreadsheet exports from formula injection.</summary>
public static class CsvExportSafety
{
    /// <summary>
    /// يُهيّئ قيمة خلية CSV ويمنع حقن الصيغ.
    /// </summary>
    /// <param name="value">القيمة الخام للخلية.</param>
    /// <returns>القيمة الآمنة للإدراج في CSV.</returns>
    public static string Cell(string? value)
    {
        value ??= string.Empty;
        var first = value.TrimStart();
        var isNumber = decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out _);
        if (!isNumber && first.Length > 0 && "=+-@".Contains(first[0])) value = "'" + value;
        if (value.Length > 0 && (value[0] == '\t' || value[0] == '\r' || value[0] == '\n'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
