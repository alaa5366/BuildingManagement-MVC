using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public sealed class LanguageAdministrationCsvExportTests
{
    [Fact]
    public async Task ExportCsvAsync_SanitizesFormulaPrefixesInAllUntrustedTextColumns()
    {
        var factory = new TestContextFactory();
        await using (var db = factory.CreateDbContext())
        {
            db.Languages.Add(new LanguageEntity
            {
                CultureCode = "en-US",
                EnglishName = "English",
                NativeName = "English",
                Direction = "LTR",
                IsEnabled = true,
                IsDefault = true
            });
            db.TranslationEntries.Add(new TranslationEntryEntity
            {
                Key = "=1+1",
                Category = "@SUM(A1:A2)",
                SourceGroup = "\u200B=1+1",
                Description = "+cmd|' /C calc'!A0",
                IsArchived = false,
                UpdatedAtUtc = DateTime.UtcNow
            });
            db.TranslationValues.Add(new TranslationValueEntity
            {
                Key = "=1+1",
                CultureCode = "en-US",
                Value = "\uFEFF=1+1",
                UpdatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var service = new LanguageAdministrationService(
            factory,
            new Mock<IAuditLogger>().Object,
            NullLogger<LanguageAdministrationService>.Instance);

        var csv = await service.ExportCsvAsync(CancellationToken.None);

        Assert.Contains("\"'=1+1\"", csv);
        Assert.Contains("\"'@SUM(A1:A2)\"", csv);
        Assert.Contains("\"'\u200B=1+1\"", csv);
        Assert.Contains("\"'+cmd|' /C calc'!A0\"", csv);
        Assert.Contains("\"'\uFEFF=1+1\"", csv);
    }

    private sealed class TestContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        public AppDbContext CreateDbContext() => new(_options);

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
