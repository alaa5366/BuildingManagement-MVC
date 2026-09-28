using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

public class BackupService
{
    private readonly FirestoreDb _db;
    private readonly IAuditLogger _audit;

    private static readonly Dictionary<string, string> KnownCollections = new()
    {
        ["buildings"] = "العمارات",
        ["users"] = "المستخدمون",
        ["auditLogs"] = "سجل التدقيق",
        ["qr_usage"] = "استخدام QR",
        ["qr-tokens"] = "رموز QR",
        ["presence"] = "الحضور",
        ["whatsappTemplates"] = "قوالب واتساب"
    };

    public BackupService(FirestoreContext ctx, IAuditLogger audit)
    {
        _db = ctx.Db;
        _audit = audit;
    }

    public static IReadOnlyDictionary<string, string> GetKnownCollections() => KnownCollections;

    // ============================================================
    // 1. إحصائيات
    // ============================================================
    public async Task<List<BackupStats>> GetStatsAsync()
    {
        var result = new List<BackupStats>();

        foreach (var kv in KnownCollections)
        {
            var count = 0;
            try
            {
                var snapshot = await _db.Collection(kv.Key).Limit(5000).GetSnapshotAsync();
                count = snapshot.Documents.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Backup] Stats error for {kv.Key}: {ex.Message}");
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

    // ============================================================
    // 2. إنشاء نسخة احتياطية
    // ============================================================
    public async Task<(byte[] Bytes, BackupResult Result)> CreateBackupAsync(
        List<string> selectedCollections,
        string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new BackupResult();

        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var colName in selectedCollections)
            {
                if (!KnownCollections.ContainsKey(colName))
                    continue;

                try
                {
                    var docs = await ReadAllDocsAsync(colName);
                    var json = JsonSerializer.Serialize(docs, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    AddFileToZip(zip, $"{colName}.json", json);

                    result.Collections.Add(colName);
                    result.TotalDocuments += docs.Count;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{colName}: {ex.Message}");
                }
            }

            var readme = $@"# Building Management — Backup
تاريخ الإنشاء: {DateTime.UtcNow:O}
بواسطة: {userId}

## المحتوى:
{string.Join("\n", result.Collections.Select(c => $"- {c}.json ({KnownCollections[c]})"))}

## ملاحظات:
- كل ملف JSON فيه مصفوفة من المستندات.
- كل مستند = {{ id, data }}.
- للاستعادة: استخدم /BackupFull/Restore.
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
                fileSize = result.FileSize,
                duration = result.Duration.TotalSeconds
            },
            severity: "warning");

        return (ms.ToArray(), result);
    }

    // ============================================================
    // 3. قراءة الـ Docs لـ JSON (بـ pagination)
    // ============================================================
    private async Task<List<Dictionary<string, object>>> ReadAllDocsAsync(string collection)
    {
        var result = new List<Dictionary<string, object>>();

        const int pageSize = 500;
        DocumentSnapshot? lastDoc = null;
        var hasMore = true;

        while (hasMore)
        {
            Query query = _db.Collection(collection).OrderBy(FieldPath.DocumentId).Limit(pageSize);
            if (lastDoc != null)
                query = query.StartAfter(lastDoc);

            var snapshot = await query.GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                var dict = new Dictionary<string, object>
                {
                    ["id"] = doc.Id,
                    ["data"] = ConvertToSerializable(doc.ToDictionary())
                };
                result.Add(dict);
            }

            hasMore = snapshot.Documents.Count == pageSize;
            if (hasMore)
                lastDoc = snapshot.Documents.Last();
        }

        return result;
    }

    // ============================================================
    // 4. تحويل Firebase types لـ JSON-compatible
    // ============================================================
    private static Dictionary<string, object?> ConvertToSerializable(Dictionary<string, object> input)
    {
        var result = new Dictionary<string, object?>();
        foreach (var kv in input)
        {
            result[kv.Key] = ConvertValue(kv.Value);
        }
        return result;
    }

    private static object? ConvertValue(object? value)
    {
        if (value == null) return null;

        if (value is Google.Cloud.Firestore.Timestamp ts)
            return new Dictionary<string, object>
            {
                ["__type"] = "timestamp",
                ["value"] = ts.ToDateTime().ToString("o")
            };

        if (value is IDictionary<string, object> dict)
            return ConvertToSerializable(new Dictionary<string, object>(dict));

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
                list.Add(ConvertValue(item));
            return list;
        }

        return value;
    }

    private static void AddFileToZip(ZipArchive zip, string fileName, string content)
    {
        var entry = zip.CreateEntry(fileName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    // ============================================================
    // 5. معاينة الاستعادة
    // ============================================================
    public async Task<RestorePreview> PreviewRestoreAsync(Stream zipStream, string fileName, string userId)
    {
        var result = new RestorePreview { FileName = fileName };

        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"restore-{Guid.NewGuid():N}.zip");
            using (var fs = File.Create(tempPath))
            {
                await zipStream.CopyToAsync(fs);
            }

            result.TempFilePath = tempPath;
            result.FileSize = new FileInfo(tempPath).Length;

            using var zip = ZipFile.OpenRead(tempPath);
            foreach (var entry in zip.Entries)
            {
                if (!entry.Name.EndsWith(".json")) continue;

                var colName = Path.GetFileNameWithoutExtension(entry.Name);
                if (!KnownCollections.ContainsKey(colName)) continue;

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                try
                {
                    var docs = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                    result.Collections.Add(new RestoreCollectionPreview
                    {
                        Collection = colName,
                        LabelAr = KnownCollections[colName],
                        DocCount = docs?.Count ?? 0,
                        SizeBytes = entry.Length,
                        Selected = true
                    });
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{colName}: JSON غير صالح - {ex.Message}");
                }
            }

            result.Success = result.Collections.Count > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"خطأ: {ex.Message}");
        }

        return result;
    }

    // ============================================================
    // 6. تنفيذ الاستعادة
    // ============================================================
    public async Task<RestoreResult> RestoreAsync(
        string tempFilePath,
        List<string> selectedCollections,
        bool overwrite,
        string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new RestoreResult();

        if (!File.Exists(tempFilePath))
        {
            result.Errors.Add("الملف المؤقت غير موجود");
            return result;
        }

        try
        {
            using var zip = ZipFile.OpenRead(tempFilePath);

            foreach (var colName in selectedCollections)
            {
                var entry = zip.GetEntry($"{colName}.json");
                if (entry == null)
                {
                    result.Errors.Add($"{colName}: غير موجود في الـ ZIP");
                    continue;
                }

                result.TotalCollections++;

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                List<Dictionary<string, JsonElement>>? docs;
                try
                {
                    docs = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{colName}: JSON غير صالح - {ex.Message}");
                    continue;
                }

                if (docs == null) continue;

                foreach (var doc in docs)
                {
                    if (!doc.ContainsKey("id") || !doc.ContainsKey("data"))
                    {
                        result.Skipped++;
                        continue;
                    }

                    try
                    {
                        var id = doc["id"].GetString();
                        if (string.IsNullOrEmpty(id))
                        {
                            result.Skipped++;
                            continue;
                        }

                        var data = JsonElementToDict(doc["data"]);

                        var docRef = _db.Collection(colName).Document(id);
                        var exists = (await docRef.GetSnapshotAsync()).Exists;

                        if (exists && !overwrite)
                        {
                            result.Skipped++;
                            continue;
                        }

                        if (exists && overwrite)
                            await docRef.SetAsync(data, SetOptions.Overwrite);
                        else
                            await docRef.SetAsync(data);

                        result.Restored++;
                        result.TotalDocuments++;
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.Errors.Add($"{colName}/{doc.GetValueOrDefault("id")}: {ex.Message}");
                    }
                }

                result.Details.Add($"✅ {colName}: {docs.Count} مستند");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"خطأ: {ex.Message}");
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
                collections = selectedCollections,
                restored = result.Restored,
                skipped = result.Skipped,
                failed = result.Failed,
                overwrite,
                duration = result.Duration.TotalSeconds
            },
            severity: "critical");

        return result;
    }

    // ============================================================
    // 7. Helper: JsonElement → Dictionary
    // ============================================================
    private static Dictionary<string, object?> JsonElementToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        if (element.ValueKind != JsonValueKind.Object) return dict;

        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = JsonElementToObject(prop.Value);
        }
        return dict;
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return element.GetString();
            case JsonValueKind.Number:
                if (element.TryGetInt64(out var l)) return l;
                return element.GetDouble();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
            case JsonValueKind.Object:
                if (element.TryGetProperty("__type", out var typeEl) &&
                    typeEl.GetString() == "timestamp" &&
                    element.TryGetProperty("value", out var valEl))
                {
                    if (DateTime.TryParse(valEl.GetString(), out var dt))
                    {
                        if (dt.Kind != DateTimeKind.Utc)
                            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        return Google.Cloud.Firestore.Timestamp.FromDateTime(dt);
                    }
                }
                return JsonElementToDict(element);
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                    list.Add(JsonElementToObject(item));
                return list;
            default:
                return element.ToString();
        }
    }
}