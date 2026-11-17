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
    private const long MaxExpandedBytes = 500L * 1024 * 1024;
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
    private static Dictionary<string, string> KnownTables => new()
    {
        ["globalSettings"] = Loc.T("Backup_Collection_globalSettings"),
        ["scheduledBackupSettings"] = Loc.T("Backup_Collection_scheduledBackupSettings"),
        ["users"] = Loc.T("Backup_Collection_users"),
        ["buildings"] = Loc.T("Backup_Collection_buildings"),
        ["buildingSettings"] = Loc.T("Backup_Collection_buildingSettings"),
        ["buildingAdmins"] = Loc.T("Backup_Collection_buildingAdmins"),
        ["floors"] = Loc.T("Backup_Collection_floors"),
        ["apartments"] = Loc.T("Backup_Collection_apartments"),
        ["residents"] = Loc.T("Backup_Collection_residents"),
        ["financialCategories"] = Loc.T("Backup_Collection_financialCategories"),
        ["expenses"] = Loc.T("Backup_Collection_expenses"),
        ["revenues"] = Loc.T("Backup_Collection_revenues"),
        ["monthlyApartmentShares"] = Loc.T("Backup_Collection_monthlyApartmentShares"),
        ["deposits"] = Loc.T("Backup_Collection_deposits"),
        ["depositEditHistory"] = Loc.T("Backup_Collection_depositEditHistory"),
        ["walletAdjustments"] = Loc.T("Backup_Collection_walletAdjustments"),
        ["userPermissions"] = Loc.T("Backup_Collection_userPermissions"),
        ["auditLog"] = Loc.T("Backup_Collection_auditLog"),
        ["notifications"] = Loc.T("Backup_Collection_notifications"),
        ["polls"] = Loc.T("Backup_Collection_polls"),
        ["pollOptions"] = Loc.T("Backup_Collection_pollOptions"),
        ["pollVotes"] = Loc.T("Backup_Collection_pollVotes"),
        ["maintenanceRecords"] = Loc.T("Backup_Collection_maintenanceRecords"),
        ["dvrs"] = Loc.T("Backup_Collection_dvrs"),
        ["cameras"] = Loc.T("Backup_Collection_cameras"),
        ["qrTokens"] = Loc.T("Backup_Collection_qrTokens"),
        ["qrUsages"] = Loc.T("Backup_Collection_qrUsages")
    };

    /// <summary>
    /// Gets the JSON options used when deserializing entities during a restore.
    /// </summary>
    /// <remarks>
    /// Property matching is case-insensitive so that backups created with a different
    /// serializer configuration still restore correctly, and null properties are ignored
    /// to avoid overwriting defaults with nulls.
    /// </remarks>
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

        // كل عملية بتحصل في context مستقل قصير العمر
        await using var db = await _sqlFactory.CreateDbContextAsync();

        foreach (var kv in KnownTables)
        {
            int count = 0;
            try
            {
                // نحسب عدد الصفوف لكل collection حسب نوعها
                count = kv.Key switch
                {
                    "globalSettings" => await db.GlobalSettings.CountAsync(),
                    "scheduledBackupSettings" => await db.ScheduledBackupSettings.CountAsync(),
                    "users" => await db.Users.CountAsync(),
                    "buildings" => await db.Buildings.CountAsync(),
                    "buildingSettings" => await db.BuildingSettings.CountAsync(),
                    "buildingAdmins" => await db.BuildingAdmins.CountAsync(),
                    "floors" => await db.Floors.CountAsync(),
                    "apartments" => await db.Apartments.CountAsync(),
                    "residents" => await db.Residents.CountAsync(),
                    "financialCategories" => await db.FinancialCategories.CountAsync(),
                    "expenses" => await db.Expenses.CountAsync(),
                    "revenues" => await db.Revenues.CountAsync(),
                    "monthlyApartmentShares" => await db.MonthlyApartmentShares.CountAsync(),
                    "deposits" => await db.Deposits.CountAsync(),
                    "depositEditHistory" => await db.DepositEditHistory.CountAsync(),
                    "walletAdjustments" => await db.WalletAdjustments.CountAsync(),
                    "userPermissions" => await db.UserPermissions.CountAsync(),
                    "auditLog" => await db.AuditLog.CountAsync(),
                    "notifications" => await db.Notifications.CountAsync(),
                    "polls" => await db.Polls.CountAsync(),
                    "pollOptions" => await db.PollOptions.CountAsync(),
                    "pollVotes" => await db.PollVotes.CountAsync(),
                    "maintenanceRecords" => await db.MaintenanceRecords.CountAsync(),
                    "dvrs" => await db.Dvrs.CountAsync(),
                    "cameras" => await db.Cameras.CountAsync(),
                    "qrTokens" => await db.QrTokens.CountAsync(),
                    "qrUsages" => await db.QrUsages.CountAsync(),
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                // فشل collection واحد مش لازم يوقف الصفحة كلها
                _log.LogWarning(ex, "Stats count failed for {Table}", kv.Key);
            }

            result.Add(new BackupStats
            {
                Collection = kv.Key,
                LabelAr = kv.Value,
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

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var tableName in selectedTables)
            {
                // تجاهل أي collection غير معروفة
                if (!KnownTables.ContainsKey(tableName)) continue;

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
        result.Success = true;
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
                fileSize = result.FileSize
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
        await using var db = await _sqlFactory.CreateDbContextAsync();
        var result = new List<Dictionary<string, object>>();

        switch (tableName)
        {
            case "globalSettings":
                foreach (var g in await db.GlobalSettings.AsNoTracking().ToListAsync())
                    result.Add(ToDict(g.Id, g));
                break;
            case "scheduledBackupSettings":
                foreach (var s in await db.ScheduledBackupSettings.AsNoTracking().ToListAsync())
                    result.Add(ToDict(s.Id, s));
                break;
            case "users":
                foreach (var u in await db.Users.AsNoTracking().ToListAsync())
                    result.Add(ToDict(u.Uid, u));
                break;
            case "buildings":
                foreach (var b in await db.Buildings.AsNoTracking().ToListAsync())
                    result.Add(ToDict(b.Id, b));
                break;
            case "buildingSettings":
                foreach (var bs in await db.BuildingSettings.AsNoTracking().ToListAsync())
                    result.Add(ToDict(bs.BuildingId, bs));
                break;
            case "buildingAdmins":
                // مفتاح مركّب: buildingId_adminUid
                foreach (var ba in await db.BuildingAdmins.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{ba.BuildingId}_{ba.AdminUid}", ba));
                break;
            case "floors":
                // مفتاح مركّب: buildingId_floorId
                foreach (var f in await db.Floors.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{f.BuildingId}_{f.Id}", f));
                break;
            case "apartments":
                // مفتاح مركّب: buildingId_apartmentId
                foreach (var a in await db.Apartments.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{a.BuildingId}_{a.Id}", a));
                break;
            case "residents":
                foreach (var r in await db.Residents.AsNoTracking().ToListAsync())
                    result.Add(ToDict(r.Id, r));
                break;
            case "financialCategories":
                // مفتاح مركّب: buildingId_kind_categoryId
                foreach (var c in await db.FinancialCategories.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{c.BuildingId}_{c.Kind}_{c.Id}", c));
                break;
            case "expenses":
                // مفتاح مركّب: buildingId_expenseId
                foreach (var e in await db.Expenses.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{e.BuildingId}_{e.Id}", e));
                break;
            case "revenues":
                // مفتاح مركّب: buildingId_revenueId
                foreach (var r in await db.Revenues.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{r.BuildingId}_{r.Id}", r));
                break;
            case "monthlyApartmentShares":
                // مفتاح مركّب: buildingId_monthKey_apartmentId
                foreach (var s in await db.MonthlyApartmentShares.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{s.BuildingId}_{s.MonthKey}_{s.ApartmentId}", s));
                break;
            case "deposits":
                // مفتاح مركّب: buildingId_depositId
                foreach (var d in await db.Deposits.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{d.BuildingId}_{d.Id}", d));
                break;
            case "depositEditHistory":
                foreach (var de in await db.DepositEditHistory.AsNoTracking().ToListAsync())
                    result.Add(ToDict(de.EditId.ToString(), de));
                break;
            case "walletAdjustments":
                // مفتاح مركّب: buildingId_adjustmentId
                foreach (var w in await db.WalletAdjustments.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{w.BuildingId}_{w.Id}", w));
                break;
            case "userPermissions":
                // مفتاح مركّب: uid_permission
                foreach (var p in await db.UserPermissions.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{p.Uid}_{p.Permission}", p));
                break;
            case "auditLog":
                foreach (var a in await db.AuditLog.AsNoTracking().ToListAsync())
                    result.Add(ToDict(a.AuditId.ToString(), a));
                break;
            case "notifications":
                // مفتاح مركّب: buildingId_notificationId
                foreach (var n in await db.Notifications.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{n.BuildingId}_{n.Id}", n));
                break;
            case "polls":
                // مفتاح مركّب: buildingId_pollId
                foreach (var p in await db.Polls.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{p.BuildingId}_{p.Id}", p));
                break;
            case "pollOptions":
                // مفتاح مركّب: buildingId_pollId_optionId
                foreach (var o in await db.PollOptions.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{o.BuildingId}_{o.PollId}_{o.Id}", o));
                break;
            case "pollVotes":
                // مفتاح مركّب: buildingId_pollId_apartmentId
                foreach (var v in await db.PollVotes.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{v.BuildingId}_{v.PollId}_{v.ApartmentId}", v));
                break;
            case "maintenanceRecords":
                // مفتاح مركّب: buildingId_recordId
                foreach (var m in await db.MaintenanceRecords.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{m.BuildingId}_{m.Id}", m));
                break;
            case "dvrs":
                foreach (var d in await db.Dvrs.AsNoTracking().ToListAsync())
                    result.Add(ToDict(d.Id, d));
                break;
            case "cameras":
                foreach (var c in await db.Cameras.AsNoTracking().ToListAsync())
                    result.Add(ToDict(c.Id, c));
                break;
            case "qrTokens":
                foreach (var q in await db.QrTokens.AsNoTracking().ToListAsync())
                    result.Add(ToDict(q.Uid, q));
                break;
            case "qrUsages":
                foreach (var q in await db.QrUsages.AsNoTracking().ToListAsync())
                    result.Add(ToDict(q.Id, q));
                break;
        }

        return result;
    }

    /// <summary>
    /// Wraps an entity into the <c>{ id, data }</c> shape used inside backup JSON files.
    /// </summary>
    /// <param name="id">The (possibly composite) primary key of the entity.</param>
    /// <param name="entity">The entity instance.</param>
    /// <returns>A dictionary with the row id and the serialized entity data.</returns>
    private static Dictionary<string, object> ToDict(string id, object entity)
    {
        return new Dictionary<string, object>
        {
            ["id"] = id,
            ["data"] = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(entity)) ?? new()
        };
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
            "globalSettings",
            "scheduledBackupSettings",
            "users",
            "buildings",
            "buildingSettings",
            "buildingAdmins",
            "floors",
            "apartments",
            "residents",
            "financialCategories",
            "expenses",
            "revenues",
            "monthlyApartmentShares",
            "deposits",
            "depositEditHistory",
            "walletAdjustments",
            "userPermissions",
            "auditLog",
            "notifications",
            "polls",
            "pollOptions",
            "pollVotes",
            "maintenanceRecords",
            "dvrs",
            "cameras",
            "qrTokens",
            "qrUsages"
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
        switch (tableName)
        {
            case "globalSettings":
                {
                    // لو موجود → نحذفه (لو overwrite) أو نتخطاه
                    var existing = await db.GlobalSettings.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.GlobalSettingsEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.GlobalSettings, existing, entity); }
                    return true;
                }

            case "scheduledBackupSettings":
                {
                    var existing = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ScheduledBackupSettingsEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.ScheduledBackupSettings, existing, entity); }
                    return true;
                }

            case "users":
                {
                    var existing = await db.Users.FirstOrDefaultAsync(x => x.Uid == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.UserEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Uid = id; ApplyOrAdd(db, db.Users, existing, entity); }
                    return true;
                }

            case "buildings":
                {
                    var existing = await db.Buildings.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.BuildingEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Buildings, existing, entity); }
                    return true;
                }

            case "buildingSettings":
                {
                    var existing = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.BuildingSettingsEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.BuildingId = id; ApplyOrAdd(db, db.BuildingSettings, existing, entity); }
                    return true;
                }

            case "buildingAdmins":
                {
                    // فك المفتاح المركّب buildingId_adminUid
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var adminUid = parts[1];

                    var existing = await db.BuildingAdmins
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.AdminUid == adminUid);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.BuildingAdminEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.AdminUid = adminUid;
                        ApplyOrAdd(db, db.BuildingAdmins, existing, entity);
                    }
                    return true;
                }

            case "floors":
                {
                    // فك المفتاح المركّب buildingId_floorId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var floorId = parts[1];

                    var existing = await db.Floors
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == floorId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.FloorEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = floorId;
                        ApplyOrAdd(db, db.Floors, existing, entity);
                    }
                    return true;
                }

            case "apartments":
                {
                    // فك المفتاح المركّب buildingId_apartmentId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var aptId = parts[1];

                    var existing = await db.Apartments
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == aptId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ApartmentEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = aptId;
                        ApplyOrAdd(db, db.Apartments, existing, entity);
                    }
                    return true;
                }

            case "residents":
                {
                    var existing = await db.Residents.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ResidentEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Residents, existing, entity); }
                    return true;
                }

            case "financialCategories":
                {
                    // فك المفتاح المركّب buildingId_kind_categoryId (categoryId ممكن يحتوي على _)
                    var parts = id.Split('_');
                    if (parts.Length < 3) return false;
                    var buildingId = parts[0];
                    var kind = parts[1];
                    var catId = string.Join("_", parts.Skip(2));

                    var existing = await db.FinancialCategories
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Kind == kind && x.Id == catId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.FinancialCategoryEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Kind = kind;
                        entity.Id = catId;
                        ApplyOrAdd(db, db.FinancialCategories, existing, entity);
                    }
                    return true;
                }

            case "expenses":
                {
                    // فك المفتاح المركّب buildingId_expenseId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var expId = parts[1];

                    var existing = await db.Expenses
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == expId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ExpenseEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = expId;
                        ApplyOrAdd(db, db.Expenses, existing, entity);
                    }
                    return true;
                }

            case "revenues":
                {
                    // فك المفتاح المركّب buildingId_revenueId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var revId = parts[1];

                    var existing = await db.Revenues
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == revId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.RevenueEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = revId;
                        ApplyOrAdd(db, db.Revenues, existing, entity);
                    }
                    return true;
                }

            case "monthlyApartmentShares":
                {
                    // فك المفتاح المركّب buildingId_monthKey_apartmentId
                    var parts = id.Split('_');
                    if (parts.Length != 3) return false;
                    var buildingId = parts[0];
                    var monthKey = parts[1];
                    var aptId = parts[2];

                    var existing = await db.MonthlyApartmentShares
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.MonthKey == monthKey && x.ApartmentId == aptId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.MonthlyApartmentShareEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.MonthKey = monthKey;
                        entity.ApartmentId = aptId;
                        ApplyOrAdd(db, db.MonthlyApartmentShares, existing, entity);
                    }
                    return true;
                }

            case "deposits":
                {
                    // فك المفتاح المركّب buildingId_depositId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var depId = parts[1];

                    var existing = await db.Deposits
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == depId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.DepositEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = depId;
                        ApplyOrAdd(db, db.Deposits, existing, entity);
                    }
                    return true;
                }

            case "depositEditHistory":
                {
                    // مفتاح رقمي بسيط
                    var existing = await db.DepositEditHistory.FirstOrDefaultAsync(x => x.EditId == long.Parse(id));
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.DepositEditEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { if (existing == null) entity.EditId = 0; ApplyOrAdd(db, db.DepositEditHistory, existing, entity); }
                    return true;
                }

            case "walletAdjustments":
                {
                    // فك المفتاح المركّب buildingId_adjustmentId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var adjId = parts[1];

                    var existing = await db.WalletAdjustments
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == adjId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.WalletAdjustmentEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = adjId;
                        ApplyOrAdd(db, db.WalletAdjustments, existing, entity);
                    }
                    return true;
                }

            case "userPermissions":
                {
                    // فك المفتاح المركّب uid_permission
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var uid = parts[0];
                    var permission = parts[1];

                    var existing = await db.UserPermissions
                        .FirstOrDefaultAsync(x => x.Uid == uid && x.Permission == permission);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.UserPermissionEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.Uid = uid;
                        entity.Permission = permission;
                        ApplyOrAdd(db, db.UserPermissions, existing, entity);
                    }
                    return true;
                }

            case "auditLog":
                {
                    // جدول append-only → نضيف دايماً بدون حذف
                    var entity = JsonSerializer.Deserialize<Data.Entities.AuditLogEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.AuditId = 0; db.AuditLog.Add(entity); }   // الـ id بيتولّد من الداتابيز (identity)
                    return true;
                }

            case "notifications":
                {
                    // فك المفتاح المركّب buildingId_notificationId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var notifId = parts[1];

                    var existing = await db.Notifications
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == notifId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.NotificationEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = notifId;
                        ApplyOrAdd(db, db.Notifications, existing, entity);
                    }
                    return true;
                }

            case "polls":
                {
                    // فك المفتاح المركّب buildingId_pollId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var pollId = parts[1];

                    var existing = await db.Polls
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == pollId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.PollEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = pollId;
                        ApplyOrAdd(db, db.Polls, existing, entity);
                    }
                    return true;
                }

            case "pollOptions":
                {
                    // فك المفتاح المركّب buildingId_pollId_optionId
                    var parts = id.Split('_');
                    if (parts.Length != 3) return false;
                    var buildingId = parts[0];
                    var pollId = parts[1];
                    var optionId = parts[2];

                    var existing = await db.PollOptions
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.PollId == pollId && x.Id == optionId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.PollOptionEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.PollId = pollId;
                        entity.Id = optionId;
                        ApplyOrAdd(db, db.PollOptions, existing, entity);
                    }
                    return true;
                }

            case "pollVotes":
                {
                    // فك المفتاح المركّب buildingId_pollId_apartmentId
                    var parts = id.Split('_');
                    if (parts.Length != 3) return false;
                    var buildingId = parts[0];
                    var pollId = parts[1];
                    var aptId = parts[2];

                    var existing = await db.PollVotes
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.PollId == pollId && x.ApartmentId == aptId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.PollVoteEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.PollId = pollId;
                        entity.ApartmentId = aptId;
                        ApplyOrAdd(db, db.PollVotes, existing, entity);
                    }
                    return true;
                }

            case "maintenanceRecords":
                {
                    // فك المفتاح المركّب buildingId_recordId
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var recordId = parts[1];

                    var existing = await db.MaintenanceRecords
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == recordId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.MaintenanceRecordEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = recordId;
                        ApplyOrAdd(db, db.MaintenanceRecords, existing, entity);
                    }
                    return true;
                }

            case "dvrs":
                {
                    var existing = await db.Dvrs.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.DvrEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Dvrs, existing, entity); }
                    return true;
                }

            case "cameras":
                {
                    var existing = await db.Cameras.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.CameraEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.Cameras, existing, entity); }
                    return true;
                }

            case "qrTokens":
                {
                    var existing = await db.QrTokens.FirstOrDefaultAsync(x => x.Uid == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.QrTokenEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Uid = id; ApplyOrAdd(db, db.QrTokens, existing, entity); }
                    return true;
                }

            case "qrUsages":
                {
                    var existing = await db.QrUsages.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        // التحديث بيتم في المكان (SetValues) — مفيش حذف، عشان الـ cascade ماتمسحش بيانات مرتبطة
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.QrUsageEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; ApplyOrAdd(db, db.QrUsages, existing, entity); }
                    return true;
                }

            default:
                // collection غير معروفة → نتخطاها
                return false;
        }
    }

    /// <summary>
    /// Previews the restore.
    /// </summary>
    /// <remarks>
    /// The uploaded stream is first copied into a temporary file, then decrypted via
    /// <see cref="BackupFileProtector.Unprotect"/>, then opened as a ZIP. Every JSON entry
    /// whose name matches a known collection is counted and added to the preview. The
    /// An opaque, single-use session identifier is returned so the browser never receives
    /// the server-side temporary file path.
    /// </remarks>
    /// <param name="zipStream">The uploaded ZIP stream (may be encrypted).</param>
    /// <param name="fileName">The original file name (shown in the preview).</param>
    /// <param name="userId">The id of the user performing the preview (reserved for audit).</param>
    /// <returns>A <see cref="RestorePreview"/> describing the collections inside the archive.</returns>
    public async Task<RestorePreview> PreviewRestoreAsync(Stream zipStream, string fileName, string userId)
    {
        var result = new RestorePreview { FileName = Path.GetFileName(fileName ?? string.Empty) };
        string? localsTempPath = null;

        try
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new InvalidDataException("A valid restore user is required.");

            CleanupExpiredRestoreSessions(DateTime.UtcNow);

            // Read with a hard compressed-size limit before decryption.
            using var ms = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await zipStream.ReadAsync(buffer)) > 0)
            {
                if (ms.Length + read > MaxArchiveBytes)
                    throw new InvalidDataException("Backup archive exceeds the maximum allowed size.");
                await ms.WriteAsync(buffer.AsMemory(0, read));
            }

            var fileBytes = ms.ToArray();
            var decryptedBytes = BackupFileProtector.Unprotect(fileBytes);
            if (decryptedBytes.LongLength == 0 || decryptedBytes.LongLength > MaxArchiveBytes)
                throw new InvalidDataException("Decrypted backup has an invalid size.");

            var tempPath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
            localsTempPath = tempPath;
            await File.WriteAllBytesAsync(tempPath, decryptedBytes);
            result.TempFilePath = tempPath;
            result.FileSize = decryptedBytes.LongLength;

            using (var zip = ZipFile.OpenRead(tempPath))
            {
                var previews = await BackupArchiveValidator.ValidateAsync(zip);
                result.Collections.AddRange(previews);
            }

            var sessionId = Guid.NewGuid().ToString("N");
            var digest = Convert.ToHexString(SHA256.HashData(decryptedBytes));
            var session = new RestoreSession(
                tempPath, userId, DateTime.UtcNow.Add(RestoreSessionLifetime), digest);

            if (!TryAddRestoreSession(sessionId, session))
                throw new InvalidOperationException("The active restore-session limit has been reached.");

            result.RestoreSessionId = sessionId;
            result.Success = true;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Backup restore preview rejected for user {UserId}", userId);
            result.Success = false;
            result.Collections.Clear();
            result.Errors.Add(Loc.T("The_Data_Is_Corrupted_Upload_The"));
            if (result.RestoreSessionId == null && localsTempPath != null)
            {
                try { File.Delete(localsTempPath); } catch { }
            }
        }

        return result;
    }

    private static bool TryAddRestoreSession(string sessionId, RestoreSession session)
    {
        lock (RestoreSessionsLock)
        {
            CleanupExpiredRestoreSessionsUnderLock(DateTime.UtcNow);
            if (RestoreSessions.Count >= MaxActiveRestoreSessions ||
                RestoreSessions.Values.Count(x => string.Equals(x.UserId, session.UserId, StringComparison.Ordinal)) >= MaxActiveRestoreSessionsPerUser)
            {
                return false;
            }

            return RestoreSessions.TryAdd(sessionId, session);
        }
    }

    private static void CleanupExpiredRestoreSessions(DateTime nowUtc)
    {
        lock (RestoreSessionsLock)
            CleanupExpiredRestoreSessionsUnderLock(nowUtc);
    }

    private static void CleanupExpiredRestoreSessionsUnderLock(DateTime nowUtc)
    {
        foreach (var item in RestoreSessions.ToArray())
        {
            if (item.Value.ExpiresAtUtc > nowUtc)
                continue;

            if (RestoreSessions.TryRemove(item.Key, out var expired))
            {
                try { File.Delete(expired.Path); } catch { }
            }
        }
    }

    /// <summary>
    /// Adds the entity, or copies its values into the already-tracked existing row.
    /// </summary>
    /// <remarks>
    /// Updating in place (instead of delete + insert) avoids database cascade deletes that would
    /// remove related rows that are not part of the restore, and avoids duplicate-key tracking errors.
    /// </remarks>
    private static void ApplyOrAdd<T>(AppDbContext db, Microsoft.EntityFrameworkCore.DbSet<T> set, T? existing, T entity) where T : class
    {
        if (existing != null) db.Entry(existing).CurrentValues.SetValues(entity);
        else set.Add(entity);
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