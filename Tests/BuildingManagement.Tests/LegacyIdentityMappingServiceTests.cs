using System.Globalization;
using System.Security.Claims;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class LegacyIdentityMappingServiceTests
{
    [Fact]
    public async Task ReviewAsync_RejectsUserAccountThatBelongsToAnotherPerson()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var reviewer = await SeedReviewerAsync(factory, now);
        var targetPersonId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.AddRange(Person(targetPersonId, now), Person(otherPersonId, now));
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = targetAccountId,
                PersonId = otherPersonId,
                AccountStatus = 1,
                SecurityStamp = "target-account-stamp",
                CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory, now);
        var actor = Principal(reviewer.AccountId, reviewer.SecurityStamp, now);
        var mappingId = await service.RegisterPendingAsync(actor, "Users", "legacy-user-42");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReviewAsync(
            actor,
            new ReviewLegacyIdentityMappingRequest(
                mappingId,
                LegacyIdentityMappingStatus.Linked,
                targetPersonId,
                targetAccountId,
                "manual-review",
                "evidence:case-42",
                "Verified against authoritative records.")));

        await using var verify = await factory.CreateDbContextAsync();
        var mapping = await verify.LegacyIdentityMaps.SingleAsync(item => item.LegacyIdentityMapId == mappingId);
        Assert.Equal((byte)LegacyIdentityMappingStatus.Pending, mapping.MappingStatus);
        Assert.Null(mapping.PersonId);
        Assert.Null(mapping.AccountId);
        Assert.Null(mapping.ReviewedByAccountId);
    }

    [Fact]
    public async Task ReviewAsync_RecordsValidUserMappingAndAppendOnlyAudit()
    {
        var factory = new TestContextFactory();
        var now = DateTime.UtcNow;
        var reviewer = await SeedReviewerAsync(factory, now);
        var targetPersonId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(Person(targetPersonId, now));
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = targetAccountId,
                PersonId = targetPersonId,
                AccountStatus = 1,
                SecurityStamp = "target-account-stamp",
                CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory, now);
        var actor = Principal(reviewer.AccountId, reviewer.SecurityStamp, now);
        var mappingId = await service.RegisterPendingAsync(actor, "Users", "legacy-user-43");
        await service.ReviewAsync(
            actor,
            new ReviewLegacyIdentityMappingRequest(
                mappingId,
                LegacyIdentityMappingStatus.Linked,
                targetPersonId,
                targetAccountId,
                "manual-review",
                "evidence:case-43",
                "Verified against authoritative records."));

        await using var verify = await factory.CreateDbContextAsync();
        var mapping = await verify.LegacyIdentityMaps.SingleAsync(item => item.LegacyIdentityMapId == mappingId);
        Assert.Equal((byte)LegacyIdentityMappingStatus.Linked, mapping.MappingStatus);
        Assert.Equal(targetPersonId, mapping.PersonId);
        Assert.Equal(targetAccountId, mapping.AccountId);
        Assert.Equal(reviewer.AccountId, mapping.ReviewedByAccountId);
        Assert.NotNull(mapping.ReviewedAtUtc);
        Assert.Equal(2, await verify.AuditLog.CountAsync());
    }

    private static LegacyIdentityMappingService CreateService(TestContextFactory factory, DateTime now)
    {
        var evaluatorContext = factory.CreateDbContext();
        var evaluator = new AuthorizationEvaluator(evaluatorContext, new DefaultAuthorizationPermissionCatalog());
        return new LegacyIdentityMappingService(factory, evaluator, new FixedTimeProvider(now));
    }

    private static async Task<(Guid AccountId, string SecurityStamp)> SeedReviewerAsync(
        TestContextFactory factory,
        DateTime now)
    {
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        const string stamp = "reviewer-security-stamp";

        await using var db = await factory.CreateDbContextAsync();
        db.Persons.Add(Person(personId, now));
        db.UserAccounts.Add(new UserAccountEntity
        {
            AccountId = accountId,
            PersonId = personId,
            AccountStatus = 1,
            SecurityStamp = stamp,
            CreatedAtUtc = now
        });
        db.UserTwoFactorSettings.Add(new UserTwoFactorSettingEntity
        {
            AccountId = accountId,
            IsEnabled = true,
            EnrolledAtUtc = now,
            LastVerifiedAtUtc = now
        });
        db.RoleDefinitions.Add(new RoleDefinitionEntity
        {
            RoleKey = "identity-reviewer",
            DisplayName = "Identity reviewer",
            IsActive = true
        });
        db.RolePermissions.Add(new RolePermissionEntity
        {
            RoleKey = "identity-reviewer",
            PermissionKey = "identity.mapping.review",
            Effect = (byte)AuthorizationEffect.Allow
        });
        db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
        {
            RoleAssignmentId = Guid.NewGuid(),
            AccountId = accountId,
            RoleKey = "identity-reviewer",
            ScopeType = (byte)AuthorizationScopeType.System,
            ValidFromUtc = now.AddDays(-1),
            GrantedAtUtc = now.AddDays(-1)
        });
        await db.SaveChangesAsync();
        return (accountId, stamp);
    }

    private static PersonEntity Person(Guid personId, DateTime now) => new()
    {
        PersonId = personId,
        DisplayName = "Mapped identity",
        CreatedAtUtc = now
    };

    private static ClaimsPrincipal Principal(Guid accountId, string stamp, DateTime now) => new(
        new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString("D")),
            new Claim("securityStamp", stamp),
            new Claim("mfaVerifiedAtUtc", now.AddMinutes(-1).ToString("O", CultureInfo.InvariantCulture))
        ],
        authenticationType: "test"));

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
    }

    private sealed class TestContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
