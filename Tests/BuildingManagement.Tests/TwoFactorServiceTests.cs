using System.Security.Cryptography;
using System.Text;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TotpAuthenticatorTests
{
    [Fact]
    public void Rfc6238_sha1_vector_matches_six_digit_code()
    {
        var secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(59);
        Assert.Equal("287082", TotpAuthenticator.GenerateCode(secret, timestamp));
    }

    [Fact]
    public void Base32_secret_round_trips()
    {
        var bytes = Enumerable.Range(0, 20).Select(i => (byte)i).ToArray();
        Assert.Equal(bytes, TotpAuthenticator.DecodeBase32(TotpAuthenticator.EncodeBase32(bytes)));
    }

    [Fact]
    public void Verification_accepts_current_window_and_rejects_malformed_codes()
    {
        var secret = TotpAuthenticator.GenerateSecret();
        var now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var code = TotpAuthenticator.GenerateCode(secret, now);
        Assert.True(TotpAuthenticator.TryVerify(secret, code, now, out var step));
        Assert.Equal(now.ToUnixTimeSeconds() / 30, step);
        Assert.False(TotpAuthenticator.TryVerify(secret, "12AB56", now, out _));
        Assert.False(TotpAuthenticator.TryVerify(secret, "12345", now, out _));
    }
}

public sealed class TwoFactorServiceTests
{
    private sealed class TestFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestFactory(string name) => _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }

    private sealed class TestEncryption : IEncryptionService
    {
        public string Encrypt(string plain) => "test:" + plain;
        public string Decrypt(string cipher) => cipher.StartsWith("test:", StringComparison.Ordinal)
            ? cipher[5..] : throw new InvalidOperationException("Invalid test ciphertext.");
    }

    private static async Task<(TwoFactorService Service, TestFactory Factory, Guid AccountId)> CreateAsync(string roleKey = "admin")
    {
        var factory = new TestFactory(Guid.NewGuid().ToString("N"));
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await using (var db = factory.CreateDbContext())
        {
            db.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Target Admin",
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
                RoleKey = roleKey, DisplayName = roleKey, IsActive = true
            });
            db.UserRoleAssignments.Add(new UserRoleAssignmentEntity
            {
                RoleAssignmentId = Guid.NewGuid(),
                AccountId = accountId,
                RoleKey = roleKey,
                ScopeType = (byte)(roleKey == "superadmin" ? 0 : 1),
                ScopeBuildingId = roleKey == "superadmin" ? null : "building-1",
                ValidFromUtc = now.AddDays(-1),
                GrantedAtUtc = now.AddDays(-1)
            });
            await db.SaveChangesAsync();
        }

        var service = new TwoFactorService(factory, new TestEncryption(), NullLogger<TwoFactorService>.Instance);
        return (service, factory, accountId);
    }

    [Fact]
    public async Task Target_identity_admin_reads_mfa_state_from_target_tables()
    {
        var (service, _, accountId) = await CreateAsync();
        var secret = TwoFactorService.CreateSecret();
        var code = TotpAuthenticator.GenerateCode(secret, DateTimeOffset.UtcNow);
        Assert.NotNull(await service.EnableAsync(accountId.ToString("D"), secret, code));
        Assert.True(await service.IsEnabledAsync(accountId.ToString("D")));
    }

    [Fact]
    public async Task Super_admin_can_enroll_in_mfa_using_the_target_identity_tables()
    {
        var (service, _, accountId) = await CreateAsync("superadmin");
        var secret = TwoFactorService.CreateSecret();
        var code = TotpAuthenticator.GenerateCode(secret, DateTimeOffset.UtcNow);

        var recovery = await service.EnableAsync(accountId.ToString("D"), secret, code);

        Assert.NotNull(recovery);
        Assert.Equal(10, recovery!.Count);
        Assert.True(await service.IsEnabledAsync(accountId.ToString("D")));
    }

    [Fact]
    public async Task Enrollment_encrypts_secret_and_stores_only_recovery_hashes()
    {
        var (service, factory, accountId) = await CreateAsync();
        var secret = TwoFactorService.CreateSecret();
        var code = TotpAuthenticator.GenerateCode(secret, DateTimeOffset.UtcNow);
        var recovery = await service.EnableAsync(accountId.ToString("D"), secret, code);

        Assert.NotNull(recovery);
        Assert.Equal(10, recovery!.Count);
        Assert.True(await service.IsEnabledAsync(accountId.ToString("D")));
        await using var db = factory.CreateDbContext();
        var setting = await db.UserTwoFactorSettings.SingleAsync(x => x.AccountId == accountId);
        Assert.StartsWith("test:", Encoding.UTF8.GetString(setting.SecretCiphertext!));
        var storedHashes = await db.UserRecoveryCodes.Where(x => x.AccountId == accountId)
            .Select(x => x.CodeHash).ToListAsync();
        Assert.Equal(10, storedHashes.Count);
        var normalizedRecovery = new string(recovery[0].Where(char.IsLetterOrDigit).ToArray());
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedRecovery));
        Assert.Contains(storedHashes, hash => CryptographicOperations.FixedTimeEquals(hash, expectedHash));
        Assert.All(storedHashes, hash => Assert.Equal(32, hash.Length));
    }

    [Fact]
    public async Task TOTP_code_used_for_enrollment_cannot_be_replayed()
    {
        var (service, _, accountId) = await CreateAsync();
        var secret = TwoFactorService.CreateSecret();
        var code = TotpAuthenticator.GenerateCode(secret, DateTimeOffset.UtcNow);
        Assert.NotNull(await service.EnableAsync(accountId.ToString("D"), secret, code));
        Assert.False(await service.VerifyAndConsumeAsync(accountId.ToString("D"), code));
    }

    [Fact]
    public async Task Recovery_code_is_consumed_once()
    {
        var (service, _, accountId) = await CreateAsync();
        var secret = TwoFactorService.CreateSecret();
        var code = TotpAuthenticator.GenerateCode(secret, DateTimeOffset.UtcNow);
        var recovery = await service.EnableAsync(accountId.ToString("D"), secret, code);
        Assert.NotNull(recovery);
        Assert.True(await service.VerifyAndConsumeAsync(accountId.ToString("D"), recovery![0]));
        Assert.False(await service.VerifyAndConsumeAsync(accountId.ToString("D"), recovery[0]));
    }

    [Fact]
    public async Task Recovery_code_cannot_disable_mandatory_admin_mfa_if_encrypted_secret_is_unreadable()
    {
        var (service, factory, accountId) = await CreateAsync();
        var secret = TwoFactorService.CreateSecret();
        var code = TotpAuthenticator.GenerateCode(secret, DateTimeOffset.UtcNow);
        var recovery = await service.EnableAsync(accountId.ToString("D"), secret, code);
        Assert.NotNull(recovery);

        await using (var db = factory.CreateDbContext())
        {
            var setting = await db.UserTwoFactorSettings.SingleAsync(x => x.AccountId == accountId);
            setting.SecretCiphertext = Encoding.UTF8.GetBytes("damaged-ciphertext");
            await db.SaveChangesAsync();
        }

        Assert.False(await service.DisableAsync(accountId.ToString("D"), recovery![0]));
        Assert.True(await service.IsEnabledAsync(accountId.ToString("D")));
        Assert.True(await service.VerifyAndConsumeAsync(accountId.ToString("D"), recovery[0]));
    }

    [Fact]
    public async Task Active_administrator_cannot_disable_mfa_even_with_a_valid_recovery_code()
    {
        var (service, factory, accountId) = await CreateAsync();
        var secret = TwoFactorService.CreateSecret();
        var code = TotpAuthenticator.GenerateCode(secret, DateTimeOffset.UtcNow);
        var recovery = await service.EnableAsync(accountId.ToString("D"), secret, code);
        Assert.NotNull(recovery);
        Assert.False(await service.DisableAsync(accountId.ToString("D"), recovery![0]));
        Assert.True(await service.IsEnabledAsync(accountId.ToString("D")));
        Assert.True(await service.VerifyAndConsumeAsync(accountId.ToString("D"), recovery[0]));
        await using var db = factory.CreateDbContext();
        var setting = await db.UserTwoFactorSettings.SingleAsync(x => x.AccountId == accountId);
        Assert.True(setting.IsEnabled);
        Assert.NotNull(setting.SecretCiphertext);
        Assert.NotNull(setting.KeyVersion);
    }
}
