using System.Security.Claims;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class AuthorizationEvaluatorTests
{
    private static readonly DateTime NowUtc = new(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("deposit.read")]
    [InlineData("deposit.confirm")]
    [InlineData("deposit.cancel")]
    [InlineData("wallet.adjust")]
    [InlineData("expense.read")]
    [InlineData("expense.post")]
    [InlineData("expense.reverse")]
    [InlineData("revenue.read")]
    [InlineData("revenue.post")]
    [InlineData("revenue.reverse")]
    [InlineData("finance.export")]
    [InlineData("finance.period.close")]
    [InlineData("finance.period.reopen")]
    public void PermissionCatalog_ContainsTheApprovedSplitFinanceActions(string permissionKey)
    {
        var catalog = new DefaultAuthorizationPermissionCatalog();

        Assert.True(catalog.TryGet(permissionKey, out var definition));
        Assert.Contains(AuthorizationScopeType.System, definition.AllowedGrantScopes);
        Assert.Contains(AuthorizationScopeType.Building, definition.AllowedGrantScopes);
    }

    [Theory]
    [InlineData("wallet.manage")]
    [InlineData("expenses.manage")]
    [InlineData("revenues.manage")]
    [InlineData("expense.create")]
    [InlineData("expense.update")]
    [InlineData("revenue.create")]
    [InlineData("revenue.update")]
    [InlineData("template.import")]
    [InlineData("tools.manage")]
    [InlineData("sync.manage")]
    public void PermissionCatalog_RejectsBroadOrUnresolvedLegacyKeys(string permissionKey)
    {
        var catalog = new DefaultAuthorizationPermissionCatalog();

        Assert.False(catalog.TryGet(permissionKey, out _));
    }

    [Theory]
    [InlineData("backup.view")]
    [InlineData("backup.create")]
    [InlineData("backup.download")]
    [InlineData("backup.restore")]
    [InlineData("backup.delete")]
    [InlineData("system.health.view")]
    public void PermissionCatalog_SeparatesPrivilegedOperationsAndRequiresFreshMfa(string permissionKey)
    {
        var catalog = new DefaultAuthorizationPermissionCatalog();

        Assert.True(catalog.TryGet(permissionKey, out var definition));
        Assert.True(definition.RequiresFreshMfa);
        Assert.Equal(TimeSpan.FromMinutes(10), definition.MfaFreshnessWindow);
        Assert.Equal(new HashSet<AuthorizationScopeType> { AuthorizationScopeType.System }, definition.AllowedGrantScopes);
    }

    [Fact]
    public void PermissionCatalog_RejectsLegacyBroadBackupPermission()
    {
        var catalog = new DefaultAuthorizationPermissionCatalog();

        Assert.False(catalog.TryGet("backup.manage", out _));
    }

    [Fact]
    public void PermissionCatalog_ApplicationLogsAndSystemHealthAreIndependentPermissions()
    {
        var catalog = new DefaultAuthorizationPermissionCatalog();

        Assert.True(catalog.TryGet("system.logs.view", out var logs));
        Assert.True(catalog.TryGet("system.health.view", out var health));
        Assert.NotSame(logs, health);
        Assert.NotEqual(logs.Key, health.Key);
    }

    [Fact]
    public void PermissionCatalog_ApartmentAccessManagementRequiresMfaAtBuildingOrApartmentScope()
    {
        var catalog = new DefaultAuthorizationPermissionCatalog();

        Assert.True(catalog.TryGet("apartment.access.grant.manage", out var definition));
        Assert.True(definition.RequiresFreshMfa);
        Assert.Equal(TimeSpan.FromMinutes(10), definition.MfaFreshnessWindow);
        Assert.Contains(AuthorizationScopeType.Building, definition.AllowedGrantScopes);
        Assert.Contains(AuthorizationScopeType.Apartment, definition.AllowedGrantScopes);
        Assert.DoesNotContain(AuthorizationScopeType.System, definition.AllowedGrantScopes);
    }

    [Fact]
    public void PermissionCatalog_IdentityMappingReviewRequiresSystemScopeAndFreshMfa()
    {
        var catalog = new DefaultAuthorizationPermissionCatalog();

        Assert.True(catalog.TryGet("identity.mapping.review", out var definition));
        Assert.True(definition.RequiresFreshMfa);
        Assert.Equal(new HashSet<AuthorizationScopeType> { AuthorizationScopeType.System }, definition.AllowedGrantScopes);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesUnauthenticatedPrincipal()
    {
        await using var db = CreateContext();
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "buildings.manage",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.Deny, result.Decision);
        Assert.Equal(AuthorizationReason.NotAuthenticated, result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesUnknownPermission()
    {
        await using var db = CreateContext();
        var accountId = Guid.NewGuid();
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(accountId, "stamp"),
            "legacy.all.power",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.Deny, result.Decision);
        Assert.Equal(AuthorizationReason.UnknownPermission, result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_ExplicitDenyWinsOverAllow()
    {
        await using var db = CreateContext();
        var actor = await SeedAccountWithGrantsAsync(db,
            ("allow-role", AuthorizationEffect.Allow),
            ("deny-role", AuthorizationEffect.Deny));
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(actor.AccountId, actor.SecurityStamp),
            "buildings.manage",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.Deny, result.Decision);
        Assert.Equal(AuthorizationReason.ExplicitDeny, result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_RequiresStepUpOnlyWhenSensitivePermissionHasAnAllow()
    {
        await using var db = CreateContext();
        var actor = await SeedAccountWithGrantsAsync(db,
            ("audit-role", AuthorizationEffect.Allow));
        db.UserTwoFactorSettings.Add(new UserTwoFactorSettingEntity
        {
            AccountId = actor.AccountId,
            IsEnabled = true,
            SecretCiphertext = System.Text.Encoding.UTF8.GetBytes("test-ciphertext"),
            KeyVersion = "test",
            EnrolledAtUtc = NowUtc.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(actor.AccountId, actor.SecurityStamp),
            "audit.view.all",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.RequiresStepUp, result.Decision);
        Assert.Equal(AuthorizationReason.StepUpRequired, result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_AllowsSensitivePermissionWithFreshMfaProof()
    {
        await using var db = CreateContext();
        var actor = await SeedAccountWithGrantsAsync(db,
            ("audit-role", AuthorizationEffect.Allow));
        db.UserTwoFactorSettings.Add(new UserTwoFactorSettingEntity
        {
            AccountId = actor.AccountId,
            IsEnabled = true,
            SecretCiphertext = System.Text.Encoding.UTF8.GetBytes("test-ciphertext"),
            KeyVersion = "test",
            EnrolledAtUtc = NowUtc.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(actor.AccountId, actor.SecurityStamp, NowUtc.AddMinutes(-5)),
            "audit.view.all",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.Allow, result.Decision);
        Assert.Equal(AuthorizationReason.Granted, result.Reason);
    }

    [Theory]
    [InlineData(-11)]
    [InlineData(1)]
    public async Task EvaluateAsync_RequiresStepUpWhenMfaProofIsExpiredOrFromTheFuture(int minutesFromNow)
    {
        await using var db = CreateContext();
        var actor = await SeedAccountWithGrantsAsync(db,
            ("audit-role", AuthorizationEffect.Allow));
        db.UserTwoFactorSettings.Add(new UserTwoFactorSettingEntity
        {
            AccountId = actor.AccountId,
            IsEnabled = true,
            SecretCiphertext = System.Text.Encoding.UTF8.GetBytes("test-ciphertext"),
            KeyVersion = "test",
            EnrolledAtUtc = NowUtc.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(actor.AccountId, actor.SecurityStamp, NowUtc.AddMinutes(minutesFromNow)),
            "audit.view.all",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.RequiresStepUp, result.Decision);
        Assert.Equal(AuthorizationReason.StepUpRequired, result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_AllowsActiveSystemGrantWithCurrentSecurityStamp()
    {
        await using var db = CreateContext();
        var actor = await SeedAccountWithGrantsAsync(db,
            ("buildings-role", AuthorizationEffect.Allow));
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(actor.AccountId, actor.SecurityStamp),
            "buildings.manage",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.Allow, result.Decision);
        Assert.Equal(AuthorizationReason.Granted, result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesWhenSecurityStampIsStale()
    {
        await using var db = CreateContext();
        var actor = await SeedAccountWithGrantsAsync(db,
            ("buildings-role", AuthorizationEffect.Allow));
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(actor.AccountId, "old-stamp"),
            "buildings.manage",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.Deny, result.Decision);
        Assert.Equal(AuthorizationReason.StaleSecurityStamp, result.Reason);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesSensitivePermissionWhenTargetMfaIsNotEnrolled()
    {
        await using var db = CreateContext();
        var actor = await SeedAccountWithGrantsAsync(db,
            ("audit-role", AuthorizationEffect.Allow));
        var evaluator = new AuthorizationEvaluator(db, new DefaultAuthorizationPermissionCatalog());

        var result = await evaluator.EvaluateAsync(
            Principal(actor.AccountId, actor.SecurityStamp),
            "audit.view.all",
            new ResolvedAuthorizationResource(AuthorizationScopeType.System),
            NowUtc);

        Assert.Equal(AuthorizationDecision.Deny, result.Decision);
        Assert.Equal(AuthorizationReason.MfaNotEnrolled, result.Reason);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(Guid AccountId, string SecurityStamp)> SeedAccountWithGrantsAsync(
        AppDbContext db,
        params (string RoleKey, AuthorizationEffect Effect)[] grants)
    {
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var stamp = "security-stamp-current";
        db.Persons.Add(new PersonEntity
        {
            PersonId = personId,
            DisplayName = "Test person",
            CreatedAtUtc = NowUtc
        });
        db.UserAccounts.Add(new UserAccountEntity
        {
            AccountId = accountId,
            PersonId = personId,
            AccountStatus = 1,
            SecurityStamp = stamp,
            CreatedAtUtc = NowUtc
        });

        foreach (var grant in grants)
        {
            var permission = grant.RoleKey == "audit-role" ? "audit.view.all" : "buildings.manage";
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = grant.RoleKey,
                DisplayName = grant.RoleKey,
                IsActive = true
            });
            db.RolePermissions.Add(new RolePermissionEntity
            {
                RoleKey = grant.RoleKey,
                PermissionKey = permission,
                Effect = (byte)grant.Effect
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(),
                AccountId = accountId,
                RoleKey = grant.RoleKey,
                ScopeType = (byte)AuthorizationScopeType.System,
                ValidFromUtc = NowUtc.AddDays(-1),
                GrantedAtUtc = NowUtc.AddDays(-1)
            });
        }

        await db.SaveChangesAsync();
        return (accountId, stamp);
    }

    private static ClaimsPrincipal Principal(Guid accountId, string stamp, DateTime? mfaVerifiedAtUtc = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId.ToString("D")),
            new("securityStamp", stamp)
        };
        if (mfaVerifiedAtUtc.HasValue)
            claims.Add(new Claim("mfaVerifiedAtUtc", mfaVerifiedAtUtc.Value.ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
