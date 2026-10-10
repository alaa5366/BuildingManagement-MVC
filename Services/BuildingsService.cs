using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BuildingManagementMvc.Services;

public class BuildingsService
{
    private readonly FirestoreDb _db;
    private readonly BuildingSqlStore? _sql;
    private readonly StorageSettingsService _storageSettings;
    private readonly IDbContextFactory<AppDbContext>? _sqlFactory;
    private readonly bool _useSqlFromConfig;
    private readonly ILogger<BuildingsService>? _log;

    public BuildingsService(
        FirestoreContext ctx,
        IConfiguration cfg,
        BuildingSqlStore? sql = null,
        StorageSettingsService? storageSettings = null,
        IDbContextFactory<AppDbContext>? sqlFactory = null,
        ILogger<BuildingsService>? log = null)
    {
        _db = ctx.Db;
        _sql = sql;
        _storageSettings = storageSettings
            ?? throw new InvalidOperationException("StorageSettingsService مطلوب");
        _sqlFactory = sqlFactory;
        _useSqlFromConfig = string.Equals(
            cfg["Storage:Provider"], "Sql", StringComparison.OrdinalIgnoreCase);
        _log = log;

        if (_useSqlFromConfig && _sql == null)
            throw new InvalidOperationException("Storage:Provider=Sql لكن BuildingSqlStore مش متسجّل.");
    }

    private CollectionReference Col => _db.Collection("buildings");

    private async Task<string> GetModeAsync()
    {
        if (_storageSettings == null)
            return _useSqlFromConfig ? "Sql" : "Firestore";
        return await _storageSettings.GetModeAsync();
    }

    private async Task RecordChangeAsync(string entityId, string operation, object? payload = null)
    {
        if (_sqlFactory == null) return;
        try
        {
            await using var db = await _sqlFactory.CreateDbContextAsync();
            db.SyncPendingChanges.Add(new SyncPendingChangeEntity
            {
                EntityType = "Building",
                EntityId = entityId,
                Operation = operation,
                Payload = payload == null ? null : JsonSerializer.Serialize(payload),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _log?.LogWarning(ex, "Failed to record change for {Id}", entityId);
        }
    }

    // ============================================================
    // ✅ القراءة
    // ============================================================
    public async Task<List<Building>> GetAllAsync()
    {
        var mode = await GetModeAsync();

        if (mode == "Sql" && _sql != null)
        {
            var sqlList = await _sql.GetAllAsync();
            foreach (var b in sqlList) NormalizeOrder(b);
            return sqlList;
        }

        var snap = await Col.OrderBy("buildingNumber").GetSnapshotAsync();
        var buildings = snap.Documents.Select(d => d.ConvertTo<Building>()).ToList();
        foreach (var b in buildings) NormalizeOrder(b);
        return buildings;
    }

    public async Task<Building?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var mode = await GetModeAsync();

        if (mode == "Sql" && _sql != null)
        {
            var sqlBuilding = await _sql.GetByIdAsync(id);
            if (sqlBuilding != null) NormalizeOrder(sqlBuilding);
            return sqlBuilding;
        }

        var doc = await Col.Document(id).GetSnapshotAsync();
        if (!doc.Exists) return null;

        var building = doc.ConvertTo<Building>();
        NormalizeOrder(building);
        return building;
    }

    // ============================================================
    // ✅ الكتابة
    // ============================================================
    public async Task<(string Id, string BuildingNumber)> CreateAsync(
        string name, string adminPin, string adminWhatsapp = "",
        string? logoUrl = null, string? buildingNumber = null,
        bool addDefaultExpenseCategories = true,
        bool addDefaultRevenueCategories = true)
    {
        var mode = await GetModeAsync();

        string nextNumber;
        if (!string.IsNullOrWhiteSpace(buildingNumber))
        {
            var all = await GetAllAsync();
            if (all.Any(b => b.BuildingNumber == buildingNumber))
                throw new InvalidOperationException(
                    Loc.T("Building_Number_N_Is_Already_In", buildingNumber));
            nextNumber = buildingNumber.Trim();
        }
        else
        {
            var all = await GetAllAsync();
            var maxNum = 0;
            foreach (var b in all)
            {
                var m = Regex.Match(b.BuildingNumber ?? "", @"^BLD-(\d+)$");
                if (m.Success) maxNum = Math.Max(maxNum, int.Parse(m.Groups[1].Value));
            }
            nextNumber = "BLD-" + (maxNum + 1).ToString("D3");
        }

        var building = new Building
        {
            BuildingNumber = nextNumber,
            Name = name,
            AdminPin = adminPin,
            AdminWhatsapp = AuthHelpers.NormalizePhone(adminWhatsapp),
            LogoUrl = logoUrl ?? "",
            DataVersion = "3.2",
            ExpenseCategories = addDefaultExpenseCategories ? DefaultExpenseCategories() : new(),
            RevenueCategories = addDefaultRevenueCategories ? DefaultRevenueCategories() : new(),
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        if (mode == "Sql" && _sql != null)
        {
            building.Id = Guid.NewGuid().ToString("N");
            await _sql.SaveFullAsync(building);
            await RecordChangeAsync(building.Id, "Insert", building);
            return (building.Id, nextNumber);
        }

        var docRef = await Col.AddAsync(building);
        building.Id = docRef.Id;

        if (mode == "Dual" && _sql != null)
        {
            try { await _sql.SaveFullAsync(building); }
            catch (Exception ex)
            {
                SqlMirrorHealth.RecordFailure($"Create {building.Id}", ex);
                _log?.LogError(ex, "SQL mirror failed for Create {Id}", building.Id);
            }
        }

        return (building.Id, nextNumber);
    }

    public async Task DeleteAsync(string id)
    {
        var mode = await GetModeAsync();

        if (mode == "Firestore" || mode == "Dual")
            await Col.Document(id).DeleteAsync();

        if ((mode == "Sql" || mode == "Dual") && _sql != null)
        {
            try
            {
                await _sql.DeleteAsync(id);
                if (mode == "Sql")
                    await RecordChangeAsync(id, "Delete");
            }
            catch (Exception ex)
            {
                SqlMirrorHealth.RecordFailure($"Delete {id}", ex);
                _log?.LogError(ex, "SQL Delete failed for {Id}", id);
                if (mode == "Sql") throw;
            }
        }
    }

    public async Task SaveFullAsync(Building building)
    {
        var mode = await GetModeAsync();

        if (mode == "Firestore" || mode == "Dual")
            await Col.Document(building.Id).SetAsync(building, SetOptions.Overwrite);

        if ((mode == "Sql" || mode == "Dual") && _sql != null)
        {
            try
            {
                await _sql.SaveFullAsync(building);
                SqlMirrorHealth.RecordSuccess();

                if (mode == "Sql")
                    await RecordChangeAsync(building.Id, "Update", building);
            }
            catch (Exception ex)
            {
                SqlMirrorHealth.RecordFailure($"SaveFull {building.Id}", ex);
                _log?.LogError(ex, "SQL SaveFull failed for {Id}", building.Id);
                if (mode == "Sql") throw;
            }
        }
    }

    public async Task UpdateAsync(string id, Dictionary<string, object> partialData)
    {
        var mode = await GetModeAsync();
        if (mode == "Sql")
            throw new NotSupportedException("UpdateAsync غير مدعوم في وضع SQL.");

        partialData["dataVersion"] = "3.2";
        await Col.Document(id).SetAsync(partialData, SetOptions.MergeAll);
    }

    // ============================================================
    // ✅ إضافة أدوار وشقق (بدون Firebase Auth)
    // ============================================================
    public async Task<string> AddFloorAsync(string buildingId, string label, List<(string Phone, string Pin)> apartments)
    {
        var building = await GetByIdAsync(buildingId)
            ?? throw new InvalidOperationException("building-not-found");

        var maxOrder = building.Floors.Count == 0 ? -1 : building.Floors.Max(f => f.Order);
        var floorOrder = maxOrder + 1;
        var floorId = Guid.NewGuid().ToString("N");
        building.Floors.Add(new Floor { Id = floorId, Label = label, Order = floorOrder });

        var number = building.Apartments.Count == 0
            ? 1
            : building.Apartments.Max(a => a.Number) + 1;

        foreach (var (phoneRaw, pin) in apartments)
        {
            var phone = AuthHelpers.NormalizePhone(phoneRaw);
            building.Apartments.Add(new Apartment
            {
                Id = Guid.NewGuid().ToString("N"),
                FloorId = floorId,
                Number = number,
                Phone = phone,
                MonthlyFee = 0,
                Pin = pin,
                Closed = false,
                OpenDate = DateTime.UtcNow.ToString("o")
            });

            number++;
        }

        await SaveFullAsync(building);
        return floorId;
    }

    public async Task AddApartmentAsync(string buildingId, string floorId, string phoneRaw, string pin)
    {
        var building = await GetByIdAsync(buildingId)
            ?? throw new InvalidOperationException("building-not-found");

        var floor = building.Floors.FirstOrDefault(f => f.Id == floorId)
            ?? throw new InvalidOperationException("floor-not-found");

        var nextNum = building.Apartments.Count == 0
            ? 1
            : building.Apartments.Max(a => a.Number) + 1;
        var phone = AuthHelpers.NormalizePhone(phoneRaw);

        building.Apartments.Add(new Apartment
        {
            Id = Guid.NewGuid().ToString("N"),
            FloorId = floorId,
            Number = nextNum,
            Phone = phone,
            MonthlyFee = 0,
            Pin = pin,
            Closed = false,
            OpenDate = DateTime.UtcNow.ToString("o")
        });

        await SaveFullAsync(building);
    }

    public static List<FinancialCategory> DefaultExpenseCategories() => new()
    {
        new FinancialCategory{ Id="water",       Name="مياه",   Color="#4A90D9", Active=true, Order=0 },
        new FinancialCategory{ Id="electricity", Name="كهرباء", Color="#D9B34A", Active=true, Order=1 },
        new FinancialCategory{ Id="cleaning",    Name="نظافة",  Color="#4FB286", Active=true, Order=2 },
        new FinancialCategory{ Id="other",       Name="أخرى",   Color="#A0432A", Active=true, Order=3 },
    };

    public static List<FinancialCategory> DefaultRevenueCategories() => new()
    {
        new FinancialCategory{ Id="rent-roof",     Name="تأجير السطح", Color="#2E7D5B", Active=true, Order=0 },
        new FinancialCategory{ Id="rent-shop",     Name="تأجير محل",   Color="#4FB286", Active=true, Order=1 },
        new FinancialCategory{ Id="scrap",         Name="بيع خردة",    Color="#8E6BB2", Active=true, Order=2 },
        new FinancialCategory{ Id="other-revenue", Name="إيراد آخر",   Color="#B9853B", Active=true, Order=3 },
    };

    private static void NormalizeOrder(Building building)
    {
        building.Floors = building.Floors.OrderBy(f => f.Order).ToList();
        var floorOrderMap = building.Floors.ToDictionary(f => f.Id, f => f.Order);
        building.Apartments = building.Apartments
            .OrderBy(a => floorOrderMap.GetValueOrDefault(a.FloorId, int.MaxValue))
            .ThenBy(a => a.Number)
            .ToList();
    }

    public async Task UpdateApartmentAsync(
        string buildingId, string aptId, string owner, string label,
        string phone, string pin, double monthlyFee, string notes, string? email = null)
    {
        var building = await GetByIdAsync(buildingId)
            ?? throw new InvalidOperationException("building-not-found");

        var apt = building.Apartments.FirstOrDefault(a => a.Id == aptId)
            ?? throw new InvalidOperationException("apt-not-found");

        apt.Owner = owner ?? "";
        apt.Label = label ?? "";
        apt.Phone = AuthHelpers.NormalizePhone(phone);
        apt.Pin = pin;
        apt.MonthlyFee = monthlyFee;
        apt.Notes = notes ?? "";
        if (!string.IsNullOrWhiteSpace(email))
            apt.Email = email;

        await SaveFullAsync(building);
    }

    // ============================================================
    // ✅ خصائص StorageSyncController + المزامنة
    // ============================================================
    public string StorageProvider => _useSqlFromConfig ? "Sql" : "Firestore";
    public bool SqlMirrorEnabled => _sql != null;

    public async Task<SyncReport> SyncAllToSqlAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = new SyncReport();

        if (_sql == null)
        {
            report.Errors.Add("BuildingSqlStore مش مسجّل.");
            return report;
        }

        try
        {
            var snap = await Col.GetSnapshotAsync();
            var buildings = snap.Documents.Select(d => d.ConvertTo<Building>()).ToList();

            foreach (var b in buildings)
            {
                try
                {
                    NormalizeOrder(b);
                    await _sql.SaveFullAsync(b);
                    report.Buildings++;
                }
                catch (Exception ex)
                {
                    report.Errors.Add($"عمارة {b.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            report.Errors.Add($"خطأ عام: {ex.Message}");
        }

        sw.Stop();
        report.Elapsed = sw.Elapsed;
        SqlMirrorHealth.RecordSuccess();
        return report;
    }
}