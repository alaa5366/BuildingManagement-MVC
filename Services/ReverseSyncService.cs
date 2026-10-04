// =====================================================================
//  ReverseSyncService - مزامنة SQL → Firebase بعد رجوع Firebase
//  بتستخدم جدول SyncPendingChanges لتحديد التعديلات
// =====================================================================
using BuildingManagementMvc.Data;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

public class ReverseSyncService
{
    private readonly IDbContextFactory<AppDbContext> _sqlFactory;
    private readonly FirestoreDb _firebase;
    private readonly ILogger<ReverseSyncService> _log;

    public ReverseSyncService(
        IDbContextFactory<AppDbContext> sqlFactory,
        FirestoreContext firestoreCtx,
        ILogger<ReverseSyncService> log)
    {
        _sqlFactory = sqlFactory;
        _firebase = firestoreCtx.Db;
        _log = log;
    }

    public async Task<ReverseSyncReport> SyncSqlToFirebaseAsync()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var report = new ReverseSyncReport();

        await using var db = await _sqlFactory.CreateDbContextAsync();

        var pending = await db.SyncPendingChanges
            .Where(x => !x.Applied && x.RetryCount < 5)
            .OrderBy(x => x.CreatedAt)
            .Take(500)
            .ToListAsync();

        _log.LogInformation("Found {Count} pending changes", pending.Count);

        foreach (var change in pending)
        {
            try
            {
                switch (change.EntityType)
                {
                    case "Building":
                        await ApplyBuildingChangeAsync(change);
                        break;
                    case "User":
                        await ApplyUserChangeAsync(change);
                        break;
                    default:
                        _log.LogWarning("Unknown entity type: {Type}", change.EntityType);
                        break;
                }

                change.Applied = true;
                change.AppliedAt = DateTime.UtcNow;
                report.Updated++;
            }
            catch (Exception ex)
            {
                change.RetryCount++;
                change.ErrorMessage = ex.Message;
                report.Errors.Add($"{change.EntityType}#{change.EntityId}: {ex.Message}");
                _log.LogError(ex, "Failed change {Id}", change.Id);
            }
        }

        await db.SaveChangesAsync();
        sw.Stop();
        report.Elapsed = sw.Elapsed;
        return report;
    }

    private async Task ApplyBuildingChangeAsync(SyncPendingChangeEntity change)
    {
        if (change.Operation == "Delete")
        {
            await _firebase.Collection("buildings").Document(change.EntityId).DeleteAsync();
            return;
        }

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var building = await BuildingSqlMapper.LoadAsync(db, change.EntityId);
        if (building == null)
        {
            _log.LogWarning("Building {Id} not found", change.EntityId);
            return;
        }

        await _firebase.Collection("buildings").Document(change.EntityId)
            .SetAsync(building, SetOptions.Overwrite);
    }

    private async Task ApplyUserChangeAsync(SyncPendingChangeEntity change)
    {
        if (change.Operation == "Delete")
        {
            await _firebase.Collection("users").Document(change.EntityId).DeleteAsync();
            return;
        }

        await using var db = await _sqlFactory.CreateDbContextAsync();
        var user = await db.Users
            .Include(u => u.Permissions)
            .Include(u => u.BuildingAdmins)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Uid == change.EntityId);

        if (user == null)
        {
            _log.LogWarning("User {Id} not found", change.EntityId);
            return;
        }

        var appUser = UserSqlMapper.ToDoc(user);
        await _firebase.Collection("users").Document(change.EntityId)
            .SetAsync(appUser, SetOptions.Overwrite);
    }
}
