using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class BackupService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly IAuditLogger _audit;
    private readonly ILogger<BackupService> _log;

    private static Dictionary<string, string> KnownTables => new()
    {
        ["buildings"] = Loc.T("Buildings"),
        ["users"] = Loc.T("Users"),
        ["auditLog"] = Loc.T("Audit_Log"),
        ["qrTokens"] = Loc.T("QR_Tokens"),
        ["qrUsages"] = Loc.T("QR_Usage"),
        ["presence"] = Loc.T("Presence"),
        ["residents"] = Loc.T("Residents"),
        ["dvrs"] = Loc.T("Nav_DVRs"),
        ["cameras"] = Loc.T("Nav_Cameras")
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
                    "buildings" => await db.Buildings.CountAsync(),
                    "users" => await db.Users.CountAsync(),
                    "auditLog" => await db.AuditLog.CountAsync(),
                    "qrTokens" => await db.QrTokens.CountAsync(),
                    "qrUsages" => await db.QrUsages.CountAsync(),
                    "presence" => await db.Presence.CountAsync(),
                    "residents" => await db.Residents.CountAsync(),
                    "dvrs" => await db.Dvrs.CountAsync(),
                    "cameras" => await db.Cameras.CountAsync(),
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
            case "buildings":
                foreach (var b in await db.Buildings.AsNoTracking().ToListAsync())
                    result.Add(ToDict(b.Id, b));
                break;
            case "users":
                foreach (var u in await db.Users.AsNoTracking().ToListAsync())
                    result.Add(ToDict(u.Uid, u));
                break;
            case "auditLog":
                foreach (var a in await db.AuditLog.AsNoTracking().ToListAsync())
                    result.Add(ToDict(a.AuditId.ToString(), a));
                break;
            case "qrTokens":
                foreach (var q in await db.QrTokens.AsNoTracking().ToListAsync())
                    result.Add(ToDict(q.Uid, q));
                break;
            case "qrUsages":
                foreach (var q in await db.QrUsages.AsNoTracking().ToListAsync())
                    result.Add(ToDict(q.Id, q));
                break;
            case "presence":
                foreach (var p in await db.Presence.AsNoTracking().ToListAsync())
                    result.Add(ToDict(p.Uid, p));
                break;
            case "residents":
                foreach (var r in await db.Residents.AsNoTracking().ToListAsync())
                    result.Add(ToDict(r.Id, r));
                break;
            case "dvrs":
                foreach (var d in await db.Dvrs.AsNoTracking().ToListAsync())
                    result.Add(ToDict(d.Id, d));
                break;
            case "cameras":
                foreach (var c in await db.Cameras.AsNoTracking().ToListAsync())
                    result.Add(ToDict(c.Id, c));
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

    public async Task<RestorePreview> PreviewRestoreAsync(Stream zipStream, string fileName, string userId)
    {
        var result = new RestorePreview { FileName = fileName };

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
            using (var fs = File.Create(tempPath))
                await zipStream.CopyToAsync(fs);

            result.TempFilePath = tempPath;
            result.FileSize = new FileInfo(tempPath).Length;

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

        try
        {
            using var zip = ZipFile.OpenRead(tempFilePath);

            foreach (var tableName in selectedTables)
            {
                var entry = zip.GetEntry($"{tableName}.json");
                if (entry == null)
                {
                    result.Errors.Add(Loc.T("N_Not_Found_In_The_ZIP", tableName));
                    continue;
                }

                result.TotalCollections++;

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                try
                {
                    await using var db = await _sqlFactory.CreateDbContextAsync();
                    var docs = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);

                    if (docs == null) continue;

                    // ملاحظة: هذا الـ restore مبسط جداً
                    // في الواقع محتاجين نتعامل مع كل table حسب الـ entity
                    // ممكن نضيف logic هنا لاحقاً

                    result.Details.Add($"{tableName}: {docs.Count} docs (restore skipped — manual restore recommended)");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{tableName}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add(ex.Message);
        }
        finally
        {
            try { File.Delete(tempFilePath); } catch { }
        }

        sw.Stop();
        result.Success = result.Errors.Count == 0;
        result.Duration = sw.Elapsed;

        await _audit.LogAsync(
            action: "backup.restore",
            userId: userId,
            userRole: "superadmin",
            metadata: new
            {
                tables = selectedTables,
                restored = result.Restored,
                errors = result.Errors.Count
            },
            severity: "critical");

        return result;
    }
}