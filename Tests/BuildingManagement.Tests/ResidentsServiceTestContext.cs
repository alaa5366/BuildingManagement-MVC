using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BuildingManagement.Tests;

internal sealed class ResidentsServiceTestContext : IAsyncDisposable
{
    private readonly DbContextOptions<AppDbContext> _options;
    public AppDbContext Db { get; }
    public ResidentsService Service { get; }

    public ResidentsServiceTestContext()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        Db = new AppDbContext(_options);
        var factory = new TestFactory(_options);
        Service = new ResidentsService(factory, new Mock<IAuditLogger>().Object, new BcryptPasswordHasher(), NullLogger<ResidentsService>.Instance);
    }

    public async Task SeedApartmentAsync(string buildingId = "b1", string apartmentId = "a1",
        string owner = "Owner", string phone = "01001234567")
    {
        await using var db = CreateDb();
        if (!await db.Buildings.AnyAsync(item => item.Id == buildingId))
            db.Buildings.Add(new BuildingEntity { Id = buildingId, Name = "Test Building", BuildingNumber = buildingId });
        if (!await db.Floors.AnyAsync(item => item.BuildingId == buildingId && item.Id == "f1"))
            db.Floors.Add(new FloorEntity { BuildingId = buildingId, Id = "f1", Label = "Floor 1", SortOrder = 1 });
        if (!await db.Apartments.AnyAsync(item => item.BuildingId == buildingId && item.Id == apartmentId))
            db.Apartments.Add(new ApartmentEntity
            {
                BuildingId = buildingId, Id = apartmentId, FloorId = "f1",
                Number = 1, Owner = owner, Phone = phone, Email = "owner@example.test"
            });
        await db.SaveChangesAsync();
    }

    public AppDbContext CreateDb() => new(_options);

    public async ValueTask DisposeAsync() => await Db.DisposeAsync();

    private sealed class TestFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public TestFactory(DbContextOptions<AppDbContext> options) => _options = options;
        public AppDbContext CreateDbContext() => new(_options);
        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());
    }
}
