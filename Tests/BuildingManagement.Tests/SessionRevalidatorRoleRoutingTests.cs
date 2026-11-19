using System.Security.Claims;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class SessionRevalidatorRoleRoutingTests
{
    private sealed class UnavailableFactory : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => throw new InvalidOperationException("Legacy lookup must not be reached.");
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<AppDbContext>(new InvalidOperationException("Legacy lookup must not be reached."));
    }

    private static SessionRevalidator CreateValidator() =>
        new(new UnavailableFactory(), new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SessionRevalidator>.Instance);

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Cookies"));

    [Fact]
    public async Task AllowListedEmailWithResidentRole_DoesNotBypassTargetIdentityRequirement()
    {
        SuperAdminIdentityPolicy.Configure("allow-listed@example.test");
        var principal = Principal(
            new Claim(ClaimTypes.Role, "resident"),
            new Claim(ClaimTypes.Email, "allow-listed@example.test"),
            new Claim(ClaimTypes.NameIdentifier, "legacy-resident"));
        Assert.False((await CreateValidator().ValidateAsync(principal)).Valid);
    }

    [Fact]
    public async Task LegacySuperAdminRoleWithoutTargetMarker_IsRejected()
    {
        var principal = Principal(
            new Claim(ClaimTypes.Role, "superadmin"),
            new Claim(ClaimTypes.Email, "root@example.test"),
            new Claim(ClaimTypes.NameIdentifier, "legacy-superadmin"));
        Assert.False((await CreateValidator().ValidateAsync(principal)).Valid);
    }

    [Fact]
    public async Task OriginalSuperAdminClaimAlone_DoesNotPreserveLegacySession()
    {
        var principal = Principal(
            new Claim("originalRole", "superadmin"),
            new Claim(ClaimTypes.Role, "resident"),
            new Claim(ClaimTypes.NameIdentifier, "legacy-resident"));
        Assert.False((await CreateValidator().ValidateAsync(principal)).Valid);
    }

    [Fact]
    public void SuperAdminSecurityStamp_ChangesWhenCredentialOrAccountStateChanges()
    {
        var updatedAt = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
        var original = SuperAdminIdentityPolicy.CreateSecurityStamp("bcrypt-hash-a", updatedAt, true, false);
        var changedPassword = SuperAdminIdentityPolicy.CreateSecurityStamp("bcrypt-hash-b", updatedAt, true, false);
        var changedStatus = SuperAdminIdentityPolicy.CreateSecurityStamp("bcrypt-hash-a", updatedAt, false, true);

        Assert.NotEqual(original, changedPassword);
        Assert.NotEqual(original, changedStatus);
        Assert.DoesNotContain("bcrypt-hash-a", original, StringComparison.Ordinal);
    }
}
