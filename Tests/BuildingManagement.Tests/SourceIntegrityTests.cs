using System.Text.RegularExpressions;
using Xunit;

namespace BuildingManagement.Tests;

/// <summary>
/// Guards against a mistake that has already happened once: a comment banner that swallowed the
/// <c>[Authorize]</c> attribute because both ended up on the same line. These checks scan the source files.
/// </summary>
public class SourceIntegrityTests
{
    // سطر تعليق فيه خط فاصل (=== أو ---) وبعده مباشرة attribute
    private static readonly Regex HiddenAttribute = new(
        @"^\s*//.*[=\-]{3,}\s*\[(?:Authorize|AllowAnonymous|Http\w+|ValidateAntiForgeryToken|IgnoreAntiforgeryToken|EnableRateLimiting|RequestSizeLimit|RequestFormLimits|AdminPermission|HasPermission|Route|NonAction)",
        RegexOptions.Compiled);

    // أي كود بعد خط فاصل طويل جوه سطر تعليق
    private static readonly Regex CodeAfterBanner = new(@"^\s*//.*[=\-]{5,}[^=\-\s]", RegexOptions.Compiled);

    private static string? FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "BuildingManagementMVC.csproj")))
            dir = dir.Parent;
        return dir?.FullName;
    }

    private static IEnumerable<string> AppSourceFiles(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                var rel = f.Substring(root.Length).Replace('\\', '/');
                return !rel.Contains("/Migrations/") && !rel.Contains("/obj/") &&
                       !rel.Contains("/bin/") && !rel.Contains("/Tests/");
            });

    [Fact]
    public void No_attribute_is_hidden_inside_a_line_comment()
    {
        var root = FindRoot();
        if (root == null) return;

        var hits = new List<string>();
        foreach (var file in AppSourceFiles(root))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
                if (HiddenAttribute.IsMatch(lines[i])) hits.Add($"{Path.GetFileName(file)}:{i + 1}");
        }

        Assert.True(hits.Count == 0, "Attribute inside a comment: " + string.Join(", ", hits));
    }

    [Fact]
    public void No_code_follows_a_comment_banner_on_the_same_line()
    {
        var root = FindRoot();
        if (root == null) return;

        var hits = new List<string>();
        foreach (var file in AppSourceFiles(root))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
                if (CodeAfterBanner.IsMatch(lines[i])) hits.Add($"{Path.GetFileName(file)}:{i + 1}");
        }

        Assert.True(hits.Count == 0, "Text after a comment banner: " + string.Join(", ", hits));
    }
}
