using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BuildingManagement.Tests;

public sealed class SettingsServiceOwnSettingsTests
{
    [Fact]
    public async Task GetOwnAsync_LoadsCanonicalIdentityAndAccountPreferences()
    {
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var factory = CreateFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                LegalName = "Legal Name",
                DisplayName = "Display Name",
                PrimaryEmail = "person@example.test",
                PrimaryPhone = "+201000000001",
                Whatsapp = "+201000000002",
                PhotoUrl = "https://example.test/avatar.png",
                CreatedAtUtc = DateTime.UtcNow
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                NormalizedEmail = "LOGIN@EXAMPLE.TEST",
                ExternalProvider = "Google",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
                LastLoginAtUtc = DateTime.UtcNow.AddHours(-1),
                MustChangePin = true
            });
            db.UserPreferences.Add(new UserPreferenceEntity
            {
                PreferenceId = Guid.NewGuid(),
                AccountId = accountId,
                PreferenceKey = "profile.settings.v1",
                PreferenceJson = """{"Language":"fr","NotificationPreferences":{"WhatsApp":false,"Email":true,"InApp":true},"PreferredPaymentMethod":"bank"}""",
                UpdatedAtUtc = DateTime.UtcNow
            });
            db.UserTwoFactorSettings.Add(new UserTwoFactorSettingEntity
            {
                AccountId = accountId,
                IsEnabled = true,
                EnrolledAtUtc = DateTime.UtcNow.AddDays(-2),
                LastVerifiedAtUtc = DateTime.UtcNow.AddMinutes(-2)
            });
            db.UserRecoveryCodes.Add(new UserRecoveryCodeEntity
            {
                RecoveryCodeId = Guid.NewGuid(),
                AccountId = accountId,
                CodeHash = new byte[] { 1, 2, 3 },
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var settings = await service.GetOwnAsync(accountId.ToString(), "admin");

        Assert.Equal("Legal Name", settings.LegalName);
        Assert.Equal("Display Name", settings.DisplayName);
        Assert.Equal("person@example.test", settings.PrimaryEmail);
        Assert.Equal("+201000000001", settings.PrimaryPhone);
        Assert.Equal("+201000000002", settings.Whatsapp);
        Assert.Equal("https://example.test/avatar.png", settings.PhotoUrl);
        Assert.Equal("LOGIN@EXAMPLE.TEST", settings.LoginEmail);
        Assert.Equal("Google", settings.ExternalProvider);
        Assert.True(settings.MustChangePin);
        Assert.True(settings.TwoFactorEnabled);
        Assert.Equal(1, settings.UnusedRecoveryCodes);
        Assert.Equal("fr", settings.Language);
        Assert.False(settings.NotificationPreferences.WhatsApp);
        Assert.True(settings.NotificationPreferences.Email);
        Assert.Equal("bank", settings.PreferredPaymentMethod);
    }

    [Fact]
    public async Task SaveOwnAsync_PersistsPreferencesForNonResidentAccounts()
    {
        var accountId = Guid.NewGuid();
        var factory = CreateFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            var personId = Guid.NewGuid();
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Admin",
                CreatedAtUtc = DateTime.UtcNow
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        await service.SaveOwnAsync(accountId.ToString(), new OwnSettings
        {
            Language = "de",
            PreferredPaymentMethod = "cash",
            NotificationPreferences = new NotificationPreferences
            {
                WhatsApp = false,
                Email = true,
                InApp = false
            }
        }, accountId.ToString(), "admin");

        var saved = await service.GetOwnAsync(accountId.ToString(), "admin");
        Assert.Equal("de", saved.Language);
        Assert.Equal("cash", saved.PreferredPaymentMethod);
        Assert.False(saved.NotificationPreferences.WhatsApp);
        Assert.True(saved.NotificationPreferences.Email);
        Assert.False(saved.NotificationPreferences.InApp);
    }

    [Fact]
    public async Task SaveOwnAsync_RejectsUnsupportedLanguageWithoutPersisting()
    {
        var accountId = Guid.NewGuid();
        var factory = CreateFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            var personId = Guid.NewGuid();
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Admin",
                CreatedAtUtc = DateTime.UtcNow
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SaveOwnAsync(
            accountId.ToString(),
            new OwnSettings { Language = "xx" },
            accountId.ToString(),
            "admin"));

        await using var verify = await factory.CreateDbContextAsync();
        Assert.Empty(await verify.UserPreferences.ToListAsync());
    }

    [Fact]
    public async Task UpdateOwnIdentityProfileAsync_UpdatesSuperAdminIdentityFields()
    {
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var factory = CreateFactory();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Original Name",
                LegalName = "Original Legal Name",
                PrimaryEmail = "admin@example.test",
                PrimaryPhone = "+201000000001",
                CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "superadmin",
                DisplayName = "Super Admin",
                IsSystemRole = true,
                IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(),
                AccountId = accountId,
                RoleKey = "superadmin",
                ScopeType = 0,
                ValidFromUtc = now.AddDays(-1),
                GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        await service.UpdateOwnIdentityProfileAsync(accountId.ToString(), "superadmin", null, null,
            new OwnSettings
            {
                DisplayName = "Updated Name",
                LegalName = "Updated Legal Name",
                PrimaryEmail = "admin@example.test",
                PrimaryPhone = "+201000000001",
                PhotoUrl = "https://example.test/avatar.png"
            }, null, null);

        await using var verify = await factory.CreateDbContextAsync();
        var person = await verify.Persons.SingleAsync(item => item.PersonId == personId);
        Assert.Equal("Updated Name", person.DisplayName);
        Assert.Equal("Updated Legal Name", person.LegalName);
        Assert.Equal("https://example.test/avatar.png", person.PhotoUrl);
        Assert.NotNull(person.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateOwnIdentityProfileAsync_RequiresFreshMfaForSuperAdminContactChanges()
    {
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var factory = CreateFactory();
        var now = DateTime.UtcNow;
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Original Name",
                PrimaryEmail = "admin@example.test",
                PrimaryPhone = "+201000000001",
                CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = now
            });
            db.RoleDefinitions.Add(new RoleDefinitionEntity
            {
                RoleKey = "superadmin",
                DisplayName = "Super Admin",
                IsSystemRole = true,
                IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(),
                AccountId = accountId,
                RoleKey = "superadmin",
                ScopeType = 0,
                ValidFromUtc = now.AddDays(-1),
                GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateOwnIdentityProfileAsync(
            accountId.ToString(), "superadmin", null, null,
            new OwnSettings
            {
                DisplayName = "Original Name",
                PrimaryEmail = "new-admin@example.test",
                PrimaryPhone = "+201000000001"
            }, null, null));

        await using var verify = await factory.CreateDbContextAsync();
        var person = await verify.Persons.SingleAsync(item => item.PersonId == personId);
        Assert.Equal("admin@example.test", person.PrimaryEmail);
    }

    [Fact]
    public async Task UpdateOwnIdentityProfileAsync_AcceptsSixDigitPinAndRevokesPreviousCredential()
    {
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var oldCredentialId = Guid.NewGuid();
        var factory = CreateFactory();
        var hasher = new BcryptPasswordHasher();
        var now = DateTime.UtcNow;

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId, DisplayName = "Building Admin",
                PrimaryPhone = "+19995550123", PrimaryEmail = "admin@example.test",
                CreatedAtUtc = now
            });
            db.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId, PersonId = personId, AccountStatus = 1,
                SecurityStamp = "before-pin-change", CreatedAtUtc = now
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(), AccountId = accountId, RoleKey = "admin",
                ScopeType = 1, ScopeBuildingId = "building-1",
                ValidFromUtc = now.AddDays(-1), GrantedAtUtc = now.AddDays(-1)
            });
            db.UserCredentials.Add(new UserCredentialEntity
            {
                CredentialId = oldCredentialId, AccountId = accountId, CredentialType = 0,
                CredentialHash = hasher.HashPin(new string('1', 6)), HashAlgorithm = "BCrypt.Net-Next",
                CreatedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = CreateService(factory, hasher);
        var nextPin = new string('2', 6);
        await service.UpdateOwnIdentityProfileAsync(
            accountId.ToString(), "admin", "building-1", null,
            new OwnSettings
            {
                DisplayName = "Building Admin",
                PrimaryEmail = "admin@example.test",
                PrimaryPhone = "+19995550123"
            },
            nextPin, new string('1', 6));

        await using var verify = await factory.CreateDbContextAsync();
        var account = await verify.UserAccounts.SingleAsync(item => item.AccountId == accountId);
        Assert.False(account.MustChangePin);
        Assert.NotEqual("before-pin-change", account.SecurityStamp);
        Assert.NotNull((await verify.UserCredentials.SingleAsync(item => item.CredentialId == oldCredentialId)).RevokedAtUtc);
        var activeCredential = await verify.UserCredentials.SingleAsync(
            item => item.AccountId == accountId && item.RevokedAtUtc == null);
        Assert.True(hasher.VerifyPin(nextPin, activeCredential.CredentialHash));
    }

    private static SettingsService CreateService(
        IDbContextFactory<AppDbContext> factory, IPasswordHasher? hasher = null)
    {
        var audit = Substitute.For<IAuditLogger>();
        audit.LogAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<object?>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        return new SettingsService(
            factory,
            audit,
            buildings: null!,
            encryption: null!,
            hasher: hasher ?? null!,
            logger: NullLogger<SettingsService>.Instance);
    }

    private static IDbContextFactory<AppDbContext> CreateFactory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new TestDbContextFactory(options);
    }

    private sealed class TestDbContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(options));
    }
}
