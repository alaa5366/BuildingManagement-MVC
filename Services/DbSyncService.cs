using BuildingManagementMvc.Models;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BuildingManagementMvc.Services;

// خدمة مزامنة Firebase Auth ↔ Firestore
public class DbSyncService
{
    private readonly UsersService _users;
    private readonly BuildingsService _buildings;
    private readonly FirebaseAuthRestService _fbAuth;
    private readonly FirebaseAdminService _fbAdmin;
    private readonly IAuditLogger _audit;

    public DbSyncService(
        UsersService users,
        BuildingsService buildings,
        FirebaseAuthRestService fbAuth,
        FirebaseAdminService fbAdmin,
        IAuditLogger audit)
    {
        _users = users;
        _buildings = buildings;
        _fbAuth = fbAuth;
        _fbAdmin = fbAdmin;
        _audit = audit;
    }

    // ============================================================
    // 1. مزامنة الأدمنة
    // ============================================================
    public async Task<SyncResult> SyncAdminsAsync(bool autoFix, string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult();

        var admins = await _users.GetAdminsAsync();
        result.TotalChecked = admins.Count;

        foreach (var admin in admins)
        {
            var buildingId = admin.BuildingIds.FirstOrDefault() ?? "";
            var email = AuthHelpers.AdminEmailForPhone(buildingId, admin.Phone);

            // 1. نتأكد إن الإيميل في Firestore مطابق للصيغة المتوقعة
            if (admin.Email != email)
            {
                result.TotalIssues++;
                var item = new SyncDiffItem
                {
                    Type = "admin",
                    Id = admin.Uid,
                    Name = admin.Name,
                    Issue = Loc.T("Email_Mismatch_N_N", admin.Email, email),
                    CanAutoFix = true
                };

                if (autoFix)
                {
                    await _users.SetAsync(admin.Uid, new Dictionary<string, object>
                    {
                        ["email"] = email
                    });
                    item.Fixed = true;
                    result.TotalFixed++;
                }

                result.Items.Add(item);
            }

            // 2. نتأكد إن حساب Firebase Auth موجود
            var uid = await _fbAdmin.CreateOrGetUserAsync(email,
                AuthHelpers.AdminPasswordForPhone(admin.Phone, admin.Pin));

            if (string.IsNullOrEmpty(uid))
            {
                result.TotalIssues++;
                result.Items.Add(new SyncDiffItem
                {
                    Type = "admin",
                    Id = admin.Uid,
                    Name = admin.Name,
                    Issue = Loc.T("Firebase_Auth_Account_Does_Not_Exist"),
                    CanAutoFix = false
                });
            }
            else if (uid != admin.Uid)
            {
                result.TotalIssues++;
                result.Items.Add(new SyncDiffItem
                {
                    Type = "admin",
                    Id = admin.Uid,
                    Name = admin.Name,
                    Issue = Loc.T("UID_Mismatch_Firestore_N_Auth_N", admin.Uid, uid),
                    CanAutoFix = false
                });
            }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

        await _audit.LogAsync(
            action: "sync.admins",
            userId: userId,
            userRole: "superadmin",
            metadata: new { total = result.TotalChecked, issues = result.TotalIssues, fixedCount = result.TotalFixed, autoFix },
            severity: "warning");

        return result;
    }

    // ============================================================
    // 2. مزامنة السكان
    // ============================================================
    public async Task<SyncResult> SyncResidentsAsync(bool autoFix, string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult();

        var buildings = await _buildings.GetAllAsync();
        var total = buildings.Sum(b => b.Apartments.Count);
        result.TotalChecked = total;

        foreach (var building in buildings)
        {
            foreach (var apt in building.Apartments)
            {
                var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
                if (floor == null) continue;

                var email = AuthHelpers.ResidentInternalEmail(building.Id, floor.Order, apt.Number);
                var password = AuthHelpers.ResidentPassword(building.Id, apt.Number, apt.Pin);

                var uid = await _fbAdmin.CreateOrGetUserAsync(email, password);

                if (string.IsNullOrEmpty(uid))
                {
                    result.TotalIssues++;
                    result.Items.Add(new SyncDiffItem
                    {
                        Type = "resident",
                        Id = apt.Id,
                        Name = Loc.T("Apartment_N_N_4", apt.Number, building.Name),
                        Issue = Loc.T("Firebase_Auth_Account_Does_Not_Exist"),
                        CanAutoFix = false
                    });
                }
            }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

        await _audit.LogAsync(
            action: "sync.residents",
            userId: userId,
            userRole: "superadmin",
            metadata: new { total = result.TotalChecked, issues = result.TotalIssues, autoFix },
            severity: "warning");

        return result;
    }

    // ============================================================
    // 3. مزامنة كلمات السر
    // ============================================================
    public async Task<SyncResult> SyncPasswordsAsync(string userId)
    {
        var sw = Stopwatch.StartNew();
        var result = new SyncResult();

        var buildings = await _buildings.GetAllAsync();
        result.TotalChecked = buildings.Sum(b => b.Apartments.Count + 1); // +1 for admin

        foreach (var building in buildings)
        {
            // الأدمن
            var adminEmail = AuthHelpers.AdminEmailForPhone(building.Id, building.AdminWhatsapp);
            var adminPassword = AuthHelpers.AdminPasswordForPhone(building.AdminWhatsapp, building.AdminPin);

            var ok = await _fbAdmin.UpdatePasswordAsync(adminEmail, adminPassword);
            if (!ok)
            {
                result.TotalIssues++;
                result.Items.Add(new SyncDiffItem
                {
                    Type = "admin",
                    Id = building.Id,
                    Name = Loc.T("Admin_N_2", building.Name),
                    Issue = Loc.T("Failed_To_Update_The_Password"),
                    CanAutoFix = false
                });
            }
            else
            {
                result.TotalFixed++;
            }

            // السكان
            foreach (var apt in building.Apartments)
            {
                var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);
                if (floor == null) continue;

                var email = AuthHelpers.ResidentInternalEmail(building.Id, floor.Order, apt.Number);
                var password = AuthHelpers.ResidentPassword(building.Id, apt.Number, apt.Pin);

                var aptOk = await _fbAdmin.UpdatePasswordAsync(email, password);
                if (!aptOk)
                {
                    result.TotalIssues++;
                    result.Items.Add(new SyncDiffItem
                    {
                        Type = "resident",
                        Id = apt.Id,
                        Name = Loc.T("Apartment_N_N_4", apt.Number, building.Name),
                        Issue = Loc.T("Failed_To_Update_The_Password"),
                        CanAutoFix = false
                    });
                }
                else
                {
                    result.TotalFixed++;
                }
            }
        }

        sw.Stop();
        result.Duration = sw.Elapsed;

        await _audit.LogAsync(
            action: "sync.passwords",
            userId: userId,
            userRole: "superadmin",
            metadata: new { total = result.TotalChecked, fixedCount = result.TotalFixed },
            severity: "critical");

        return result;
    }
}