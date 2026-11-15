using System.Text.RegularExpressions;
using Xunit;

namespace BuildingManagement.Tests;

/// <summary>
/// Keeps the XML documentation complete: every public type, and every public method of the
/// controllers, services and data classes, must have a <c>///</c> block. The check reads the
/// source files, so it works the same on every machine. When the repository root cannot be found
/// (for example when only the compiled tests are copied elsewhere) the checks return without failing.
/// </summary>
public class DocumentationCoverageTests
{
    private static readonly Regex TypeRegex = new(
        @"^\s*(?:public|internal)\s+(?:(?:sealed|static|abstract|partial|readonly)\s+)*(?:class|record|enum|interface|struct)\s+(\w+)",
        RegexOptions.Compiled);

    private static readonly Regex MethodRegex = new(
        @"^\s+public\s+(?!class\b|record\b|enum\b|interface\b|struct\b|override\b)(?:(?:static|async|virtual|sealed|abstract|new)\s+)*[\w<>\[\],.?()\s]+?\s+(\w+)\s*\(",
        RegexOptions.Compiled);

    private static string? FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BuildingManagementMVC.csproj")))
            dir = dir.Parent;
        return dir?.FullName;
    }

    private static IEnumerable<string> SourceFiles(string root, params string[] folders)
    {
        foreach (var folder in folders)
        {
            var path = Path.Combine(root, folder);
            if (!Directory.Exists(path)) continue;
            foreach (var f in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
                if (!f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) yield return f;
        }

        var loc = Path.Combine(root, "Loc.cs");
        if (File.Exists(loc)) yield return loc;
    }

    // الـ doc لازم يبقى فوق الـ attributes مباشرة
    private static bool IsDocumented(string[] lines, int index)
    {
        var k = index - 1;
        while (k >= 0 && lines[k].TrimStart().StartsWith("[", StringComparison.Ordinal)) k--;
        return k >= 0 && lines[k].TrimStart().StartsWith("///", StringComparison.Ordinal);
    }

    private static string Report(List<string> missing) =>
        "Missing XML documentation: " + string.Join(", ", missing.Take(25)) +
        (missing.Count > 25 ? $" ... (+{missing.Count - 25} more)" : "");

    [Fact]
    public void Every_public_type_has_xml_documentation()
    {
        var root = FindRoot();
        if (root == null) return;

        var missing = new List<string>();
        foreach (var file in SourceFiles(root, "Controllers", "Services", "Models", "ViewModels", "Data", "Attributes"))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var m = TypeRegex.Match(lines[i]);
                if (m.Success && !IsDocumented(lines, i))
                    missing.Add($"{Path.GetFileName(file)}:{m.Groups[1].Value}");
            }
        }

        Assert.True(missing.Count == 0, Report(missing));
    }

    [Fact]
    public void Every_public_method_of_controllers_services_and_data_classes_has_xml_documentation()
    {
        var root = FindRoot();
        if (root == null) return;

        var missing = new List<string>();
        foreach (var file in SourceFiles(root, "Controllers", "Services", "Data"))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(" override ", StringComparison.Ordinal)) continue;
                var m = MethodRegex.Match(lines[i]);
                if (m.Success && !IsDocumented(lines, i))
                    missing.Add($"{Path.GetFileName(file)}:{m.Groups[1].Value}");
            }
        }

        Assert.True(missing.Count == 0, Report(missing));
    }
}
