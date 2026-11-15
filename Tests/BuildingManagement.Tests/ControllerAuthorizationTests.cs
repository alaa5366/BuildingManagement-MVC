using System.Reflection;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BuildingManagement.Tests;

// حارس ضد نسيان أو ضياع [Authorize]:
// أي Controller لازم يبقى عليه [Authorize] أو [AllowAnonymous] (على الكلاس أو على كل أكشن)،
// ماعدا الصفحات العامة عن قصد. لو حد حط تعليق/سطر بالغلط ولغى الـ Authorize الاختبار ده بيفشل.
public class ControllerAuthorizationTests
{
    // صفحات عامة عن قصد (الرئيسية بتحوّل لصفحة الدخول)
    private static readonly HashSet<string> PublicByDesign = new() { "HomeController" };

    private static IEnumerable<Type> ControllerTypes() =>
        typeof(SecretVerifier).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract);

    private static bool HasAuthAttribute(ICustomAttributeProvider p) =>
        p.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any() ||
        p.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any();

    [Fact]
    public void Every_action_is_protected_or_explicitly_anonymous()
    {
        var offenders = new List<string>();

        foreach (var c in ControllerTypes())
        {
            if (PublicByDesign.Contains(c.Name)) continue;
            if (HasAuthAttribute(c)) continue;               // حماية على مستوى الكلاس

            foreach (var m in c.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (m.IsSpecialName) continue;
                if (m.GetCustomAttribute<NonActionAttribute>() != null) continue;
                if (!HasAuthAttribute(m)) offenders.Add($"{c.Name}.{m.Name}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Actions without [Authorize]/[AllowAnonymous]: " + string.Join(", ", offenders));
    }

    [Theory]
    [InlineData("ExpensesController", "admin")]
    [InlineData("RevenuesController", "admin")]
    [InlineData("WalletController", "admin")]
    [InlineData("BackupController", "superadmin")]
    public void Sensitive_controllers_keep_their_role_restriction(string controllerName, string expectedRole)
    {
        var type = ControllerTypes().Single(t => t.Name == controllerName);
        var roles = type.GetCustomAttributes<AuthorizeAttribute>(true)
            .Select(a => a.Roles ?? "")
            .SelectMany(r => r.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToList();

        Assert.Contains(expectedRole, roles);
    }
}
