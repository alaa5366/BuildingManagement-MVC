using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

public class AuditLogger : IAuditLogger
{
    private readonly FirestoreDb _db;
    private const string Collection = "auditLogs";

    public AuditLogger(FirestoreContext ctx)
    {
        _db = ctx.Db;
    }

    public async Task LogAsync(
        string action,
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        string? userRole = null,
        object? metadata = null,
        string severity = "info")
    {
        try
        {
            var entry = new AuditEntryDoc
            {
                Action = action,
                BuildingId = buildingId,
                ApartmentId = apartmentId,
                UserId = userId,
                UserRole = userRole,
                Severity = severity,
                CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow),
                Metadata = metadata == null
                    ? null
                    : JsonConvert.DeserializeObject<Dictionary<string, object>>(
                        JsonConvert.SerializeObject(metadata))
            };

            await _db.Collection(Collection).AddAsync(entry);
        }
        catch
        {
            // فشل السجل مايوقفش العملية الأساسية
        }
    }

    public async Task<List<AuditEntryDoc>> QueryAsync(
        string? buildingId = null,
        string? apartmentId = null,
        string? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        string? action = null,
        string? severity = null,
        int limit = 100)
    {
        var list = new List<AuditEntryDoc>();

        try
        {
            var snapshot = await _db.Collection(Collection).GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                try
                {
                    var data = doc.ToDictionary();

                    var entry = new AuditEntryDoc
                    {
                        Id = doc.Id,
                        Action = GetString(data, "action"),
                        BuildingId = GetStringOrNull(data, "buildingId"),
                        ApartmentId = GetStringOrNull(data, "apartmentId"),
                        UserId = GetStringOrNull(data, "userId"),
                        UserRole = GetStringOrNull(data, "userRole"),
                        Severity = GetString(data, "severity", "info"),
                        Details = GetStringOrNull(data, "details"),
                        CreatedAt = GetTimestamp(data, "createdAt"),
                        Timestamp = GetTimestamp(data, "timestamp"),
                        Metadata = GetMetadata(data)
                    };

                    // الفلاتر
                    if (!string.IsNullOrWhiteSpace(buildingId) && entry.BuildingId != buildingId) continue;
                    if (!string.IsNullOrWhiteSpace(apartmentId) && entry.ApartmentId != apartmentId) continue;
                    if (!string.IsNullOrWhiteSpace(userId) && entry.UserId != userId) continue;
                    if (!string.IsNullOrWhiteSpace(action) && entry.Action != action) continue;
                    if (!string.IsNullOrWhiteSpace(severity) && entry.Severity != severity) continue;
                    if (from.HasValue && entry.EffectiveDate < from.Value) continue;
                    if (to.HasValue && entry.EffectiveDate > to.Value) continue;

                    list.Add(entry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AuditLogger.Query] Doc {doc.Id} failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLogger.Query] ERROR: {ex.Message}");
        }

        return list
            .OrderByDescending(e => e.EffectiveDate)
            .Take(limit)
            .ToList();
    }

    // ============================================================
    // Helpers
    // ============================================================
    private static string GetString(Dictionary<string, object> data, string key, string defaultVal = "")
    {
        if (data.TryGetValue(key, out var v) && v != null)
            return v.ToString() ?? defaultVal;
        return defaultVal;
    }

    private static string? GetStringOrNull(Dictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var v) && v != null)
            return v.ToString();
        return null;
    }

    private static Google.Cloud.Firestore.Timestamp? GetTimestamp(
        Dictionary<string, object> data, string key)
    {
        if (!data.TryGetValue(key, out var v) || v == null) return null;
        if (v is Google.Cloud.Firestore.Timestamp ts) return ts;
        if (v is string s && DateTime.TryParse(s, out var dt))
            return Google.Cloud.Firestore.Timestamp.FromDateTime(dt.ToUniversalTime());
        return null;
    }

    private static Dictionary<string, object>? GetMetadata(Dictionary<string, object> data)
    {
        if (!data.TryGetValue("metadata", out var v) || v == null) return null;
        if (v is Dictionary<string, object> dict) return dict;
        if (v is IReadOnlyDictionary<string, object> ro)
            return new Dictionary<string, object>(ro);
        return null;
    }
}