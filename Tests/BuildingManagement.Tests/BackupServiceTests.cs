using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using BuildingManagementMvc.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BuildingManagement.Tests;

public class BackupServiceTests
{
    private readonly BackupService _service;
    private readonly Mock<IAuditLogger> _mockAudit;
    private readonly AppDbContext _db;

    public BackupServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDbContext(options);

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
               .ReturnsAsync(() => new AppDbContext(options));

        _mockAudit = new Mock<IAuditLogger>();
        _service = new BackupService(factory.Object, _mockAudit.Object, NullLogger<BackupService>.Instance);
    }

    // ================================================================
    // GetStatsAsync
    // ================================================================

    [Fact]
    public async Task GetStatsAsync_EmptyDb_ReturnsStatsForAllTables()
    {
        var stats = await _service.GetStatsAsync();

        Assert.NotNull(stats);
        Assert.NotEmpty(stats);
        // لازم يكون فيه stats لكل جدول في KnownTables
        Assert.Contains(stats, s => s.Collection == "buildings");
        Assert.Contains(stats, s => s.Collection == "users");
        Assert.All(stats, s => Assert.Equal(0, s.DocCount));
    }

    [Fact]
    public async Task GetStatsAsync_WithData_ReturnsCorrectCounts()
    {
        // Arrange: نضيف بيانات
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "Test", BuildingNumber = "BLD-001" });
        _db.Users.Add(new UserEntity { Uid = "u1", Email = "a@test.com", Role = "admin" });
        await _db.SaveChangesAsync();

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        var buildingsStat = stats.FirstOrDefault(s => s.Collection == "buildings");
        Assert.NotNull(buildingsStat);
        Assert.Equal(1, buildingsStat.DocCount);

        var usersStat = stats.FirstOrDefault(s => s.Collection == "users");
        Assert.NotNull(usersStat);
        Assert.Equal(1, usersStat.DocCount);
    }

    // ================================================================
    // CreateBackupAsync
    // ================================================================

    [Fact]
    public async Task CreateBackupAsync_EmptyDb_ReturnsValidZip()
    {
        var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCollections);
        Assert.Equal(0, result.TotalDocuments);

        // الزيبي لازم يكون صالح
        using var ms = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms);
        Assert.Contains(zip.Entries, e => e.Name == "buildings.json");
        Assert.Contains(zip.Entries, e => e.Name == "README.md");
    }

    [Fact]
    public async Task CreateBackupAsync_WithData_IncludesAllRows()
    {
        // Arrange
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T1", BuildingNumber = "BLD-001" });
        _db.Buildings.Add(new BuildingEntity { Id = "b2", Name = "T2", BuildingNumber = "BLD-002" });
        await _db.SaveChangesAsync();

        // Act
        var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.TotalDocuments);

        // نتأكد إن الـ JSON فيه البيانات
        using var ms = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms);
        var entry = zip.GetEntry("buildings.json");
        Assert.NotNull(entry);

        using var reader = new StreamReader(entry.Open());
        var json = await reader.ReadToEndAsync();
        Assert.Contains("BLD-001", json);
        Assert.Contains("BLD-002", json);
    }

    [Fact]
    public async Task CreateBackupAsync_MultipleTables_BacksUpAll()
    {
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T", BuildingNumber = "BLD-001" });
        _db.Users.Add(new UserEntity { Uid = "u1", Email = "a@test.com", Role = "admin" });
        await _db.SaveChangesAsync();

        var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings", "users" }, "admin");

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalCollections);
        Assert.Equal(2, result.TotalDocuments);

        using var ms = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(ms);
        Assert.Contains(zip.Entries, e => e.Name == "buildings.json");
        Assert.Contains(zip.Entries, e => e.Name == "users.json");
    }

    [Fact]
    public async Task CreateBackupAsync_UnknownTable_RejectsRequestWithoutCreatingArchive()
    {
        var (bytes, result) = await _service.CreateBackupAsync(new() { "buildings", "unknown_table" }, "admin");

        Assert.False(result.Success);
        Assert.Empty(bytes);
        Assert.Empty(result.Collections);
        Assert.Contains(result.Errors, error => error.Contains("unknown_table", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateBackupAsync_DuplicateCollections_AreWrittenOnlyOnce()
    {
        var (bytes, result) = await _service.CreateBackupAsync(
            new() { "buildings", "buildings" }, "admin");

        Assert.True(result.Success);
        Assert.Equal(1, result.TotalCollections);
        using var stream = new MemoryStream(bytes);
        using var zip = new System.IO.Compression.ZipArchive(stream);
        Assert.Single(zip.Entries.Where(entry => entry.Name == "buildings.json"));
    }

    [Fact]
    public async Task CreateBackupAsync_EmptySelection_IsRejected()
    {
        var (bytes, result) = await _service.CreateBackupAsync(new(), "admin");

        Assert.False(result.Success);
        Assert.Empty(bytes);
        Assert.NotEmpty(result.Errors);
    }

    // ================================================================
    // PreviewRestoreAsync
    // ================================================================

    [Fact]
    public async Task PreviewRestoreAsync_ValidZip_ReturnsPreview()
    {
        // Arrange: نعمل backup الأول
        _db.Buildings.Add(new BuildingEntity { Id = "b1", Name = "T", BuildingNumber = "BLD-001" });
        await _db.SaveChangesAsync();

        var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");
        using var ms = new MemoryStream(bytes);

        // Act
        var preview = await _service.PreviewRestoreAsync(ms, "test.zip", "admin");

        // Assert
        Assert.True(preview.Success);
        Assert.Single(preview.Collections);
        Assert.Equal("buildings", preview.Collections[0].Collection);
        Assert.Equal(1, preview.Collections[0].DocCount);
        Assert.NotNull(preview.TempFilePath);
        Assert.True(File.Exists(preview.TempFilePath));

        // تنظيف
        if (preview.TempFilePath != null) File.Delete(preview.TempFilePath);
    }

    [Fact]
    public async Task RestoreSession_IsBoundToPreviewingUser_AndSingleUse()
    {
        _db.Buildings.Add(new BuildingEntity { Id = "session-b1", Name = "Session Tower", BuildingNumber = "BLD-991" });
        await _db.SaveChangesAsync();
        var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "admin-a");
        using var stream = new MemoryStream(bytes);
        var preview = await _service.PreviewRestoreAsync(stream, "session.zip", "admin-a");
        Assert.True(preview.Success);
        Assert.False(string.IsNullOrWhiteSpace(preview.RestoreSessionId));

        var denied = await _service.RestoreSessionAsync(preview.RestoreSessionId!, new() { "buildings" }, true, "admin-b");
        Assert.False(denied.Success);
        Assert.True(File.Exists(preview.TempFilePath));

        var restored = await _service.RestoreSessionAsync(preview.RestoreSessionId!, new() { "buildings" }, true, "admin-a");
        Assert.True(restored.Success, string.Join(";", restored.Errors));
        Assert.False(File.Exists(preview.TempFilePath));

        var replay = await _service.RestoreSessionAsync(preview.RestoreSessionId!, new() { "buildings" }, true, "admin-a");
        Assert.False(replay.Success);
    }

    [Fact]
    public async Task PreviewRestoreAsync_InvalidZip_ReturnsError()
    {
        using var ms = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });

        var preview = await _service.PreviewRestoreAsync(ms, "bad.zip", "admin");

        Assert.False(preview.Success);
        Assert.NotEmpty(preview.Errors);
    }

    // ================================================================
    // RestoreAsync — الحالي (skeleton، مش بيعمل restore فعلي)
    // ================================================================

    [Fact]
    public async Task RestoreAsync_ValidZip_RestoresBuildings()
    {
        // Arrange
        _db.Buildings.Add(new BuildingManagementMvc.Data.Entities.BuildingEntity
        {
            Id = "b1",
            Name = "T",
            BuildingNumber = "BLD-001"
        });
        await _db.SaveChangesAsync();

        var (bytes, backupResult) = await _service.CreateBackupAsync(new() { "buildings" }, "admin");
        Assert.True(backupResult.Success, "Backup failed: " + string.Join(", ", backupResult.Errors));
        Assert.Equal(1, backupResult.TotalCollections);

        var tempPath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(tempPath, bytes);

        try
        {
            // Act
            var result = await _service.RestoreAsync(tempPath, new() { "buildings" }, overwrite: false, "admin");

            // Assert — الـ Restore لازم يشتغل
            Assert.True(result.Success, "Restore failed: " + string.Join(", ", result.Errors));
            Assert.Equal(1, result.TotalCollections);
            Assert.Contains(result.Details, d => d.Contains("buildings"));
            // الـ b1 موجود بالفعل، فمفروض يتم تخطيه
            Assert.Equal(0, result.Restored);
            Assert.Equal(1, result.Skipped);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task PreviewRestoreAsync_DuplicateCollectionEntries_AreRejected()
    {
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            foreach (var _ in Enumerable.Range(0, 2))
            {
                var entry = zip.CreateEntry("buildings.json");
                using var writer = new StreamWriter(entry.Open());
                await writer.WriteAsync("[]");
            }
        }
        ms.Position = 0;

        var preview = await _service.PreviewRestoreAsync(ms, "duplicate.zip", "phase10-duplicate");

        Assert.False(preview.Success);
        Assert.NotEmpty(preview.Errors);
        Assert.Null(preview.RestoreSessionId);
    }

    [Fact]
    public async Task PreviewRestoreAsync_PathBearingEntry_IsRejected()
    {
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry("../buildings.json");
            using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync("[]");
        }
        ms.Position = 0;

        var preview = await _service.PreviewRestoreAsync(ms, "path-entry.zip", "phase10-path");

        Assert.False(preview.Success);
        Assert.NotEmpty(preview.Errors);
    }

    [Fact]
    public async Task PreviewRestoreAsync_InvalidRowEnvelope_IsRejected()
    {
        using var ms = new MemoryStream();
        using (var zip = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry("buildings.json");
            using var writer = new StreamWriter(entry.Open());
            await writer.WriteAsync("[{\"id\":\"b1\",\"data\":null}]");
        }
        ms.Position = 0;

        var preview = await _service.PreviewRestoreAsync(ms, "invalid-row.zip", "phase10-invalid-row");

        Assert.False(preview.Success);
        Assert.NotEmpty(preview.Errors);
    }

    [Fact]
    public async Task RestoreSession_ModifiedArchive_IsRejectedBeforeRestore()
    {
        _db.Buildings.Add(new BuildingEntity { Id = "phase10-original", Name = "Original", BuildingNumber = "BLD-910" });
        await _db.SaveChangesAsync();

        var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "phase10-tamper");
        using var upload = new MemoryStream(bytes);
        var preview = await _service.PreviewRestoreAsync(upload, "tamper.zip", "phase10-tamper");
        Assert.True(preview.Success);
        Assert.NotNull(preview.TempFilePath);

        await File.WriteAllBytesAsync(preview.TempFilePath!, new byte[] { 1, 2, 3, 4 });

        var result = await _service.RestoreSessionAsync(
            preview.RestoreSessionId!, new() { "buildings" }, overwrite: true, "phase10-tamper");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.False(File.Exists(preview.TempFilePath));
        Assert.True(await _db.Buildings.AnyAsync(x => x.Id == "phase10-original"));
    }

    [Fact]
    public async Task PreviewRestoreAsync_EnforcesPerUserActiveSessionLimit()
    {
        var (bytes, _) = await _service.CreateBackupAsync(new() { "buildings" }, "phase10-session-cap");
        var userId = "phase10-session-cap-" + Guid.NewGuid().ToString("N");

        using var firstUpload = new MemoryStream(bytes);
        var first = await _service.PreviewRestoreAsync(firstUpload, "first.zip", userId);
        using var secondUpload = new MemoryStream(bytes);
        var second = await _service.PreviewRestoreAsync(secondUpload, "second.zip", userId);
        using var thirdUpload = new MemoryStream(bytes);
        var third = await _service.PreviewRestoreAsync(thirdUpload, "third.zip", userId);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.False(third.Success);

        var firstRestore = await _service.RestoreSessionAsync(first.RestoreSessionId!, new() { "buildings" }, false, userId);
        var secondRestore = await _service.RestoreSessionAsync(second.RestoreSessionId!, new() { "buildings" }, false, userId);
        Assert.True(firstRestore.Success);
        Assert.True(secondRestore.Success);
    }

    // ================================================================
    // KnownTables
    // ================================================================

    [Fact]
    public async Task SavedWhatsAppTemplates_BackupAndRestore_RestoresTemplateByStableGuid()
    {
        var templateId = Guid.NewGuid();
        _db.SavedWhatsAppTemplates.Add(new SavedWhatsAppTemplateEntity
        {
            Id = templateId,
            BuildingId = "backup-template-building",
            Name = "Emergency notice",
            Message = "Please contact building management.",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var (bytes, backup) = await _service.CreateBackupAsync(
            new() { "savedWhatsAppTemplates" }, "backup-template-test");

        Assert.True(backup.Success, string.Join("; ", backup.Errors));
        Assert.Equal(1, backup.TotalDocuments);

        _db.SavedWhatsAppTemplates.Remove(
            await _db.SavedWhatsAppTemplates.SingleAsync(x => x.Id == templateId));
        await _db.SaveChangesAsync();

        var tempPath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
        await File.WriteAllBytesAsync(tempPath, bytes);
        try
        {
            var restored = await _service.RestoreAsync(
                tempPath, new() { "savedWhatsAppTemplates" }, overwrite: false, "backup-template-test");

            Assert.True(restored.Success, string.Join("; ", restored.Errors));
            var entity = await _db.SavedWhatsAppTemplates.SingleAsync(x => x.Id == templateId);
            Assert.Equal("Emergency notice", entity.Name);
            Assert.Equal("Please contact building management.", entity.Message);
            Assert.Equal("backup-template-building", entity.BuildingId);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void GetKnownCollections_ReturnsAllKnownTables()
    {
        var collections = BackupService.GetKnownCollections();

        Assert.Contains("buildings", collections.Keys);
        Assert.Contains("users", collections.Keys);
        Assert.Contains("residents", collections.Keys);
        Assert.Contains("auditLog", collections.Keys);
        Assert.Contains("savedWhatsAppTemplates", collections.Keys);
        Assert.Contains("notificationMessages", collections.Keys);
        Assert.Contains("notificationRecipients", collections.Keys);
        Assert.Contains("notificationDeliveries", collections.Keys);
        Assert.Contains("notificationReadStates", collections.Keys);
        Assert.Contains("notificationPreferences", collections.Keys);
    }

    [Fact]
    public async Task NormalizedNotifications_BackupAndRestore_RestoresAllRowsByStableGuid()
    {
        var messageId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var deliveryId = Guid.NewGuid();
        var readStateId = Guid.NewGuid();
        var preferenceId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _db.NotificationMessages.Add(new NotificationMessageEntity
        {
            MessageId = messageId,
            BuildingId = "backup-notification-building",
            Title = "Original title",
            Body = "Original body",
            Channel = 0,
            Origin = "deposit",
            CreatedAtUtc = now,
            Recipients =
            {
                new NotificationRecipientEntity
                {
                    RecipientId = recipientId,
                    RecipientType = 1,
                    RecipientReference = "APT-1",
                    ApartmentId = "APT-1",
                    CreatedAtUtc = now
                }
            }
        });
        _db.NotificationDeliveries.Add(new NotificationDeliveryEntity
        {
            DeliveryId = deliveryId,
            RecipientId = recipientId,
            AttemptNumber = 1,
            Provider = "test-provider",
            Status = 2,
            AttemptedAtUtc = now,
            CompletedAtUtc = now,
            ProviderReference = "provider-ref-original",
            SafeErrorCode = "SAFE-001"
        });
        _db.NotificationReadStates.Add(new NotificationReadStateEntity
        {
            ReadStateId = readStateId,
            RecipientId = recipientId,
            ReadAtUtc = now
        });
        _db.NotificationPreferences.Add(new NotificationPreferenceEntity
        {
            PreferenceId = preferenceId,
            SubjectType = 1,
            SubjectReference = "APT-1",
            Channel = 0,
            IsEnabled = true,
            UpdatedAtUtc = now
        });
        await _db.SaveChangesAsync();

        var selected = new List<string>
        {
            "notificationMessages",
            "notificationRecipients",
            "notificationDeliveries",
            "notificationReadStates",
            "notificationPreferences"
        };
        var (bytes, backup) = await _service.CreateBackupAsync(selected, "backup-notification-test");

        Assert.True(backup.Success, string.Join("; ", backup.Errors));
        Assert.Equal(5, backup.TotalCollections);
        Assert.Equal(5, backup.TotalDocuments);

        var message = await _db.NotificationMessages.SingleAsync(x => x.MessageId == messageId);
        message.Title = "Changed title";
        message.Body = "Changed body";
        var delivery = await _db.NotificationDeliveries.SingleAsync(x => x.DeliveryId == deliveryId);
        delivery.ProviderReference = "provider-ref-changed";
        var readState = await _db.NotificationReadStates.SingleAsync(x => x.ReadStateId == readStateId);
        readState.ReadAtUtc = null;
        var preference = await _db.NotificationPreferences.SingleAsync(x => x.PreferenceId == preferenceId);
        preference.IsEnabled = false;
        await _db.SaveChangesAsync();

        using var upload = new MemoryStream(bytes);
        var preview = await _service.PreviewRestoreAsync(upload, "notifications.zip", "backup-notification-test");
        Assert.True(preview.Success, string.Join("; ", preview.Errors));
        Assert.NotNull(preview.RestoreSessionId);

        var restored = await _service.RestoreSessionAsync(
            preview.RestoreSessionId!, selected, overwrite: true, "backup-notification-test");

        Assert.True(restored.Success, string.Join("; ", restored.Errors));

        // Restore runs in a separate DbContext. Clear the arrange context's identity map
        // so assertions read the persisted values instead of already-tracked stale entities.
        _db.ChangeTracker.Clear();

        var restoredMessage = await _db.NotificationMessages.SingleAsync(x => x.MessageId == messageId);
        Assert.Equal("Original title", restoredMessage.Title);
        Assert.Equal("Original body", restoredMessage.Body);

        var restoredRecipient = await _db.NotificationRecipients.SingleAsync(x => x.RecipientId == recipientId);
        Assert.Equal(messageId, restoredRecipient.MessageId);
        Assert.Equal("APT-1", restoredRecipient.ApartmentId);

        var restoredDelivery = await _db.NotificationDeliveries.SingleAsync(x => x.DeliveryId == deliveryId);
        Assert.Equal("provider-ref-original", restoredDelivery.ProviderReference);
        Assert.Equal("SAFE-001", restoredDelivery.SafeErrorCode);

        var restoredReadState = await _db.NotificationReadStates.SingleAsync(x => x.ReadStateId == readStateId);
        Assert.NotNull(restoredReadState.ReadAtUtc);

        var restoredPreference = await _db.NotificationPreferences.SingleAsync(x => x.PreferenceId == preferenceId);
        Assert.True(restoredPreference.IsEnabled);
        Assert.False(File.Exists(preview.TempFilePath));
    }

    [Fact]
    public async Task Localization_BackupAndRestore_RestoresLanguagesEntriesAndValues()
    {
        const string culture = "ar-EG";
        const string translationKey = "Backup.Restore.RoundTrip";
        _db.Languages.Add(new LanguageEntity
        {
            CultureCode = culture,
            EnglishName = "Arabic (Egypt)",
            NativeName = "العربية (مصر)",
            Direction = "RTL",
            IsEnabled = true,
            IsDefault = false,
            SortOrder = 7,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedBy = "localization-test"
        });
        _db.TranslationEntries.Add(new TranslationEntryEntity
        {
            Key = translationKey,
            Category = "Tests",
            SourceGroup = "BackupRestore",
            Description = "Localization backup round-trip",
            IsArchived = false,
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedBy = "localization-test"
        });
        _db.TranslationValues.Add(new TranslationValueEntity
        {
            Key = translationKey,
            CultureCode = culture,
            Value = "القيمة الأصلية",
            UpdatedAtUtc = DateTime.UtcNow,
            UpdatedBy = "localization-test"
        });
        await _db.SaveChangesAsync();

        var selected = new List<string> { "languages", "translationEntries", "translationValues" };
        var (bytes, backup) = await _service.CreateBackupAsync(selected, "localization-test");
        Assert.True(backup.Success, string.Join("; ", backup.Errors));
        Assert.Equal(3, backup.TotalCollections);
        Assert.Equal(3, backup.TotalDocuments);

        var language = await _db.Languages.SingleAsync(x => x.CultureCode == culture);
        language.NativeName = "Changed language";
        var entry = await _db.TranslationEntries.SingleAsync(x => x.Key == translationKey);
        entry.Description = "Changed description";
        var value = await _db.TranslationValues.SingleAsync(x => x.Key == translationKey && x.CultureCode == culture);
        value.Value = "قيمة معدّلة";
        await _db.SaveChangesAsync();

        using var upload = new MemoryStream(bytes);
        var preview = await _service.PreviewRestoreAsync(upload, "localization.zip", "localization-test");
        Assert.True(preview.Success, string.Join("; ", preview.Errors));
        Assert.NotNull(preview.RestoreSessionId);

        var restored = await _service.RestoreSessionAsync(
            preview.RestoreSessionId!, selected, overwrite: true, "localization-test");
        Assert.True(restored.Success, string.Join("; ", restored.Errors));

        _db.ChangeTracker.Clear();
        var restoredLanguage = await _db.Languages.SingleAsync(x => x.CultureCode == culture);
        Assert.Equal("العربية (مصر)", restoredLanguage.NativeName);
        Assert.Equal("RTL", restoredLanguage.Direction);

        var restoredEntry = await _db.TranslationEntries.SingleAsync(x => x.Key == translationKey);
        Assert.Equal("Localization backup round-trip", restoredEntry.Description);
        Assert.NotEmpty(restoredEntry.RowVersion);

        var restoredValue = await _db.TranslationValues.SingleAsync(
            x => x.Key == translationKey && x.CultureCode == culture);
        Assert.Equal("القيمة الأصلية", restoredValue.Value);
        Assert.False(File.Exists(preview.TempFilePath));
    }

    [Fact]
    public void GetKnownCollections_DoesNotExposeRetiredLegacyCollections()
    {
        var collections = BackupService.GetKnownCollections();

        Assert.DoesNotContain("expenses", collections.Keys);
        Assert.DoesNotContain("revenues", collections.Keys);
        Assert.DoesNotContain("monthlyApartmentShares", collections.Keys);
        Assert.DoesNotContain("deposits", collections.Keys);
        Assert.DoesNotContain("depositEditHistory", collections.Keys);
        Assert.DoesNotContain("walletAdjustments", collections.Keys);
        Assert.DoesNotContain("notifications", collections.Keys);
        Assert.DoesNotContain("pollVotes", collections.Keys);
        Assert.DoesNotContain("maintenanceRecords", collections.Keys);
    }
}