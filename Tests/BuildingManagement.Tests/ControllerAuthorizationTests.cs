using System.Reflection;
using System.Security.Claims;
using BuildingManagementMvc.Attributes;
using BuildingManagementMvc.Controllers;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
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
    [Fact]
    public void Sensitive_admin_features_enforce_their_declared_permissions()
    {
        Assert.Equal(
            AdminPermissions.ManageResidents,
            typeof(AdminResidentsController).GetCustomAttribute<AdminPermissionAttribute>()?.Permission);

        Assert.Equal(
            AdminPermissions.ViewCameras,
            typeof(CamerasController).GetCustomAttribute<AdminPermissionAttribute>()?.Permission);

        var buildingSettingsActions = typeof(SettingsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.Name == "Building")
            .ToList();

        Assert.Equal(2, buildingSettingsActions.Count);
        Assert.All(buildingSettingsActions, method =>
            Assert.Equal(
                AdminPermissions.ManageSettingsBuilding,
                method.GetCustomAttribute<AdminPermissionAttribute>()?.Permission));
    }

    [Fact]
    public void Admin_resident_actions_enforce_building_scope_and_protect_move_post()
    {
        var controller = new AdminResidentsController(null!, null!);
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("buildingId", "building-a")
            },
            "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var scopeCheck = typeof(AdminResidentsController).GetMethod(
            "CanAccessBuilding",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.True((bool)scopeCheck.Invoke(controller, new object[] { "building-a" })!);
        Assert.False((bool)scopeCheck.Invoke(controller, new object[] { "building-b" })!);

        var move = typeof(AdminResidentsController).GetMethod(nameof(AdminResidentsController.Move))!;
        Assert.NotNull(move.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(move.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, "superadmin") }, "test"))
            }
        };
        Assert.True((bool)scopeCheck.Invoke(controller, new object[] { "building-b" })!);
    }

    [Fact]
    public void Anonymous_post_actions_require_explicit_antiforgery_validation()
    {
        var offenders = new List<string>();

        foreach (var controller in ControllerTypes())
        {
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var acceptsPost = method.GetCustomAttributes<HttpMethodAttribute>(true)
                    .Any(attribute => attribute.HttpMethods.Contains("POST", StringComparer.OrdinalIgnoreCase));
                if (!acceptsPost || method.GetCustomAttribute<AllowAnonymousAttribute>() is null)
                    continue;

                if (method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() is null &&
                    controller.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>() is null &&
                    controller.GetCustomAttribute<AutoValidateAntiforgeryTokenAttribute>() is null)
                    offenders.Add($"{controller.Name}.{method.Name}");
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void Controller_actions_do_not_repeat_the_same_http_method_attribute()
    {
        var offenders = new List<string>();

        foreach (var controller in ControllerTypes())
        {
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var duplicateVerbs = method.GetCustomAttributes<HttpMethodAttribute>(true)
                    .SelectMany(attribute => attribute.HttpMethods)
                    .GroupBy(verb => verb, StringComparer.OrdinalIgnoreCase)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key);

                offenders.AddRange(duplicateVerbs.Select(verb => $"{controller.Name}.{method.Name} [{verb}]"));
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void Resident_dashboard_does_not_expose_claim_diagnostics()
    {
        Assert.DoesNotContain(
            typeof(ResidentHomeController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            method => method.Name == "Debug");
    }

    [Theory]
    [InlineData("resident", AdminPermissions.ViewAuditLogOwn, true)]
    [InlineData("resident", AdminPermissions.ViewAuditLogBuilding, false)]
    [InlineData("admin", AdminPermissions.ViewAuditLogBuilding, true)]
    [InlineData("admin", AdminPermissions.ViewAuditLogOwn, false)]
    [InlineData("superadmin", "any-permission", true)]
    [InlineData("unknown", AdminPermissions.ViewAuditLogOwn, false)]
    public void Audit_log_access_uses_the_role_scoped_permission(string role, string permission, bool expected)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Role, role),
            new Claim("perm", permission)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        Assert.Equal(expected, AuditLogController.HasPermissionForUser(principal));
    }

}
