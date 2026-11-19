using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Exports database collections as JSON/ZIP backups and restores them, with a preview step.
/// </summary>
/// <remarks>
/// The service works against the <see cref="AppDbContext"/> through
/// <see cref="IDbContextFactory{TContext}"/> so that every operation gets its own short-lived
/// context. All restore operations are wrapped in a database transaction when the provider is
/// relational (SQL Server in production) and fall back to plain execution for in-memory providers
/// used by tests.
/// </remarks>
public class BackupService
{
    private sealed record RestoreSession(string Path, string UserId, DateTime ExpiresAtUtc, string Sha256);
    private static readonly ConcurrentDictionary<string, RestoreSession> RestoreSessions = new();
    private static readonly object RestoreSessionsLock = new();
    private static readonly TimeSpan RestoreSessionLifetime = TimeSpan.FromMinutes(20);
    private const int MaxActiveRestoreSessions = 10;
    private const int MaxActiveRestoreSessionsPerUser = 2;
    private const long MaxArchiveBytes = 100L * 1024 * 1024;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IAuditLogger _audit;
    private readonly ILogger<BackupService> _log;

    /// <summary>
    /// Gets the map of known collection names to their localized display labels.
    /// </summary>
    /// <remarks>
    /// The order of this dictionary defines the display order in the UI and is also used by
    /// <see cref="GetStatsAsync"/> to enumerate collections.
    /// </remarks>
    private static Dictionary<string, string> KnownTables => new(StringComparer.Ordinal)
    {
        ["buildings"] = "العمارات",
        ["buildingSettings"] = "إعدادات العمارات",
        ["floors"] = "الأدوار",
        ["apartments"] = "الشقق",
        ["financialCategories"] = "التصنيفات المالية",
        ["savedWhatsAppTemplates"] = "قوالب واتساب",
        ["persons"] = "الأشخاص",
        ["userAccounts"] = "حسابات المستخدمين",
        ["userCredentials"] = "بيانات الاعتماد",
        ["userTwoFactorSettings"] = "إعدادات التحقق الثنائي",
        ["userRecoveryCodes"] = "رموز الاسترداد",
        ["roleDefinitions"] = "تعريفات الأدوار",
        ["rolePermissions"] = "صلاحيات الأدوار",
        ["userRoleAssignments"] = "تعيينات الأدوار",
        ["apartmentOwnerships"] = "ملكية الشقق",
        ["apartmentOccupancies"] = "إشغال الشقق",
        ["apartmentAccessGrants"] = "صلاحيات الوصول للشقق",
        ["accountPermissionOverrides"] = "استثناءات الصلاحيات",
        ["legacyIdentityMaps"] = "خرائط ربط الهوية التاريخية",
        ["personPhoneChangeRequests"] = "طلبات تغيير الهاتف",
        ["globalApplicationSettings"] = "الإعدادات العامة",
        ["protectedIntegrationSecrets"] = "أسرار التكامل المشفرة",
        ["auditEvents"] = "أحداث التدقيق",
        ["qrCapabilities"] = "صلاحيات رموز QR",
        ["qrCapabilityUses"] = "استخدامات رموز QR",
        ["mediaDevices"] = "أجهزة المراقبة",
        ["mediaDeviceChannels"] = "قنوات الكاميرات",
        ["mediaCredentialReferences"] = "مراجع بيانات اعتماد أجهزة المراقبة",
        ["mediaProvisioningOperations"] = "عمليات تجهيز أجهزة المراقبة",
        ["mediaAccessSessions"] = "جلسات الوصول للكاميرات",
        ["backupJobs"] = "عمليات النسخ الاحتياطي",
        ["backupArtifacts"] = "ملفات النسخ الاحتياطي",
        ["backupRetentionEvents"] = "أحداث الاحتفاظ بالنسخ",
        ["restoreOperations"] = "عمليات الاستعادة",
        ["backupScheduleConfigurations"] = "جدولة النسخ الاحتياطي",
        ["financialPeriods"] = "الفترات المالية",
        ["paymentDeposits"] = "الإيداعات",
        ["ledgerTransactions"] = "المعاملات المالية",
        ["ledgerEntries"] = "قيود دفتر الأستاذ",
        ["financialSourceLinks"] = "روابط مصادر المعاملات",
        ["financialAllocationBatches"] = "دفعات التوزيع المالي",
        ["financialAllocationLines"] = "بنود التوزيع المالي",
        ["financialReconciliationRuns"] = "عمليات المطابقة المالية",
        ["financialReconciliationDifferences"] = "فروق المطابقة المالية",
        ["invoiceIssues"] = "مشكلات الفواتير",
        ["pollDefinitions"] = "استطلاعات الرأي",
        ["pollChoiceOptions"] = "خيارات الاستطلاعات",
        ["pollVotingPolicies"] = "سياسات التصويت",
        ["pollEligibility"] = "أهلية التصويت",
        ["pollBallots"] = "بطاقات التصويت",
        ["pollLifecycleEvents"] = "أحداث دورة الاستطلاع",
        ["maintenanceRequests"] = "طلبات الصيانة",
        ["maintenanceAssignments"] = "تعيينات الصيانة",
        ["maintenanceComments"] = "تعليقات الصيانة",
        ["maintenanceAttachments"] = "مرفقات الصيانة",
        ["maintenanceStatusEvents"] = "أحداث حالة الصيانة",
        ["maintenanceLedgerLinks"] = "روابط الصيانة المالية",
        ["notificationMessages"] = "رسائل الإشعارات",
        ["notificationRecipients"] = "مستلمو الإشعارات",
        ["notificationDeliveries"] = "تسليم الإشعارات",
        ["notificationReadStates"] = "حالات قراءة الإشعارات",
        ["notificationPreferences"] = "تفضيلات الإشعارات",
        ["presenceSessions"] = "جلسات الحضور",
        ["presenceHeartbeats"] = "نبضات الحضور",
        ["languages"] = "اللغات",
        ["translationEntries"] = "مفاتيح الترجمة",
        ["translationValues"] = "قيم الترجمة"
    };

    private static readonly IReadOnlyDictionary<string, Type> BackupEntityTypes =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["buildings"] = typeof(Data.Entities.BuildingEntity),
            ["buildingSettings"] = typeof(Data.Entities.BuildingSettingsEntity),
            ["floors"] = typeof(Data.Entities.FloorEntity),
            ["apartments"] = typeof(Data.Entities.ApartmentEntity),
            ["financialCategories"] = typeof(Data.Entities.FinancialCategoryEntity),
            ["savedWhatsAppTemplates"] = typeof(Data.Entities.SavedWhatsAppTemplateEntity),
            ["persons"] = typeof(Data.Entities.PersonEntity),
            ["userAccounts"] = typeof(Data.Entities.UserAccountEntity),
            ["userCredentials"] = typeof(Data.Entities.UserCredentialEntity),
            ["userTwoFactorSettings"] = typeof(Data.Entities.UserTwoFactorSettingEntity),
            ["userRecoveryCodes"] = typeof(Data.Entities.UserRecoveryCodeEntity),
            ["roleDefinitions"] = typeof(Data.Entities.RoleDefinitionEntity),
            ["rolePermissions"] = typeof(Data.Entities.RolePermissionEntity),
            ["userRoleAssignments"] = typeof(Data.Entities.UserRoleAssignmentEntity),
            ["apartmentOwnerships"] = typeof(Data.Entities.ApartmentOwnershipEntity),
            ["apartmentOccupancies"] = typeof(Data.Entities.ApartmentOccupancyEntity),
            ["apartmentAccessGrants"] = typeof(Data.Entities.ApartmentAccessGrantEntity),
            ["accountPermissionOverrides"] = typeof(Data.Entities.AccountPermissionOverrideEntity),
            ["legacyIdentityMaps"] = typeof(Data.Entities.LegacyIdentityMapEntity),
            ["personPhoneChangeRequests"] = typeof(Data.Entities.PersonPhoneChangeRequestEntity),
            ["globalApplicationSettings"] = typeof(Data.Entities.GlobalApplicationSettingsEntity),
            ["protectedIntegrationSecrets"] = typeof(Data.Entities.ProtectedIntegrationSecretEntity),
            ["auditEvents"] = typeof(Data.Entities.AuditEventEntity),
            ["qrCapabilities"] = typeof(Data.Entities.QrCapabilityEntity),
            ["qrCapabilityUses"] = typeof(Data.Entities.QrCapabilityUseEntity),
            ["mediaDevices"] = typeof(Data.Entities.MediaDeviceEntity),
            ["mediaDeviceChannels"] = typeof(Data.Entities.MediaDeviceChannelEntity),
            ["mediaCredentialReferences"] = typeof(Data.Entities.MediaCredentialReferenceEntity),
            ["mediaProvisioningOperations"] = typeof(Data.Entities.MediaProvisioningOperationEntity),
            ["mediaAccessSessions"] = typeof(Data.Entities.MediaAccessSessionEntity),
            ["backupJobs"] = typeof(Data.Entities.BackupJobEntity),
            ["backupArtifacts"] = typeof(Data.Entities.BackupArtifactEntity),
            ["backupRetentionEvents"] = typeof(Data.Entities.BackupRetentionEventEntity),
            ["restoreOperations"] = typeof(Data.Entities.RestoreOperationEntity),
            ["backupScheduleConfigurations"] = typeof(Data.Entities.BackupScheduleConfigurationEntity),
            ["financialPeriods"] = typeof(Data.Entities.FinancialPeriodEntity),
            ["paymentDeposits"] = typeof(Data.Entities.PaymentDepositEntity),
            ["ledgerTransactions"] = typeof(Data.Entities.LedgerTransactionEntity),
            ["ledgerEntries"] = typeof(Data.Entities.LedgerEntryEntity),
            ["financialSourceLinks"] = typeof(Data.Entities.FinancialSourceLinkEntity),
            ["financialAllocationBatches"] = typeof(Data.Entities.FinancialAllocationBatchEntity),
            ["financialAllocationLines"] = typeof(Data.Entities.FinancialAllocationLineEntity),
            ["financialReconciliationRuns"] = typeof(Data.Entities.FinancialReconciliationRunEntity),
            ["financialReconciliationDifferences"] = typeof(Data.Entities.FinancialReconciliationDifferenceEntity),
            ["invoiceIssues"] = typeof(Data.Entities.InvoiceIssueEntity),
            ["pollDefinitions"] = typeof(Data.Entities.PollEntity),
            ["pollChoiceOptions"] = typeof(Data.Entities.PollOptionEntity),
            ["pollVotingPolicies"] = typeof(Data.Entities.PollVotingPolicyEntity),
            ["pollEligibility"] = typeof(Data.Entities.PollEligibilityEntity),
            ["pollBallots"] = typeof(Data.Entities.PollBallotEntity),
            ["pollLifecycleEvents"] = typeof(Data.Entities.PollLifecycleEventEntity),
            ["maintenanceRequests"] = typeof(Data.Entities.MaintenanceRequestEntity),
            ["maintenanceAssignments"] = typeof(Data.Entities.MaintenanceAssignmentEntity),
            ["maintenanceComments"] = typeof(Data.Entities.MaintenanceCommentEntity),
            ["maintenanceAttachments"] = typeof(Data.Entities.MaintenanceAttachmentEntity),
            ["maintenanceStatusEvents"] = typeof(Data.Entities.MaintenanceStatusEventEntity),
            ["maintenanceLedgerLinks"] = typeof(Data.Entities.MaintenanceLedgerLinkEntity),
            ["notificationMessages"] = typeof(Data.Entities.NotificationMessageEntity),
            ["notificationRecipients"] = typeof(Data.Entities.NotificationRecipientEntity),
            ["notificationDeliveries"] = typeof(Data.Entities.NotificationDeliveryEntity),
            ["notificationReadStates"] = typeof(Data.Entities.NotificationReadStateEntity),
            ["notificationPreferences"] = typeof(Data.Entities.NotificationPreferenceEntity),
            ["presenceSessions"] = typeof(Data.Entities.PresenceSessionEntity),
            ["presenceHeartbeats"] = typeof(Data.Entities.PresenceHeartbeatEntity),
            ["languages"] = typeof(Data.Entities.LanguageEntity),
            ["translationEntries"] = typeof(Data.Entities.TranslationEntryEntity),
            ["translationValues"] = typeof(Data.Entities.TranslationValueEntity)
        };
    private static readonly JsonSerializerOptions RestoreJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="sqlFactory">Factory used to create short-lived <see cref="AppDbContext"/> instances.</param>
    /// <param name="audit">Audit logger used to record backup/restore actions.</param>
    /// <param name="log">Logger for diagnostic and warning messages.</param>
    public BackupService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IAuditLogger audit,
        ILogger<BackupService> log)
    {
        _sqlFactory = sqlFactory;
        _audit = audit;
        _log = log;
    }

    /// <summary>
    /// Gets the known collections.
    /// </summary>
    /// <returns>A read-only dictionary mapping collection names to Arabic labels.</returns>
    public static IReadOnlyDictionary<string, string> GetKnownCollections() => KnownTables;

    /// <summary>
    /// Gets the stats.
    /// </summary>
    /// <remarks>
    /// Counts rows in every known collection. Any per-collection failure is logged as a
    /// warning and reported as a count of <c>0</c> so that one broken table does not break
    /// the whole statistics page.
    /// </remarks>
    /// <returns>A list of <see cref="BackupStats"/> ordered like <see cref="KnownTables"/>.</returns>
    public async Task<List<BackupStats>> GetStatsAsync()
    {
        var result = new List<BackupStats>();
        await using var db = await _sqlFactory.CreateDbContextAsync();
        foreach (var collection in KnownTables)
        {
            var count = 0;
            try
            {
                var entityType = BackupEntityTypes[collection.Key];
                count = await QuerySet(db, entityType).CountAsync();
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Stats count failed for target collection {Collection}", collection.Key);
            }

            result.Add(new BackupStats
            {
                Collection = collection.Key,
                LabelAr = collection.Value,
                DocCount = count,
                Selected = true
            });
        }
        return result;
    }

    /// <summary>
    /// Creates the backup.
    /// </summary>
    /// <remarks>
    /// Serializes each selected collection to a JSON file and packs everything into a ZIP
    /// along with a <c>README.md</c>. Per-collection failures are captured in
    /// <see cref="BackupResult.Errors"/> and do not abort the whole backup.
    /// </remarks>
    /// <param name="selectedTables">The collection names to include in the backup.</param>
    /// <param name="userId">The id of the user performing the backup (recorded in the audit log).</param>
    /// <returns>
    /// A tuple containing the produced ZIP bytes and a <see cref="BackupResult"/> describing
    /// the operation (collections, document count, file size, duration and file name).
    /// </returns>
    public async Task<(byte[] Bytes, BackupResult Result)> CreateBackupAsync(
        List<string> selectedTables, string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new BackupResult();

        var requestedTables = selectedTables?
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList() ?? new List<string>();
        var unknownTables = requestedTables
            .Where(name => !KnownTables.ContainsKey(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (unknownTables.Count > 0)
        {
            result.Errors.Add($"Unknown backup collections: {string.Join(", ", unknownTables)}");
            result.Duration = sw.Elapsed;
            return (Array.Empty<byte>(), result);
        }

        requestedTables = requestedTables.Distinct(StringComparer.Ordinal).ToList();
        if (requestedTables.Count == 0)
        {
            result.Errors.Add("At least one valid backup collection must be selected.");
            result.Duration = sw.Elapsed;
            return (Array.Empty<byte>(), result);
        }

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var tableName in requestedTables)
            {

                try
                {
                    // 1) قراءة صفوف الـ collection من الداتابيز
                    var docs = await ReadTableAsync(tableName);

                    // 2) تحويلها لـ JSON منسّق
                    var json = JsonSerializer.Serialize(docs, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    // 3) إضافة الملف للـ ZIP
                    AddFileToZip(zip, $"{tableName}.json", json);
                    result.Collections.Add(tableName);
                    result.TotalDocuments += docs.Count;
                }
                catch (Exception ex)
                {
                    // فشل collection واحد يتسجل وميكملش
                    result.Errors.Add($"{tableName}: {ex.Message}");
                }
            }

            // 4) إضافة ملف README يوضح محتويات الـ ZIP
            var readme = $@"# Building Management — SQL Backup
Created: {DateTime.UtcNow:O}
By: {userId}

## Contents:
{string.Join("\n", result.Collections.Select(c => $"- {c}.json ({KnownTables[c]})"))}

## Notes:
- Each JSON file contains an array of rows.
- Each row = {{ id, data }}.
";
            AddFileToZip(zip, "README.md", readme);
        }

        sw.Stop();
        result.Success = result.Errors.Count == 0 &&
            result.Collections.Count == requestedTables.Count;
        if (!result.Success && result.Errors.Count == 0)
            result.Errors.Add("The backup archive is incomplete.");
        result.TotalCollections = result.Collections.Count;
        result.FileSize = ms.Length;
        result.Duration = sw.Elapsed;
        result.FileName = $"backup-{DateTime.UtcNow:yyyy-MM-dd-HHmm}.zip";

        // تسجيل العملية في الـ audit
        await _audit.LogAsync(
            action: "backup.create",
            userId: userId,
            userRole: "superadmin",
            metadata: new
            {
                collections = result.Collections,
                totalDocs = result.TotalDocuments,
                fileSize = result.FileSize,
                success = result.Success,
                errors = result.Errors.Count
            },
            severity: "warning");

        return (ms.ToArray(), result);
    }

    /// <summary>
    /// Reads all rows of the given table as a list of <c>{ id, data }</c> dictionaries.
    /// </summary>
    /// <remarks>
    /// The <c>id</c> value is either the entity's primary key or a composite key joined
    /// with underscores (for tables whose primary key is made of several columns). The
    /// composite-key format is the one expected by <see cref="RestoreEntityAsync"/>.
    /// </remarks>
    /// <param name="tableName">The collection name to read.</param>
    /// <returns>A list of row dictionaries, one per entity.</returns>
    private async Task<List<Dictionary<string, object>>> ReadTableAsync(string tableName)
    {
        if (!BackupEntityTypes.TryGetValue(tableName, out var clrType))
            throw new InvalidOperationException($"Unknown backup collection: {tableName}");
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var entityType = db.Model.FindEntityType(clrType)
            ?? throw new InvalidOperationException($"Entity {clrType.Name} is not mapped.");
        var rows = await QuerySet(db, clrType).AsNoTracking().ToListAsync();
        var result = new List<Dictionary<string, object>>(rows.Count);
        foreach (var row in rows)
            result.Add(ToDict(db, BuildEntityKey(db, entityType, row), row));
        return result;
    }
    /// <summary>
    /// Wraps an entity into the <c>{ id, data }</c> shape used inside backup JSON files.
    /// </summary>
    /// <param name="db">The active database context used to read entity values.</param>
    /// <param name="id">The (possibly composite) primary key of the entity.</param>
    /// <param name="entity">The entity instance.</param>
    /// <returns>A dictionary with the row id and the serialized entity data.</returns>
    private static Dictionary<string, object> ToDict(AppDbContext db, string id, object entity)
    {
        var entry = db.Entry(entity);
        var data = new Dictionary<string, object?>();
        foreach (var property in entry.Metadata.GetProperties())
            data[property.Name] = entry.Property(property.Name).CurrentValue;
        return new Dictionary<string, object>
        {
            ["id"] = id,
            ["data"] = data
        };
    }

    private static string BuildEntityKey(AppDbContext db, Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, object entity)
    {
        var key = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"Entity {entityType.ClrType.Name} has no primary key.");
        var entry = db.Entry(entity);
        return string.Join("_", key.Properties.Select(property =>
            Convert.ToString(entry.Property(property.Name).CurrentValue, CultureInfo.InvariantCulture) ?? ""));
    }
    /// <summary>
    /// Adds a text file to the given ZIP archive.
    /// </summary>
    /// <param name="zip">The target ZIP archive.</param>
    /// <param name="fileName">The name of the entry inside the archive.</param>
    /// <param name="content">The UTF-8 text content.</param>
    private static void AddFileToZip(ZipArchive zip, string fileName, string content)
    {
        var entry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Restores the operation on the backup.
    /// </summary>
    /// <remarks>
    /// Tables are restored in a fixed order (<c>restoreOrder</c>) so that parent rows are
    /// inserted before their children. When the provider is relational, all work happens
    /// inside a single transaction that is committed on success and rolled back on failure.
    /// For non-relational test providers, changes are saved only after the full archive has
    /// validated successfully. The temporary ZIP file is always deleted at the end.
    /// </remarks>
    /// <param name="restoreSessionId"></param>
    /// <param name="selectedTables">The collection names to restore.</param>
    /// <param name="overwrite">When <c>true</c>, existing rows are updated in place; otherwise they are skipped.</param>
    /// <param name="userId">The id of the user performing the restore (recorded in the audit log).</param>
    /// <returns>A <see cref="RestoreResult"/> summarizing restored/skipped counts and any errors.</returns>
    public async Task<RestoreResult> RestoreSessionAsync(
        string restoreSessionId, List<string> selectedTables, bool overwrite, string userId)
    {
        RestoreSession? consumedSession = null;
        var nowUtc = DateTime.UtcNow;
        lock (RestoreSessionsLock)
        {
            CleanupExpiredRestoreSessionsUnderLock(nowUtc);
            if (string.IsNullOrWhiteSpace(restoreSessionId) ||
                !RestoreSessions.TryGetValue(restoreSessionId, out var session) ||
                !string.Equals(session.UserId, userId, StringComparison.Ordinal) ||
                session.ExpiresAtUtc <= nowUtc ||
                !RestoreSessions.TryRemove(restoreSessionId, out consumedSession))
            {
                consumedSession = null;
            }
        }

        if (consumedSession == null)
            return new RestoreResult { Success = false, Errors = { Loc.T("The_Temporary_File_Does_Not_Exist") } };

        return await RestoreAsyncCore(
            consumedSession.Path, selectedTables, overwrite, userId, consumedSession.Sha256);
    }
    /// <summary>
    /// يستعيد نسخة احتياطية إلى قاعدة البيانات.
    /// </summary>
    /// <param name="tempFilePath">مسار ملف النسخة الاحتياطية.</param>
    /// <param name="selectedTables">معرّف جلسة الاستعادة.</param>
    /// <param name="overwrite"></param>
    /// <param name="userId"></param>
    /// <returns>مهمة تُكمل عند انتهاء الاستعادة.</returns>
    public Task<RestoreResult> RestoreAsync(
        string tempFilePath, List<string> selectedTables, bool overwrite, string userId) =>
        RestoreAsyncCore(tempFilePath, selectedTables, overwrite, userId, expectedSha256: null);

    private static IQueryable<object> QuerySet(AppDbContext db, Type entityType)
    {
        var setMethod = typeof(DbContext).GetMethods()
            .Single(method => method.Name == nameof(DbContext.Set)
                && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
        var query = (IQueryable)setMethod.MakeGenericMethod(entityType).Invoke(db, null)!;
        return query.Cast<object>();
    }

    /// <summary> Gets or previews data through PreviewRestoreAsync. </summary>
    public async Task<RestorePreview> PreviewRestoreAsync(Stream input, string fileName, string userId)
    {
        var preview = new RestorePreview { FileName = Path.GetFileName(fileName ?? "") };
        string? tempFilePath = null;
        try
        {
            if (input is null || string.IsNullOrWhiteSpace(fileName) ||
                !fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                preview.Errors.Add("A valid ZIP backup file is required.");
                return preview;
            }

            using var upload = new MemoryStream();
            await input.CopyToAsync(upload);
            var uploadedBytes = upload.ToArray();
            preview.FileSize = uploadedBytes.LongLength;
            if (uploadedBytes.LongLength == 0 || uploadedBytes.LongLength > MaxArchiveBytes)
            {
                preview.Errors.Add("Backup archive size is invalid.");
                return preview;
            }

            var zipBytes = BackupFileProtector.Unprotect(uploadedBytes);
            if (zipBytes.LongLength == 0 || zipBytes.LongLength > MaxArchiveBytes)
            {
                preview.Errors.Add("Backup archive size is invalid.");
                return preview;
            }

            using (var archiveStream = new MemoryStream(zipBytes, writable: false))
            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false))
            {
                // Apply the same strict ZIP, path, size, collection, and row-envelope
                // validation during preview that is repeated immediately before restore.
                preview.Collections.AddRange(await BackupArchiveValidator.ValidateAsync(archive));
            }

            if (preview.Collections.Count == 0 && preview.Errors.Count == 0)
                preview.Errors.Add("The ZIP does not contain any recognized backup collections.");
            if (preview.Errors.Count > 0) return preview;

            tempFilePath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
            await File.WriteAllBytesAsync(tempFilePath, zipBytes);
            var sessionId = Guid.NewGuid().ToString("N");
            var session = new RestoreSession(tempFilePath, userId,
                DateTime.UtcNow.Add(RestoreSessionLifetime),
                Convert.ToHexString(SHA256.HashData(zipBytes)));
            lock (RestoreSessionsLock)
            {
                CleanupExpiredRestoreSessionsUnderLock(DateTime.UtcNow);
                var userSessions = RestoreSessions.Values.Count(item =>
                    string.Equals(item.UserId, userId, StringComparison.Ordinal));
                if (RestoreSessions.Count >= MaxActiveRestoreSessions ||
                    userSessions >= MaxActiveRestoreSessionsPerUser)
                    throw new InvalidOperationException("Too many active restore sessions.");
                RestoreSessions[sessionId] = session;
            }

            preview.RestoreSessionId = sessionId;
            preview.TempFilePath = tempFilePath;
            preview.Success = true;
            return preview;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Backup preview failed for {FileName}", Path.GetFileName(fileName ?? ""));
            preview.Errors.Add("The backup file could not be validated.");
            if (tempFilePath is not null)
            {
                try { File.Delete(tempFilePath); } catch { }
            }
            return preview;
        }
    }

    private static void CleanupExpiredRestoreSessionsUnderLock(DateTime nowUtc)
    {
        foreach (var pair in RestoreSessions.Where(pair => pair.Value.ExpiresAtUtc <= nowUtc).ToList())
        {
            if (!RestoreSessions.TryRemove(pair.Key, out var expired)) continue;
            try { if (File.Exists(expired.Path)) File.Delete(expired.Path); } catch { }
        }
    }

    private async Task<RestoreResult> RestoreAsyncCore(
        string tempFilePath, List<string> selectedTables, bool overwrite, string userId, string? expectedSha256)
    {
        var sw = Stopwatch.StartNew();
        var result = new RestoreResult();

        // 0) الملف لازم يكون اللي اتعمل في خطوة المعاينة (restore-<guid>.zip في فولدر الـ temp)،
        //    مش أي مسار جاي من الفورم — وإلا ممكن نفتح/نمسح أي ملف على السيرفر
        if (!IsSafeRestoreFile(tempFilePath))
        {
            result.Errors.Add(Loc.T("The_Temporary_File_Does_Not_Exist"));
            return result;
        }

        // 1) تحقق إن الملف المؤقت موجود
        if (!File.Exists(tempFilePath))
        {
            result.Errors.Add(Loc.T("The_Temporary_File_Does_Not_Exist"));
            return result;
        }

        // 2) ترتيب الاستعادة حسب الـ FK (الأب قبل الابن)
        var restoreOrder = new[]
        {
            "buildings",
            "buildingSettings",
            "floors",
            "apartments",
            "financialCategories",
            "persons",
            "roleDefinitions",
            "rolePermissions",
            "userAccounts",
            "userCredentials",
            "userTwoFactorSettings",
            "userRecoveryCodes",
            "userRoleAssignments",
            "apartmentOwnerships",
            "apartmentOccupancies",
            "apartmentAccessGrants",
            "accountPermissionOverrides",
            "legacyIdentityMaps",
            "personPhoneChangeRequests",
            "financialPeriods",
            "paymentDeposits",
            "ledgerTransactions",
            "ledgerEntries",
            "financialSourceLinks",
            "financialAllocationBatches",
            "financialAllocationLines",
            "financialReconciliationRuns",
            "financialReconciliationDifferences",
            "invoiceIssues",
            "pollDefinitions",
            "pollChoiceOptions",
            "pollVotingPolicies",
            "pollEligibility",
            "pollBallots",
            "pollLifecycleEvents",
            "maintenanceRequests",
            "maintenanceAssignments",
            "maintenanceComments",
            "maintenanceAttachments",
            "maintenanceStatusEvents",
            "maintenanceLedgerLinks",
            "notificationMessages",
            "notificationRecipients",
            "notificationDeliveries",
            "notificationReadStates",
            "notificationPreferences",
            "presenceSessions",
            "presenceHeartbeats",
            "qrCapabilities",
            "qrCapabilityUses",
            "mediaDevices",
            "mediaCredentialReferences",
            "mediaProvisioningOperations",
            "mediaDeviceChannels",
            "mediaAccessSessions",
            "backupArtifacts",
            "backupJobs",
            "backupRetentionEvents",
            "restoreOperations",
            "backupScheduleConfigurations",
            "globalApplicationSettings",
            "protectedIntegrationSecrets",
            "auditEvents",
            "savedWhatsAppTemplates",
            "languages",
            "translationEntries",
            "translationValues"
        };

        // نحتفظ فقط بالجداول اللي المستخدم اختارها
        var orderedTables = restoreOrder
            .Where(t => selectedTables.Contains(t, StringComparer.Ordinal))
            .ToList();
        if (orderedTables.Count == 0 || selectedTables.Any(t => !restoreOrder.Contains(t, StringComparer.Ordinal)))
        {
            result.Errors.Add("Invalid restore collection selection.");
            try { File.Delete(tempFilePath); } catch { }
            return result;
        }

        try
        {
            // Keep one read handle from digest verification through ZIP processing to avoid
            // a preview/restore time-of-check-to-time-of-use replacement window.
            await using var archiveStream = new FileStream(
                tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (expectedSha256 != null)
            {
                var expectedHash = Convert.FromHexString(expectedSha256);
                var actualHash = await SHA256.HashDataAsync(archiveStream);
                if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
                    throw new InvalidDataException("The reviewed restore archive has changed.");
                archiveStream.Position = 0;
            }

            using var zip = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true);
            _ = await BackupArchiveValidator.ValidateAsync(zip);

            await using var db = await _sqlFactory.CreateDbContextAsync();

            // ✅ دعم InMemory (الـ tests) و SQL Server (الإنتاج)
            IDbContextTransaction? tx = null;
            if (db.Database.IsRelational())
            {
                tx = await db.Database.BeginTransactionAsync();
            }

            try
            {
                foreach (var tableName in orderedTables)
                {
                    var entry = zip.GetEntry($"{tableName}.json");
                    if (entry == null)
                    {
                        result.Errors.Add($"Table '{tableName}' not found in ZIP");
                        continue;
                    }

                    result.TotalCollections++;

                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var json = await reader.ReadToEndAsync();

                    try
                    {
                        var docs = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                        if (docs == null) continue;

                        result.TotalDocuments += docs.Count;

                        int restored = 0;
                        int skipped = 0;

                        foreach (var doc in docs)
                        {
                            var id = doc["id"].GetString();
                            var data = doc["data"];

                            if (string.IsNullOrEmpty(id)) continue;

                            try
                            {
                                // استعادة سجل واحد — لو نجح نزوّد restored وإلا skipped
                                var success = await RestoreEntityAsync(db, tableName, id, data, overwrite);
                                if (success) restored++;
                                else skipped++;
                            }
                            catch (Exception ex)
                            {
                                _log.LogWarning(ex, "Backup restore row failed in collection {Collection}", tableName);
                                result.Errors.Add($"A record in '{tableName}' could not be restored.");
                                skipped++;
                            }
                        }

                        result.Restored += restored;
                        result.Skipped += skipped;
                        result.Details.Add($"{tableName}: {restored} restored, {skipped} skipped");

                        // حفظ الجدول ده (جوه الـ transaction) — من غير SaveChanges مفيش حاجة بتتكتب فعلًا
                        if (tx != null)
                        {
                            await db.SaveChangesAsync();
                            db.ChangeTracker.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        // فشل الجدول كله → rollback ورمي الخطأ
                        _log.LogError(ex, "Backup restore collection failed: {Collection}", tableName);
                        result.Errors.Add($"Collection '{tableName}' could not be restored.");
                        throw new InvalidDataException("A backup collection could not be restored.");
                    }
                }

                if (result.Errors.Count > 0)
                {
                    if (tx != null) await tx.RollbackAsync();
                    else db.ChangeTracker.Clear(); // no SaveChanges occurred for non-relational providers
                    result.Success = false;
                    result.Restored = 0;
                    result.Details.Add("Rolled back: no changes were saved because of the errors above");
                }
                else
                {
                    if (tx != null) await tx.CommitAsync();
                    else await db.SaveChangesAsync();
                    result.Success = true;
                }
            }
            catch
            {
                // أي خطأ غير متوقع → rollback
                if (tx != null) await tx.RollbackAsync();
                throw;
            }
            finally
            {
                if (tx != null) await tx.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Backup restore failed for user {UserId}", userId);
            result.Success = false;
            result.Errors.Add("The restore could not be completed. Please contact an administrator.");
        }
        finally
        {
            // امسح الملف المؤقت في كل الأحوال
            try { File.Delete(tempFilePath); } catch { }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

        // تسجيل العملية في الـ audit
        await _audit.LogAsync(
            action: "backup.restore",
            userId: userId,
            userRole: "superadmin",
            metadata: new
            {
                tables = orderedTables,
                restored = result.Restored,
                skipped = result.Skipped,
                errors = result.Errors.Count
            },
            severity: "critical");

        return result;
    }

    /// <summary>
    /// Restores a single row into its target table based on the row's <c>id</c> and data.
    /// </summary>
    /// <remarks>
    /// The composite <c>id</c> format must match the one produced by <see cref="ReadTableAsync"/>:
    /// <c>buildingId_adminUid</c>, <c>buildingId_monthKey_apartmentId</c>, etc.
    /// Returns <c>false</c> when the row already exists and <paramref name="overwrite"/> is
    /// <c>false</c>, or when the id cannot be parsed.
    /// </remarks>
    /// <param name="db">The active <see cref="AppDbContext"/> (shared across the whole restore).</param>
    /// <param name="tableName">The target collection name.</param>
    /// <param name="id">The row's (possibly composite) primary key.</param>
    /// <param name="data">The JSON payload of the entity.</param>
    /// <param name="overwrite">When <c>true</c>, the existing row is deleted before insertion.</param>
    /// <returns><c>true</c> when the row was restored; <c>false</c> when it was skipped.</returns>
    private async Task<bool> RestoreEntityAsync(
        AppDbContext db, string tableName, string id, JsonElement data, bool overwrite)
    {
        if (!BackupEntityTypes.TryGetValue(tableName, out var clrType)) return false;
        var entity = JsonSerializer.Deserialize(data.GetRawText(), clrType, RestoreJsonOptions);
        if (entity is null) return false;
        var metadata = db.Model.FindEntityType(clrType);
        var primaryKey = metadata?.FindPrimaryKey();
        if (metadata is null || primaryKey is null) return false;

        if (clrType == typeof(Data.Entities.PollBallotEntity))
        {
            var incomingBallot = (Data.Entities.PollBallotEntity)entity;
            var existingTokenHashes = await db.PollBallots.AsNoTracking()
                .Select(item => item.BallotTokenHash).ToListAsync();
            if (existingTokenHashes.Any(hash => hash.Length == incomingBallot.BallotTokenHash.Length &&
                CryptographicOperations.FixedTimeEquals(hash, incomingBallot.BallotTokenHash)))
                return false;
        }

        var incomingEntry = db.Entry(entity);
        var keyValues = primaryKey.Properties
            .Select(property => incomingEntry.Property(property.Name).CurrentValue).ToArray();
        if (keyValues.Any(value => value is null)) return false;
        var existing = await db.FindAsync(clrType, keyValues);
        if (existing is null)
        {
            db.Add(entity);
            return true;
        }

        // Append-only evidence may be restored when missing, but an existing row
        // must never be replaced, even when the caller requested overwrite.
        if (clrType == typeof(Data.Entities.AuditEventEntity) ||
            clrType == typeof(Data.Entities.PollBallotEntity) ||
            clrType == typeof(Data.Entities.PollLifecycleEventEntity) ||
            clrType == typeof(Data.Entities.BackupRetentionEventEntity))
            return false;

        if (!overwrite) return false;

        var existingEntry = db.Entry(existing);
        foreach (var property in metadata.GetProperties())
        {
            if (primaryKey.Properties.Contains(property) || property.IsConcurrencyToken ||
                property.ValueGenerated != Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never)
                continue;
            existingEntry.Property(property.Name).CurrentValue = incomingEntry.Property(property.Name).CurrentValue;
        }
        return true;
    }
    private static void ApplyOrAdd<T>(AppDbContext db, Microsoft.EntityFrameworkCore.DbSet<T> set, T? existing, T entity) where T : class
    {
        if (existing != null) db.Entry(existing).CurrentValues.SetValues(entity);
        else set.Add(entity);
    }

    private static string EncodeTranslationValueId(string key, string cultureCode) =>
        $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(key))}.{Convert.ToBase64String(Encoding.UTF8.GetBytes(cultureCode))}";

    private static (string Key, string CultureCode)? DecodeTranslationValueId(string id)
    {
        var separator = id.IndexOf('.');
        if (separator <= 0 || separator == id.Length - 1 || id.IndexOf('.', separator + 1) >= 0)
            return null;
        try
        {
            var key = Encoding.UTF8.GetString(Convert.FromBase64String(id[..separator]));
            var cultureCode = Encoding.UTF8.GetString(Convert.FromBase64String(id[(separator + 1)..]));
            return string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(cultureCode)
                ? null
                : (key, cultureCode);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Determines whether a restore file path is the temporary file created by the preview step:
    /// directly inside the temp folder and named <c>restore-&lt;32 hex digits&gt;.zip</c>.
    /// </summary>
    private static bool IsSafeRestoreFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            var full = Path.GetFullPath(path);
            var dir = Path.GetDirectoryName(full)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(dir, temp, StringComparison.OrdinalIgnoreCase)
                && System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(full), @"^restore-[0-9a-fA-F]{32}\.zip$");
        }
        catch { return false; }
    }
}
