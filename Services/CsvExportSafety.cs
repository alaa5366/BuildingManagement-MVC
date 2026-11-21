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
        var isNumber = decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out _);

        // Spreadsheet applications may ignore leading whitespace, control characters,
        // BOMs, and Unicode format characters before interpreting a formula prefix.
        // Preserve legitimate numeric values (including negative amounts).
        var firstMeaningful = value.FirstOrDefault(character =>
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);
            return !char.IsWhiteSpace(character) &&
                   !char.IsControl(character) &&
                   category != System.Globalization.UnicodeCategory.Format;
        });

        if (!isNumber && "=+-@".Contains(firstMeaningful))
            value = "'" + value;
        else if (value.Length > 0 && (char.IsControl(value[0]) ||
                 System.Globalization.CharUnicodeInfo.GetUnicodeCategory(value[0]) ==
                 System.Globalization.UnicodeCategory.Format))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
