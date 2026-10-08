using BuildingManagementMvc.Data;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class DataIntegrityService
{
    private readonly FirestoreDb _fs;
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly ILogger<DataIntegrityService> _log;

    public DataIntegrityService(
        FirestoreContext fsCtx,
        IDbContextFactory<AppDbContext> sqlFactory,
        ILogger<DataIntegrityService> log)
    {
        _fs = fsCtx.Db;
        _sqlFactory = sqlFactory;
        _log = log;
    }

    // ============================================================
    // 1. الإحصائيات الكاملة
    // ============================================================
    public async Task<DataIntegrityReport> GetFullReportAsync()
    {
        var report = new DataIntegrityReport
        {
            GeneratedAt = DateTime.UtcNow
        };

        // ========== Firestore Collections ==========
        var fsCollections = new[]
        {
            "buildings", "users", "auditLogs", "qr_usage",
            "qr-tokens", "presence", "dvrs", "cameras"
        };

        foreach (var col in fsCollections)
        {
            var count = await GetFirestoreCountAsync(col);
            report.FirestoreCollections.Add(new CollectionStat
            {
                Name = col,
                Count = count
            });
        }

        // ========== SQL Tables ==========
        await using var db = await _sqlFactory.CreateDbContextAsync();

        var sqlTables = new (string Table, Func<Task<int>> Count)[]
        {
            ("Users",           async () => await db.Users.CountAsync()),
            ("UserPermissions", async () => await db.UserPermissions.CountAsync()),
            ("Buildings",       async () => await db.Buildings.CountAsync()),
            ("BuildingAdmins",  async () => await db.BuildingAdmins.CountAsync()),
            ("Floors",          async () => await db.Floors.CountAsync()),
            ("Apartments",      async () => await db.Apartments.CountAsync()),
            ("FinancialCategories", async () => await db.FinancialCategories.CountAsync()),
            ("Expenses",        async () => await db.Expenses.CountAsync()),
            ("Revenues",        async () => await db.Revenues.CountAsync()),
            ("Deposits",        async () => await db.Deposits.CountAsync()),
            ("DepositEditHistory", async () => await db.DepositEditHistory.CountAsync()),
            ("MonthlyApartmentShares", async () => await db.MonthlyApartmentShares.CountAsync()),
            ("Notifications",   async () => await db.Notifications.CountAsync()),
            ("AuditLog",        async () => await db.AuditLog.CountAsync()),
            ("Polls",           async () => await db.Polls.CountAsync()),
            ("PollOptions",     async () => await db.PollOptions.CountAsync()),
            ("PollVotes",       async () => await db.PollVotes.CountAsync()),
            ("MaintenanceRecords", async () => await db.MaintenanceRecords.CountAsync()),
            ("Dvrs",            async () => await db.Dvrs.CountAsync()),
            ("Cameras",         async () => await db.Cameras.CountAsync()),
            ("Presence",        async () => await db.Presence.CountAsync()),
            ("QrTokens",        async () => await db.QrTokens.CountAsync()),
            ("QrUsages",        async () => await db.QrUsages.CountAsync()),  
            ("WalletAdjustments", async () => await db.WalletAdjustments.CountAsync()),
            ("SyncPendingChanges", async () => await db.SyncPendingChanges.CountAsync())
        };

        foreach (var (table, countFn) in sqlTables)
        {
            try
            {
                var count = await countFn();
                report.SqlTables.Add(new TableStat
                {
                    Name = table,
                    Count = count
                });
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Failed to count {Table}", table);
                report.SqlTables.Add(new TableStat
                {
                    Name = table,
                    Count = -1,
                    Error = ex.Message
                });
            }
        }

        // ========== SyncPendingChanges Details ==========
        try
        {
            report.PendingChanges = await db.SyncPendingChanges
                .Where(x => !x.Applied)
                .OrderBy(x => x.CreatedAt)
                .Take(50)
                .Select(x => new PendingChangeInfo
                {
                    Id = x.Id,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    Operation = x.Operation,
                    CreatedAt = x.CreatedAt,
                    RetryCount = x.RetryCount,
                    ErrorMessage = x.ErrorMessage
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load pending changes");
        }

        return report;
    }

    // ============================================================
    // 2. عدد documents في Firestore
    // ============================================================
    private async Task<int> GetFirestoreCountAsync(string collection)
    {
        try
        {
            var snapshot = await _fs.Collection(collection).GetSnapshotAsync();
            return snapshot.Documents.Count;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Firestore count failed for {Col}", collection);
            return -1;
        }
    }

    // ============================================================
    // 3. مقارنة Buildings (Firestore vs SQL)
    // ============================================================
    public async Task<List<BuildingComparison>> CompareBuildingsAsync()
    {
        var result = new List<BuildingComparison>();

        var fsSnapshot = await _fs.Collection("buildings").GetSnapshotAsync();
        var fsBuildings = fsSnapshot.Documents
            .Select(d => new
            {
                Id = d.Id,
                Name = d.ContainsField("name") ? d.GetValue<string>("name") : "",
                Number = d.ContainsField("buildingNumber") ? d.GetValue<string>("buildingNumber") : ""
            })
            .ToDictionary(x => x.Id, x => x);

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var sqlBuildings = await db.Buildings
            .Select(b => new { b.Id, b.Name, b.BuildingNumber })
            .ToDictionaryAsync(x => x.Id, x => x);

        var allIds = fsBuildings.Keys.Union(sqlBuildings.Keys).Distinct();

        foreach (var id in allIds)
        {
            var comp = new BuildingComparison { Id = id };

            if (fsBuildings.TryGetValue(id, out var fs))
            {
                comp.InFirestore = true;
                comp.FirestoreName = fs.Name;
                comp.FirestoreNumber = fs.Number;
            }

            if (sqlBuildings.TryGetValue(id, out var sql))
            {
                comp.InSql = true;
                comp.SqlName = sql.Name;
                comp.SqlNumber = sql.BuildingNumber;
            }

            comp.Status = (comp.InFirestore, comp.InSql) switch
            {
                (true, true) => "✅ موجود في الاتنين",
                (true, false) => "⚠️ في Firestore بس",
                (false, true) => "⚠️ في SQL بس",
                _ => "❌ مش موجود"
            };

            result.Add(comp);
        }

        return result.OrderBy(x => x.FirestoreNumber ?? x.SqlNumber).ToList();
    }

    // ============================================================
    // 4. مقارنة Users
    // ============================================================
    public async Task<List<UserComparison>> CompareUsersAsync()
    {
        var result = new List<UserComparison>();

        var fsSnapshot = await _fs.Collection("users").GetSnapshotAsync();
        var fsUsers = fsSnapshot.Documents
            .Select(d => new
            {
                Uid = d.Id,
                Email = d.ContainsField("email") ? d.GetValue<string>("email") : "",
                Role = d.ContainsField("role") ? d.GetValue<string>("role") : "",
                Name = d.ContainsField("name") ? d.GetValue<string>("name") : ""
            })
            .ToDictionary(x => x.Uid, x => x);

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var sqlUsers = await db.Users
            .Select(u => new { u.Uid, u.Email, u.Role, u.Name })
            .ToDictionaryAsync(x => x.Uid, x => x);

        var allIds = fsUsers.Keys.Union(sqlUsers.Keys).Distinct();

        foreach (var uid in allIds)
        {
            var comp = new UserComparison { Uid = uid };

            if (fsUsers.TryGetValue(uid, out var fs))
            {
                comp.InFirestore = true;
                comp.FirestoreEmail = fs.Email;
                comp.FirestoreRole = fs.Role;
                comp.FirestoreName = fs.Name;
            }

            if (sqlUsers.TryGetValue(uid, out var sql))
            {
                comp.InSql = true;
                comp.SqlEmail = sql.Email;
                comp.SqlRole = sql.Role;
                comp.SqlName = sql.Name;
            }

            comp.Status = (comp.InFirestore, comp.InSql) switch
            {
                (true, true) => "✅",
                (true, false) => "⚠️ FS",
                (false, true) => "⚠️ SQL",
                _ => "❌"
            };

            result.Add(comp);
        }

        return result.OrderBy(x => x.FirestoreRole ?? x.SqlRole).ToList();
    }

    // ============================================================
    // 5. مقارنة Dvrs
    // ============================================================
    public async Task<List<DvrComparison>> CompareDvrsAsync()
    {
        var result = new List<DvrComparison>();

        var fsSnapshot = await _fs.Collection("dvrs").GetSnapshotAsync();
        var fsDvrs = fsSnapshot.Documents
            .Select(d => new
            {
                Id = d.Id,
                Name = d.ContainsField("name") ? d.GetValue<string>("name") : ""
            })
            .ToDictionary(x => x.Id, x => x);

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var sqlDvrs = await db.Dvrs
            .Select(d => new { d.Id, d.Name, HasPassword = !string.IsNullOrEmpty(d.PasswordEncrypted) })
            .ToDictionaryAsync(x => x.Id, x => x);

        var allIds = fsDvrs.Keys.Union(sqlDvrs.Keys).Distinct();

        foreach (var id in allIds)
        {
            var comp = new DvrComparison { Id = id };

            if (fsDvrs.TryGetValue(id, out var fs))
            {
                comp.InFirestore = true;
                comp.FirestoreName = fs.Name;
            }

            if (sqlDvrs.TryGetValue(id, out var sql))
            {
                comp.InSql = true;
                comp.SqlName = sql.Name;
                comp.HasPassword = sql.HasPassword;
            }

            comp.Status = (comp.InFirestore, comp.InSql) switch
            {
                (true, true) => "✅",
                (true, false) => "⚠️ FS",
                (false, true) => "⚠️ SQL",
                _ => "❌"
            };

            result.Add(comp);
        }

        return result.OrderBy(x => x.FirestoreName ?? x.SqlName).ToList();
    }

    // ============================================================
    // 6. مقارنة Cameras
    // ============================================================
    public async Task<List<CameraComparison>> CompareCamerasAsync()
    {
        var result = new List<CameraComparison>();

        var fsSnapshot = await _fs.Collection("cameras").GetSnapshotAsync();
        var fsCameras = fsSnapshot.Documents
            .Select(d => new
            {
                Id = d.Id,
                Name = d.ContainsField("name") ? d.GetValue<string>("name") : ""
            })
            .ToDictionary(x => x.Id, x => x);

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var sqlCameras = await db.Cameras
            .Select(c => new { c.Id, c.Name, c.Channel })
            .ToDictionaryAsync(x => x.Id, x => x);

        var allIds = fsCameras.Keys.Union(sqlCameras.Keys).Distinct();

        foreach (var id in allIds)
        {
            var comp = new CameraComparison { Id = id };

            if (fsCameras.TryGetValue(id, out var fs))
            {
                comp.InFirestore = true;
                comp.FirestoreName = fs.Name;
            }

            if (sqlCameras.TryGetValue(id, out var sql))
            {
                comp.InSql = true;
                comp.SqlName = sql.Name;
                comp.Channel = sql.Channel;
            }

            comp.Status = (comp.InFirestore, comp.InSql) switch
            {
                (true, true) => "✅",
                (true, false) => "⚠️ FS",
                (false, true) => "⚠️ SQL",
                _ => "❌"
            };

            result.Add(comp);
        }

        return result.OrderBy(x => x.SqlName ?? x.FirestoreName).ToList();
    }
}

// ============================================================
// Models
// ============================================================
public class DataIntegrityReport
{
    public DateTime GeneratedAt { get; set; }
    public List<CollectionStat> FirestoreCollections { get; set; } = new();
    public List<TableStat> SqlTables { get; set; } = new();
    public List<PendingChangeInfo> PendingChanges { get; set; } = new();
}

public class CollectionStat
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class TableStat
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public string? Error { get; set; }
}

public class PendingChangeInfo
{
    public long Id { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Operation { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BuildingComparison
{
    public string Id { get; set; } = "";
    public bool InFirestore { get; set; }
    public bool InSql { get; set; }
    public string? FirestoreName { get; set; }
    public string? FirestoreNumber { get; set; }
    public string? SqlName { get; set; }
    public string? SqlNumber { get; set; }
    public string Status { get; set; } = "";
}

public class UserComparison
{
    public string Uid { get; set; } = "";
    public bool InFirestore { get; set; }
    public bool InSql { get; set; }
    public string? FirestoreEmail { get; set; }
    public string? FirestoreRole { get; set; }
    public string? FirestoreName { get; set; }
    public string? SqlEmail { get; set; }
    public string? SqlRole { get; set; }
    public string? SqlName { get; set; }
    public string Status { get; set; } = "";
}

public class DvrComparison
{
    public string Id { get; set; } = "";
    public bool InFirestore { get; set; }
    public bool InSql { get; set; }
    public string? FirestoreName { get; set; }
    public string? SqlName { get; set; }
    public bool HasPassword { get; set; }
    public string Status { get; set; } = "";
}

public class CameraComparison
{
    public string Id { get; set; } = "";
    public bool InFirestore { get; set; }
    public bool InSql { get; set; }
    public string? FirestoreName { get; set; }
    public string? SqlName { get; set; }
    public int Channel { get; set; }
    public string Status { get; set; } = "";
}