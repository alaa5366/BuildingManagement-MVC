using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BuildingManagementMvc.Services;

public class BackupService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IAuditLogger _audit;
    private readonly ILogger<BackupService> _log;

    private static Dictionary<string, string> KnownTables => new()
    {
        ["globalSettings"] = "الإعدادات العامة",
        ["scheduledBackupSettings"] = "إعدادات النسخ المجدولة",
        ["users"] = "المستخدمون",
        ["buildings"] = "العمارات",
        ["buildingSettings"] = "إعدادات العمارات",
        ["buildingAdmins"] = "أدمن العمارات",
        ["floors"] = "الأدوار",
        ["apartments"] = "الشقق",
        ["residents"] = "السكان",
        ["financialCategories"] = "الفئات المالية",
        ["expenses"] = "المصروفات",
        ["revenues"] = "الإيرادات",
        ["monthlyApartmentShares"] = "حصص الشقق الشهرية",
        ["deposits"] = "الدفعات",
        ["depositEditHistory"] = "سجل تعديل الدفعات",
        ["walletAdjustments"] = "تعديلات المحفظة",
        ["userPermissions"] = "صلاحيات المستخدمين",
        ["auditLog"] = "سجل التدقيق",
        ["notifications"] = "الإشعارات",
        ["polls"] = "التصويتات",
        ["pollOptions"] = "خيارات التصويت",
        ["pollVotes"] = "أصوات التصويت",
        ["maintenanceRecords"] = "سجلات الصيانة",
        ["dvrs"] = "أجهزة التسجيل",
        ["cameras"] = "الكاميرات",
        ["qrTokens"] = "رموز QR",
        ["qrUsages"] = "استخدام رموز QR"
    };

    private static readonly JsonSerializerOptions RestoreJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public BackupService(
        IDbContextFactory<AppDbContext> sqlFactory,
        IAuditLogger audit,
        ILogger<BackupService> log)
    {
        _sqlFactory = sqlFactory;
        _audit = audit;
        _log = log;
    }

    public static IReadOnlyDictionary<string, string> GetKnownCollections() => KnownTables;

    public async Task<List<BackupStats>> GetStatsAsync()
    {
        var result = new List<BackupStats>();
        await using var db = await _sqlFactory.CreateDbContextAsync();

        foreach (var kv in KnownTables)
        {
            int count = 0;
            try
            {
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
                if (!KnownTables.ContainsKey(tableName)) continue;

                try
                {
                    var docs = await ReadTableAsync(tableName);
                    var json = JsonSerializer.Serialize(docs, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    AddFileToZip(zip, $"{tableName}.json", json);
                    result.Collections.Add(tableName);
                    result.TotalDocuments += docs.Count;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{tableName}: {ex.Message}");
                }
            }

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
                foreach (var ba in await db.BuildingAdmins.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{ba.BuildingId}_{ba.AdminUid}", ba));
                break;
            case "floors":
                foreach (var f in await db.Floors.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{f.BuildingId}_{f.Id}", f));
                break;
            case "apartments":
                foreach (var a in await db.Apartments.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{a.BuildingId}_{a.Id}", a));
                break;
            case "residents":
                foreach (var r in await db.Residents.AsNoTracking().ToListAsync())
                    result.Add(ToDict(r.Id, r));
                break;
            case "financialCategories":
                foreach (var c in await db.FinancialCategories.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{c.BuildingId}_{c.Kind}_{c.Id}", c));
                break;
            case "expenses":
                foreach (var e in await db.Expenses.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{e.BuildingId}_{e.Id}", e));
                break;
            case "revenues":
                foreach (var r in await db.Revenues.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{r.BuildingId}_{r.Id}", r));
                break;
            case "monthlyApartmentShares":
                foreach (var s in await db.MonthlyApartmentShares.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{s.BuildingId}_{s.MonthKey}_{s.ApartmentId}", s));
                break;
            case "deposits":
                foreach (var d in await db.Deposits.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{d.BuildingId}_{d.Id}", d));
                break;
            case "depositEditHistory":
                foreach (var de in await db.DepositEditHistory.AsNoTracking().ToListAsync())
                    result.Add(ToDict(de.EditId.ToString(), de));
                break;
            case "walletAdjustments":
                foreach (var w in await db.WalletAdjustments.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{w.BuildingId}_{w.Id}", w));
                break;
            case "userPermissions":
                foreach (var p in await db.UserPermissions.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{p.Uid}_{p.Permission}", p));
                break;
            case "auditLog":
                foreach (var a in await db.AuditLog.AsNoTracking().ToListAsync())
                    result.Add(ToDict(a.AuditId.ToString(), a));
                break;
            case "notifications":
                foreach (var n in await db.Notifications.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{n.BuildingId}_{n.Id}", n));
                break;
            case "polls":
                foreach (var p in await db.Polls.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{p.BuildingId}_{p.Id}", p));
                break;
            case "pollOptions":
                foreach (var o in await db.PollOptions.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{o.BuildingId}_{o.PollId}_{o.Id}", o));
                break;
            case "pollVotes":
                foreach (var v in await db.PollVotes.AsNoTracking().ToListAsync())
                    result.Add(ToDict($"{v.BuildingId}_{v.PollId}_{v.ApartmentId}", v));
                break;
            case "maintenanceRecords":
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

    private static Dictionary<string, object> ToDict(string id, object entity)
    {
        return new Dictionary<string, object>
        {
            ["id"] = id,
            ["data"] = JsonSerializer.Deserialize<Dictionary<string, object>>(
                JsonSerializer.Serialize(entity)) ?? new()
        };
    }

    private static void AddFileToZip(ZipArchive zip, string fileName, string content)
    {
        var entry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    public async Task<RestoreResult> RestoreAsync(
    string tempFilePath, List<string> selectedTables, bool overwrite, string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new RestoreResult();

        if (!File.Exists(tempFilePath))
        {
            result.Errors.Add(Loc.T("The_Temporary_File_Does_Not_Exist"));
            return result;
        }

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

        var orderedTables = restoreOrder
            .Where(t => selectedTables.Contains(t))
            .ToList();

        try
        {
            using var zip = ZipFile.OpenRead(tempFilePath);

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
                                var success = await RestoreEntityAsync(db, tableName, id, data, overwrite);
                                if (success) restored++;
                                else skipped++;
                            }
                            catch (Exception ex)
                            {
                                result.Errors.Add($"{tableName}/{id}: {ex.Message}");
                                skipped++;
                            }
                        }

                        result.Restored += restored;
                        result.Skipped += skipped;
                        result.Details.Add($"{tableName}: {restored} restored, {skipped} skipped");
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"{tableName}: {ex.Message}");
                        if (tx != null) await tx.RollbackAsync();
                        throw;
                    }
                }

                if (tx != null) await tx.CommitAsync();
                result.Success = result.Errors.Count == 0;
            }
            catch
            {
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
            result.Success = false;
            result.Errors.Add(ex.Message);
        }
        finally
        {
            try { File.Delete(tempFilePath); } catch { }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

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

    private async Task<bool> RestoreEntityAsync(
        AppDbContext db, string tableName, string id, JsonElement data, bool overwrite)
    {
        switch (tableName)
        {
            case "globalSettings":
                {
                    var existing = await db.GlobalSettings.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.GlobalSettings.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.GlobalSettingsEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; db.GlobalSettings.Add(entity); }
                    return true;
                }

            case "scheduledBackupSettings":
                {
                    var existing = await db.ScheduledBackupSettings.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.ScheduledBackupSettings.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ScheduledBackupSettingsEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; db.ScheduledBackupSettings.Add(entity); }
                    return true;
                }

            case "users":
                {
                    var existing = await db.Users.FirstOrDefaultAsync(x => x.Uid == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Users.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.UserEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Uid = id; db.Users.Add(entity); }
                    return true;
                }

            case "buildings":
                {
                    var existing = await db.Buildings.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Buildings.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.BuildingEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; db.Buildings.Add(entity); }
                    return true;
                }

            case "buildingSettings":
                {
                    var existing = await db.BuildingSettings.FirstOrDefaultAsync(x => x.BuildingId == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.BuildingSettings.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.BuildingSettingsEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.BuildingId = id; db.BuildingSettings.Add(entity); }
                    return true;
                }

            case "buildingAdmins":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var adminUid = parts[1];

                    var existing = await db.BuildingAdmins
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.AdminUid == adminUid);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.BuildingAdmins.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.BuildingAdminEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.AdminUid = adminUid;
                        db.BuildingAdmins.Add(entity);
                    }
                    return true;
                }

            case "floors":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var floorId = parts[1];

                    var existing = await db.Floors
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == floorId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Floors.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.FloorEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = floorId;
                        db.Floors.Add(entity);
                    }
                    return true;
                }

            case "apartments":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var aptId = parts[1];

                    var existing = await db.Apartments
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == aptId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Apartments.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ApartmentEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = aptId;
                        db.Apartments.Add(entity);
                    }
                    return true;
                }

            case "residents":
                {
                    var existing = await db.Residents.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Residents.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ResidentEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; db.Residents.Add(entity); }
                    return true;
                }

            case "financialCategories":
                {
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
                        db.FinancialCategories.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.FinancialCategoryEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Kind = kind;
                        entity.Id = catId;
                        db.FinancialCategories.Add(entity);
                    }
                    return true;
                }

            case "expenses":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var expId = parts[1];

                    var existing = await db.Expenses
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == expId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Expenses.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.ExpenseEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = expId;
                        db.Expenses.Add(entity);
                    }
                    return true;
                }

            case "revenues":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var revId = parts[1];

                    var existing = await db.Revenues
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == revId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Revenues.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.RevenueEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = revId;
                        db.Revenues.Add(entity);
                    }
                    return true;
                }

            case "monthlyApartmentShares":
                {
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
                        db.MonthlyApartmentShares.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.MonthlyApartmentShareEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.MonthKey = monthKey;
                        entity.ApartmentId = aptId;
                        db.MonthlyApartmentShares.Add(entity);
                    }
                    return true;
                }

            case "deposits":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var depId = parts[1];

                    var existing = await db.Deposits
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == depId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Deposits.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.DepositEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = depId;
                        db.Deposits.Add(entity);
                    }
                    return true;
                }

            case "depositEditHistory":
                {
                    var existing = await db.DepositEditHistory.FirstOrDefaultAsync(x => x.EditId == long.Parse(id));
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.DepositEditHistory.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.DepositEditEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) db.DepositEditHistory.Add(entity);
                    return true;
                }

            case "walletAdjustments":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var adjId = parts[1];

                    var existing = await db.WalletAdjustments
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == adjId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.WalletAdjustments.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.WalletAdjustmentEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = adjId;
                        db.WalletAdjustments.Add(entity);
                    }
                    return true;
                }

            case "userPermissions":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var uid = parts[0];
                    var permission = parts[1];

                    var existing = await db.UserPermissions
                        .FirstOrDefaultAsync(x => x.Uid == uid && x.Permission == permission);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.UserPermissions.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.UserPermissionEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.Uid = uid;
                        entity.Permission = permission;
                        db.UserPermissions.Add(entity);
                    }
                    return true;
                }

            case "auditLog":
                {
                    var entity = JsonSerializer.Deserialize<Data.Entities.AuditLogEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) db.AuditLog.Add(entity);
                    return true;
                }

            case "notifications":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var notifId = parts[1];

                    var existing = await db.Notifications
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == notifId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Notifications.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.NotificationEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = notifId;
                        db.Notifications.Add(entity);
                    }
                    return true;
                }

            case "polls":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var pollId = parts[1];

                    var existing = await db.Polls
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == pollId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Polls.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.PollEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = pollId;
                        db.Polls.Add(entity);
                    }
                    return true;
                }

            case "pollOptions":
                {
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
                        db.PollOptions.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.PollOptionEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.PollId = pollId;
                        entity.Id = optionId;
                        db.PollOptions.Add(entity);
                    }
                    return true;
                }

            case "pollVotes":
                {
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
                        db.PollVotes.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.PollVoteEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.PollId = pollId;
                        entity.ApartmentId = aptId;
                        db.PollVotes.Add(entity);
                    }
                    return true;
                }

            case "maintenanceRecords":
                {
                    var parts = id.Split('_');
                    if (parts.Length != 2) return false;
                    var buildingId = parts[0];
                    var recordId = parts[1];

                    var existing = await db.MaintenanceRecords
                        .FirstOrDefaultAsync(x => x.BuildingId == buildingId && x.Id == recordId);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.MaintenanceRecords.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.MaintenanceRecordEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null)
                    {
                        entity.BuildingId = buildingId;
                        entity.Id = recordId;
                        db.MaintenanceRecords.Add(entity);
                    }
                    return true;
                }

            case "dvrs":
                {
                    var existing = await db.Dvrs.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Dvrs.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.DvrEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; db.Dvrs.Add(entity); }
                    return true;
                }

            case "cameras":
                {
                    var existing = await db.Cameras.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.Cameras.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.CameraEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; db.Cameras.Add(entity); }
                    return true;
                }

            case "qrTokens":
                {
                    var existing = await db.QrTokens.FirstOrDefaultAsync(x => x.Uid == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.QrTokens.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.QrTokenEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Uid = id; db.QrTokens.Add(entity); }
                    return true;
                }

            case "qrUsages":
                {
                    var existing = await db.QrUsages.FirstOrDefaultAsync(x => x.Id == id);
                    if (existing != null)
                    {
                        if (!overwrite) return false;
                        db.QrUsages.Remove(existing);
                    }
                    var entity = JsonSerializer.Deserialize<Data.Entities.QrUsageEntity>(data.GetRawText(), RestoreJsonOptions);
                    if (entity != null) { entity.Id = id; db.QrUsages.Add(entity); }
                    return true;
                }

            default:
                return false;
        }
    }
    public async Task<RestorePreview> PreviewRestoreAsync(Stream zipStream, string fileName, string userId)
    {
        var result = new RestorePreview { FileName = fileName };

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");

            // 1. اقرأ الملف في الذاكرة
            using var ms = new MemoryStream();
            await zipStream.CopyToAsync(ms);
            var fileBytes = ms.ToArray();

            // 2. فك التشفير لو الملف مشفر
            var decryptedBytes = BackupFileProtector.Unprotect(fileBytes);

            // 3. اكتب الملف المفكوك
            await File.WriteAllBytesAsync(tempPath, decryptedBytes);
            result.TempFilePath = tempPath;
            result.FileSize = decryptedBytes.Length;

            // 4. اقرأ الـ ZIP
            using var zip = ZipFile.OpenRead(tempPath);
            foreach (var entry in zip.Entries)
            {
                if (!entry.Name.EndsWith(".json")) continue;
                var tableName = Path.GetFileNameWithoutExtension(entry.Name);
                if (!KnownTables.ContainsKey(tableName)) continue;

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                try
                {
                    var docs = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                    result.Collections.Add(new RestoreCollectionPreview
                    {
                        Collection = tableName,
                        LabelAr = KnownTables[tableName],
                        DocCount = docs?.Count ?? 0,
                        SizeBytes = entry.Length,
                        Selected = true
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{tableName}: {ex.Message}");
                }
            }

            result.Success = result.Collections.Count > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }

        return result;
    }
}