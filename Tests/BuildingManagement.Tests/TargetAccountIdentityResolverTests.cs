using System.Security.Claims;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class TargetAccountIdentityResolverTests
{
    [Fact]
    public async Task ResolveAsync_UsesTargetAccountSubjectWithoutEmailClaim()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        await using (var seed = new AppDbContext(options))
        {
            seed.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Phone Login Resident",
                CreatedAtUtc = DateTime.UtcNow
            });
            seed.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 1,
                SecurityStamp = "test-stamp",
                CreatedAtUtc = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, accountId.ToString("D")) }, "test"));
        var resolver = new TargetAccountIdentityResolver(new TestContextFactory(options));

        Assert.Equal(accountId, await resolver.ResolveAsync(principal));
    }

    [Fact]
    public async Task ResolveAsync_RejectsInactiveOrUnknownTargetSubjectWithoutEmailFallback()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        await using (var seed = new AppDbContext(options))
        {
            seed.Persons.Add(new PersonEntity
            {
                PersonId = personId,
                DisplayName = "Inactive Resident",
                CreatedAtUtc = DateTime.UtcNow
            });
            seed.UserAccounts.Add(new UserAccountEntity
            {
                AccountId = accountId,
                PersonId = personId,
                AccountStatus = 0,
                NormalizedEmail = "INACTIVE@EXAMPLE.INVALID",
                SecurityStamp = "test-stamp",
                CreatedAtUtc = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString("D")),
            new Claim(ClaimTypes.Email, "INACTIVE@EXAMPLE.INVALID")
        }, "test"));
        var resolver = new TargetAccountIdentityResolver(new TestContextFactory(options));

        Assert.Null(await resolver.ResolveAsync(principal));
    }

    private sealed class TestContextFactory(DbContextOptions<AppDbContext> options)
        : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() => new(options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AppDbContext(options));
    }
}
