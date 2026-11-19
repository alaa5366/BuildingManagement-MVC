// =====================================================================
//  BuildingSqlMapper
//  بيحوّل بين الـ aggregate القديم (Models.Building بكل اللي جواه) وجداول SQL.
//
//  ليه ده الحل؟ التطبيق كله شغال بنمط: حمّل Building كامل -> عدّل في الذاكرة
//  -> SaveFullAsync(building). فمينفعش نحوّل Service واحدة لوحدها لـ EF.
//  بدل كده بنبدّل التخزين تحت BuildingsService، والـ 37 ملف اللي بيستخدموه
//  (Controllers + Services) مايتغيروش.
//
//  السلوك:
//   - SaveAsync: Upsert بالمقارنة (مش delete-all/insert-all)، جوه Transaction.
//     أي صف مش موجود في الـ aggregate بيتشال من القاعدة (زي Overwrite في Firestore)،
//     ما عدا AuditLog: Append-only (بيضيف الجديد بس).
//   - لو فيه بيانات مش هتعدّي قيود القاعدة (شقة مش موجودة، مبلغ <= 0، حالة غريبة...)
//     الصف بيتخطّى ويتضاف تحذير في القايمة الراجعة. راجع التحذيرات.
//   - اللي ماتنقلش: month.collections و walletTransactions (قديمة) وشهر فاضي تماماً.
//   - تواريخ OpenDate/CloseDate/Expense.Date بقت DATE (بتتحفظ بصيغة yyyy-MM-dd).
// =====================================================================
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

/// <summary>
/// Maps between the in-memory <see cref="BuildingManagementMvc.Models.Building"/> aggregate and the EF Core entities, in both directions.
/// </summary>
public static class BuildingSqlMapper
{
    private const int AuditCap = 1000;   // نفس سقف AuditLogService.Push
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly Regex MonthKeyRx =
        new(@"^(?!0000)\d{4}-(0[1-9]|1[0-2])$", RegexOptions.Compiled);

    // ------------------------------------------------------------------
    //  Helpers
    // ------------------------------------------------------------------
    private static DateTime Trunc(DateTime d) =>
        new(d.Ticks - d.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

    private static DateTime NowSec() => Trunc(DateTime.UtcNow);

    internal static DateTime? ParseTs(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!DateTime.TryParse(s, Inv,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d))
            return null;
        return Trunc(d);
    }

    private static string? FormatTs(DateTime? d) =>
        d.HasValue
            ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc)
                .ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", Inv)
            : null;

    private static DateOnly? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!DateTime.TryParse(s, Inv,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d))
            return null;
        return DateOnly.FromDateTime(d);
    }

    private static string? FormatDate(DateOnly? d) => d?.ToString("yyyy-MM-dd", Inv);

    private static DateOnly FirstOfMonth(string monthKey) =>
        new(int.Parse(monthKey[..4], Inv), int.Parse(monthKey[5..7], Inv), 1);

    private static decimal Dec(decimal d) =>
        Math.Round(d, 2, MidpointRounding.AwayFromZero);

    internal static string Cut(string? s, int max)
    {
        s ??= "";
        return s.Length <= max ? s : s[..max];
    }

    private static string AuditKey(AuditLogEntry e)
    {
        if (!string.IsNullOrWhiteSpace(e.Id)) return Cut(e.Id, 64);
        var raw = $"{e.Ts}|{e.Action}|{e.Actor}|{e.Label}|{e.Details}";
        return "h" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..32];
    }

    /// Upsert بالمقارنة: الجديد بيتضاف، الموجود بيتحدّث، واللي مش في incoming بيتشال.
    private static void Sync<T, TKey>(
        AppDbContext db, List<T> existing, IEnumerable<T> incoming,
        Func<T, TKey> key, List<string> warn, string what)
        where T : class where TKey : notnull
    {
        var map = existing.ToDictionary(key);
        var seen = new HashSet<TKey>();
        foreach (var inc in incoming)
        {
            var k = key(inc);
            if (!seen.Add(k))
            {
                warn.Add($"{what}: مفتاح مكرر {k} - اتخطّى التكرار");
                continue;
            }
            if (map.TryGetValue(k, out var cur)) db.Entry(cur).CurrentValues.SetValues(inc);
            else db.Add(inc);
        }
        foreach (var (k, cur) in map)
            if (!seen.Contains(k)) db.Remove(cur);
    }

    // ==================================================================
    //  LOAD  : SQL -> Models.Building
    // ==================================================================
    /// <summary>
    /// Loads the operation on the building SQL.
    /// </summary>
    public static async Task<Building?> LoadAsync(
        AppDbContext db, string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        // The retired aggregate tables (Expenses, Revenues, Deposits, Notifications,
        // Polls and MaintenanceRecords) are intentionally not queried here. The
        // deployed database now uses the target ledger and operational schema.
        var be = await db.Buildings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (be == null) return null;

        var floors = await db.Floors.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var apts = await db.Apartments.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var cats = await db.FinancialCategories.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var admins = await db.BuildingAdmins.AsNoTracking().Where(x => x.BuildingId == id)
            .Select(x => x.AdminUid).ToListAsync(ct);
        var audit = await db.AuditEvents.AsNoTracking()
            .Where(x => x.BuildingId == id && x.EventCategory == "BuildingAggregate" &&
                        x.ResourceType == "BuildingAuditEntry" && x.ResourceReference != null)
            .OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.AuditEventId)
            .Take(AuditCap).ToListAsync(ct);
        audit.Reverse();

        var periods = await db.FinancialPeriods.AsNoTracking()
            .Where(x => x.BuildingId == id)
            .Select(x => new { x.PeriodId, x.PeriodKey })
            .ToDictionaryAsync(x => x.PeriodId, x => x.PeriodKey, ct);
        var postedTransactions = await db.LedgerTransactions.AsNoTracking()
            .Where(x => x.BuildingId == id && x.Status == LedgerTransactionStatus.Posted)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);
        var transactionIds = postedTransactions.Select(x => x.LedgerTransactionId).ToList();
        var ledgerEntries = transactionIds.Count == 0
            ? new List<LedgerEntryEntity>()
            : await db.LedgerEntries.AsNoTracking()
                .Where(x => x.BuildingId == id && transactionIds.Contains(x.LedgerTransactionId))
                .ToListAsync(ct);
        var entriesByTransaction = ledgerEntries.GroupBy(x => x.LedgerTransactionId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var payments = await db.PaymentDeposits.AsNoTracking()
            .Where(x => x.BuildingId == id)
            .OrderBy(x => x.SubmittedAtUtc)
            .ToListAsync(ct);

        var b = new Building
        {
            Id = be.Id,
            BuildingNumber = be.BuildingNumber,
            Name = be.Name,
            AdminPin = "",
            DataVersion = be.DataVersion,
            LogoUrl = be.LogoUrl,
            AdminWhatsapp = be.AdminWhatsapp,
            RowVersion = be.RowVersion.ToArray(),
            CreatedAt = FormatTs(be.CreatedAt) ?? "",
            PaymentInfo = new PaymentInfo
            {
                Label = be.PaymentLabel,
                AccountNumber = be.PaymentAccountNumber,
                Phone = be.PaymentPhone,
                Notes = be.PaymentNotes
            },
            AdminUids = admins,
            Floors = floors.OrderBy(x => x.SortOrder)
                .Select(x => new Floor { Id = x.Id, Label = x.Label, Order = x.SortOrder }).ToList(),
            Apartments = apts.Select(a => new Apartment
            {
                Id = a.Id,
                FloorId = a.FloorId,
                Number = a.Number,
                Owner = a.Owner,
                Phone = a.Phone,
                MonthlyFee = a.MonthlyFee,
                Pin = "",
                Closed = a.IsClosed,
                Label = a.Label,
                Email = a.Email,
                Notes = a.Notes,
                OpenDate = FormatDate(a.OpenDate) ?? "",
                CloseDate = FormatDate(a.CloseDate),
                Disabled = a.IsDisabled,
                DisabledReason = a.DisabledReason,
                Settings = ToSettings(a)
            }).ToList(),
            ExpenseCategories = cats.Where(x => x.Kind == "expense").OrderBy(x => x.SortOrder)
                .Select(ToCategory).ToList(),
            RevenueCategories = cats.Where(x => x.Kind == "revenue").OrderBy(x => x.SortOrder)
                .Select(ToCategory).ToList(),
            AuditLog = audit.Select(MapAggregateAuditEvent).ToList()
        };

        // The target ledger is the financial source of truth. Build the legacy
        // presentation DTOs from posted ledger transactions without touching
        // retired detail tables. Ledger entries are per-apartment signed movements.
        foreach (var transaction in postedTransactions)
        {
            if (!periods.TryGetValue(transaction.PeriodId, out var monthKey) ||
                !MonthKeyRx.IsMatch(monthKey))
                continue;

            var amount = entriesByTransaction.TryGetValue(transaction.LedgerTransactionId, out var entries)
                ? entries.Sum(x => Math.Abs(x.SignedAmount))
                : 0m;
            if (amount <= 0) continue;

            var month = b.Months.TryGetValue(monthKey, out var existingMonth)
                ? existingMonth
                : (b.Months[monthKey] = new MonthData());

            var transactionId = transaction.LedgerTransactionId.ToString("N");
            var effectiveDate = entries is { Count: > 0 }
                ? entries.Min(x => x.EffectiveAtUtc).ToString("yyyy-MM-dd", Inv)
                : transaction.CreatedAtUtc.ToString("yyyy-MM-dd", Inv);

            if (transaction.Kind == LedgerTransactionKind.Expense)
            {
                month.Expenses.Add(new Expense
                {
                    Id = transactionId,
                    CategoryId = "",
                    Note = transaction.Reason ?? "Expense",
                    Amount = amount,
                    Date = effectiveDate
                });
            }
            else if (transaction.Kind == LedgerTransactionKind.Revenue)
            {
                month.Revenues.Add(new Revenue
                {
                    Id = transactionId,
                    CategoryId = "",
                    Note = transaction.Reason ?? "Revenue",
                    Amount = amount,
                    Date = effectiveDate
                });
            }
            else if (transaction.Kind == LedgerTransactionKind.WalletAdjustment)
            {
                foreach (var entry in entries ?? new List<LedgerEntryEntity>())
                {
                    b.WalletAdjustments.Add(new WalletAdjustment
                    {
                        Id = transactionId + "-" + entry.ApartmentId,
                        AptId = entry.ApartmentId,
                        Amount = entry.SignedAmount,
                        Reason = transaction.Reason ?? "Wallet adjustment",
                        MonthKey = monthKey,
                        CreatedAt = FormatTs(transaction.CreatedAtUtc) ?? "",
                        CreatedBy = transaction.ActorReference ?? ""
                    });
                }
            }
        }

        foreach (var payment in payments)
        {
            if (!periods.TryGetValue(payment.PeriodId, out var monthKey) ||
                !MonthKeyRx.IsMatch(monthKey))
                continue;
            var month = b.Months.TryGetValue(monthKey, out var existingMonth)
                ? existingMonth
                : (b.Months[monthKey] = new MonthData());
            month.Deposits.Add(new Deposit
            {
                Id = payment.PaymentDepositId.ToString("N"),
                AptId = payment.ApartmentId,
                Amount = payment.Amount,
                Note = payment.Note ?? "",
                Status = payment.Status switch
                {
                    PaymentDepositStatus.Pending => "pending",
                    PaymentDepositStatus.Confirmed => "confirmed",
                    PaymentDepositStatus.Cancelled => "cancelled",
                    PaymentDepositStatus.Reversed => "reversed",
                    _ => "pending"
                },
                CreatedAt = FormatTs(payment.SubmittedAtUtc) ?? "",
                CreatedBy = payment.SourceSystem
            });
        }

        // Notifications, maintenance and voting are now independent target
        // aggregates and are loaded by their dedicated services/controllers.
        return b;
    }

    private static bool HasReceipt(DepositEntity d) =>
        d.ReceiptUrl != null || d.ReceiptPath != null || d.ReceiptFileName != null ||
        d.ReceiptFileSize != null || d.ReceiptFileType != null || d.ReceiptSuccess != null;

    private static FinancialCategory ToCategory(FinancialCategoryEntity c) => new()
    {
        Id = c.Id, Name = c.Name, Color = c.Color, Active = c.IsActive, Order = c.SortOrder
    };

    private static UserSettingsDoc? ToSettings(ApartmentEntity a)
    {
        var isDefault = a.SettingsUpdatedAt == null && a.SettingsUpdatedBy == null
            && a.Language == "ar" && a.NotifyWhatsApp && !a.NotifyEmail && a.NotifyInApp
            && a.PreferredPaymentMethod == "instapay";
        if (isDefault) return null;
        return new UserSettingsDoc
        {
            Language = a.Language,
            NotificationPreferences = new NotificationPreferences
            {
                WhatsApp = a.NotifyWhatsApp, Email = a.NotifyEmail, InApp = a.NotifyInApp
            },
            PreferredPaymentMethod = a.PreferredPaymentMethod,
            UpdatedAt = FormatTs(a.SettingsUpdatedAt),
            UpdatedBy = a.SettingsUpdatedBy
        };
    }

    // ==================================================================
    //  SAVE  : Models.Building -> SQL      (بيرجّع قايمة تحذيرات)
    // ==================================================================
    /// <summary>
    /// Saves the operation on the building SQL.
    /// </summary>
    public static async Task<List<string>> SaveAsync(
        AppDbContext db, Building src, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(src.Id))
            throw new ArgumentException("Building.Id مطلوب", nameof(src));

        var warn = new List<string>();
        var bid = src.Id;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // ---------------- 1) العمارة ----------------
        var be = await db.Buildings.FirstOrDefaultAsync(x => x.Id == bid, ct);
        if (be == null)
        {
            be = new BuildingEntity { Id = bid, CreatedAt = ParseTs(src.CreatedAt) ?? NowSec() };
            db.Buildings.Add(be);
        }
        else
        {
            ValidateAggregateConcurrency(src.RowVersion, be.RowVersion);

            // A child-only aggregate change would otherwise leave the Buildings row untouched,
            // so SQL Server would not advance RowVersion and another app instance could pass
            // the same token check. Force a no-op parent update; the rowversion predicate then
            // protects every aggregate save, including changes limited to child tables.
            MarkAggregateWrite(db, be);
        }
        be.BuildingNumber = Cut(src.BuildingNumber, 50);
        be.Name = Cut(src.Name, 200);
        be.AdminPin = ""; // لم يعد يُخزَّن
        be.DataVersion = Cut(src.DataVersion, 20);
        be.LogoUrl = Cut(src.LogoUrl, 1000);
        be.AdminWhatsapp = Cut(src.AdminWhatsapp, 32);
        var pay = src.PaymentInfo ?? new PaymentInfo();
        be.PaymentLabel = Cut(pay.Label, 200);
        be.PaymentAccountNumber = Cut(pay.AccountNumber, 100);
        be.PaymentPhone = Cut(pay.Phone, 32);
        be.PaymentNotes = Cut(pay.Notes, 500);

        // ---------------- 2) الأدمنز (لازم يكونوا موجودين في Users) ----------------
        var uids = (src.AdminUids ?? new()).Where(u => !string.IsNullOrWhiteSpace(u)).Distinct().ToList();
        var knownUids = await db.Users.Where(u => uids.Contains(u.Uid)).Select(u => u.Uid).ToListAsync(ct);
        foreach (var u in uids.Except(knownUids))
            warn.Add($"admin uid '{u}' مش موجود في جدول Users - اتخطّى");
        var adminsExisting = await db.BuildingAdmins.Where(x => x.BuildingId == bid).ToListAsync(ct);
        Sync(db, adminsExisting,
            knownUids.Select(u => new BuildingAdminEntity { BuildingId = bid, AdminUid = u }),
            x => x.AdminUid, warn, "admins");

        // ---------------- 3) الأدوار ----------------
        var floors = (src.Floors ?? new())
            .Where(f => !string.IsNullOrWhiteSpace(f.Id))
            .Select(f => new FloorEntity
            {
                BuildingId = bid, Id = Cut(f.Id, 64), Label = Cut(f.Label, 100), SortOrder = f.Order
            }).ToList();
        var floorIds = floors.Select(f => f.Id).ToHashSet();
        var floorsExisting = await db.Floors.Where(x => x.BuildingId == bid).ToListAsync(ct);
        Sync(db, floorsExisting, floors, x => x.Id, warn, "floors");

        // ---------------- 4) الشقق ----------------
        var apartments = new List<ApartmentEntity>();
        var numbers = new HashSet<int>();
        foreach (var a in src.Apartments ?? new())
        {
            if (string.IsNullOrWhiteSpace(a.Id)) { warn.Add("شقة بدون Id - اتخطّت"); continue; }
            if (!floorIds.Contains(a.FloorId))
            { warn.Add($"شقة {a.Number} ({a.Id}): الدور '{a.FloorId}' مش موجود - اتخطّت"); continue; }
            if (!numbers.Add(a.Number))
            { warn.Add($"رقم الشقة {a.Number} مكرر - اتخطّت النسخة التانية ({a.Id})"); continue; }

            var fee = Dec(a.MonthlyFee);
            if (fee < 0) { warn.Add($"شقة {a.Number}: رسوم سالبة اتحوّلت لصفر"); fee = 0; }
            var s = a.Settings;
            apartments.Add(new ApartmentEntity
            {
                BuildingId = bid,
                Id = Cut(a.Id, 64),
                FloorId = a.FloorId,
                Number = a.Number,
                Owner = Cut(a.Owner, 200),
                Phone = Cut(a.Phone, 32),
                Email = Cut(a.Email, 256),
                MonthlyFee = fee,
                Pin = "",
                IsClosed = a.Closed,
                Label = Cut(a.Label, 100),
                Notes = Cut(a.Notes, 1000),
                OpenDate = ParseDate(a.OpenDate),
                CloseDate = ParseDate(a.CloseDate),
                IsDisabled = a.Disabled,
                DisabledReason = Cut(a.DisabledReason, 500),
                Language = Cut(s?.Language ?? "ar", 10),
                NotifyWhatsApp = s?.NotificationPreferences?.WhatsApp ?? true,
                NotifyEmail = s?.NotificationPreferences?.Email ?? false,
                NotifyInApp = s?.NotificationPreferences?.InApp ?? true,
                PreferredPaymentMethod = Cut(s?.PreferredPaymentMethod ?? "instapay", 30),
                SettingsUpdatedAt = ParseTs(s?.UpdatedAt),
                SettingsUpdatedBy = s?.UpdatedBy
            });
        }
        var aptIds = apartments.Select(a => a.Id).ToHashSet();
        var aptsExisting = await db.Apartments.Where(x => x.BuildingId == bid).ToListAsync(ct);
        Sync(db, aptsExisting, apartments, x => x.Id, warn, "apartments");

        // ---------------- 5) الفئات المالية ----------------
        var cats = new List<FinancialCategoryEntity>();
        void AddCats(string kind, List<FinancialCategory>? list)
        {
            foreach (var c in list ?? new())
            {
                if (string.IsNullOrWhiteSpace(c.Id)) { warn.Add($"فئة {kind} بدون Id - اتخطّت"); continue; }
                cats.Add(new FinancialCategoryEntity
                {
                    BuildingId = bid, Kind = kind, Id = Cut(c.Id, 64), Name = Cut(c.Name, 200),
                    Color = Cut(c.Color, 20), IsActive = c.Active, SortOrder = c.Order
                });
            }
        }
        AddCats("expense", src.ExpenseCategories);
        AddCats("revenue", src.RevenueCategories);
        var expCatIds = cats.Where(c => c.Kind == "expense").Select(c => c.Id).ToHashSet();
        var revCatIds = cats.Where(c => c.Kind == "revenue").Select(c => c.Id).ToHashSet();
        var catsExisting = await db.FinancialCategories.Where(x => x.BuildingId == bid).ToListAsync(ct);
        Sync(db, catsExisting, cats, x => (x.Kind, x.Id), warn, "categories");

        // ---------------- 6) الشهور: مصروفات / إيرادات / توزيع / دفعات ----------------
        var expenses = new List<ExpenseEntity>();
        var revenues = new List<RevenueEntity>();
        var shareMap = new Dictionary<(string Month, string Apt), MonthlyApartmentShareEntity>();
        var deposits = new List<DepositEntity>();
        var depositIds = new HashSet<string>();
        var editsByDeposit = new Dictionary<string, List<DepositEditEntity>>();

        void Share(string mk, string aptId, Action<MonthlyApartmentShareEntity> set)
        {
            if (!aptIds.Contains(aptId))
            { warn.Add($"{mk}: توزيع لشقة غير موجودة '{aptId}' - اتخطّى"); return; }
            if (!shareMap.TryGetValue((mk, aptId), out var s))
                shareMap[(mk, aptId)] = s = new MonthlyApartmentShareEntity
                { BuildingId = bid, MonthKey = mk, ApartmentId = aptId };
            set(s);
        }

        foreach (var (mk, m) in src.Months ?? new())
        {
            if (!MonthKeyRx.IsMatch(mk)) { warn.Add($"شهر بمفتاح غير صالح '{mk}' - اتخطّى"); continue; }

            foreach (var e in m.Expenses ?? new())
            {
                if (string.IsNullOrWhiteSpace(e.Id)) { warn.Add($"{mk}: مصروف بدون Id - اتخطّى"); continue; }
                var amount = Dec(e.Amount);
                if (amount < 0) { warn.Add($"{mk}: مصروف {e.Id} مبلغه سالب - اتخطّى"); continue; }
                var catId = string.IsNullOrWhiteSpace(e.CategoryId) ? null : e.CategoryId;
                if (catId != null && !expCatIds.Contains(catId))
                { warn.Add($"{mk}: مصروف {e.Id} فئته '{catId}' مش موجودة - اتحفظ بدون فئة"); catId = null; }
                expenses.Add(new ExpenseEntity
                {
                    BuildingId = bid, Id = Cut(e.Id, 64), MonthKey = mk, CategoryId = catId,
                    Note = Cut(e.Note, 1000), Amount = amount,
                    ExpenseDate = ParseDate(e.Date) ?? FirstOfMonth(mk),
                    ReceiptUrl = e.Receipt == null ? null : Cut(e.Receipt, 1000)
                });
            }

            foreach (var r in m.Revenues ?? new())
            {
                if (string.IsNullOrWhiteSpace(r.Id)) { warn.Add($"{mk}: إيراد بدون Id - اتخطّى"); continue; }
                var amount = Dec(r.Amount);
                if (amount < 0) { warn.Add($"{mk}: إيراد {r.Id} مبلغه سالب - اتخطّى"); continue; }
                var catId = string.IsNullOrWhiteSpace(r.CategoryId) ? null : r.CategoryId;
                if (catId != null && !revCatIds.Contains(catId))
                { warn.Add($"{mk}: إيراد {r.Id} فئته '{catId}' مش موجودة - اتحفظ بدون فئة"); catId = null; }
                revenues.Add(new RevenueEntity
                {
                    BuildingId = bid, Id = Cut(r.Id, 64), MonthKey = mk, CategoryId = catId,
                    Note = Cut(r.Note, 1000), Amount = amount,
                    RevenueDate = ParseDate(r.Date) ?? FirstOfMonth(mk)
                });
            }

            foreach (var (aptId, v) in m.Distribution ?? new()) Share(mk, aptId, s => s.ExpenseShare = Dec(v));
            foreach (var (aptId, v) in m.RevenueDistribution ?? new()) Share(mk, aptId, s => s.RevenueShare = Dec(v));
            foreach (var (aptId, v) in m.CarryOver ?? new()) Share(mk, aptId, s => s.CarryOver = Dec(v));

            foreach (var d in m.Deposits ?? new())
            {
                if (string.IsNullOrWhiteSpace(d.Id)) { warn.Add($"{mk}: دفعة بدون Id - اتخطّت"); continue; }
                if (!aptIds.Contains(d.AptId))
                { warn.Add($"{mk}: دفعة {d.Id} لشقة غير موجودة '{d.AptId}' - اتخطّت"); continue; }
                var amount = Dec(d.Amount);
                if (amount <= 0) { warn.Add($"{mk}: دفعة {d.Id} مبلغها <= 0 - اتخطّت"); continue; }
                if (d.Status is not ("pending" or "confirmed" or "cancelled"))
                { warn.Add($"{mk}: دفعة {d.Id} حالتها '{d.Status}' غير معروفة - اتخطّت"); continue; }
                if (!depositIds.Add(d.Id)) { warn.Add($"دفعة مكررة {d.Id} - اتخطّت النسخة التانية"); continue; }

                var rc = d.Receipt;
                deposits.Add(new DepositEntity
                {
                    BuildingId = bid, Id = Cut(d.Id, 64), MonthKey = mk, Number = Cut(d.Number, 50),
                    ApartmentId = d.AptId, Amount = amount, Note = Cut(d.Note, 1000), Status = d.Status,
                    ReceiptUrl = rc == null ? null : Cut(rc.Url, 1000),
                    ReceiptPath = rc == null ? null : Cut(rc.Path, 500),
                    ReceiptFileName = rc == null ? null : Cut(rc.FileName, 260),
                    ReceiptFileSize = rc?.FileSize,
                    ReceiptFileType = rc == null ? null : Cut(rc.FileType, 100),
                    ReceiptSuccess = rc?.Success,
                    CreatedAt = ParseTs(d.CreatedAt) ?? NowSec(),
                    CreatedBy = Cut(d.CreatedBy, 128),
                    ConfirmedAt = ParseTs(d.ConfirmedAt), ConfirmedBy = d.ConfirmedBy,
                    CancelledAt = ParseTs(d.CancelledAt), CancelledBy = d.CancelledBy,
                    CancelledReason = d.CancelledReason,
                    UpdatedAt = ParseTs(d.UpdatedAt), UpdatedBy = d.UpdatedBy, UpdateReason = d.UpdateReason
                });

                if (d.EditHistory is { Count: > 0 })
                    editsByDeposit[d.Id] = d.EditHistory.Select(x => new DepositEditEntity
                    {
                        BuildingId = bid, DepositId = d.Id, Ts = ParseTs(x.Ts) ?? NowSec(),
                        ByUser = Cut(x.By, 128), Reason = Cut(x.Reason, 500),
                        OldAmount = Dec(x.OldAmount), NewAmount = Dec(x.NewAmount),
                        OldNote = Cut(x.OldNote, 1000), NewNote = Cut(x.NewNote, 1000)
                    }).ToList();
            }
        }

        Sync(db, await db.Expenses.Where(x => x.BuildingId == bid).ToListAsync(ct),
            expenses, x => x.Id, warn, "expenses");
        Sync(db, await db.Revenues.Where(x => x.BuildingId == bid).ToListAsync(ct),
            revenues, x => x.Id, warn, "revenues");
        Sync(db, await db.MonthlyApartmentShares.Where(x => x.BuildingId == bid).ToListAsync(ct),
            shareMap.Values, x => (x.MonthKey, x.ApartmentId), warn, "shares");
        Sync(db, await db.Deposits.Where(x => x.BuildingId == bid).ToListAsync(ct),
            deposits, x => x.Id, warn, "deposits");

        // سجل تعديلات الدفعات: بنستبدله بس لو اتغيّر
        var editsExisting = (await db.DepositEditHistory.Where(x => x.BuildingId == bid)
                .OrderBy(x => x.EditId).ToListAsync(ct))
            .GroupBy(x => x.DepositId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var (depId, incoming) in editsByDeposit)
        {
            editsExisting.TryGetValue(depId, out var cur);
            cur ??= new List<DepositEditEntity>();
            if (SameEdits(cur, incoming)) continue;
            db.DepositEditHistory.RemoveRange(cur);
            db.DepositEditHistory.AddRange(incoming);
        }
        foreach (var (depId, cur) in editsExisting)
            if (depositIds.Contains(depId) && !editsByDeposit.ContainsKey(depId))
                db.DepositEditHistory.RemoveRange(cur);

        // ---------------- 7) تعديلات المحفظة ----------------
        var adjustments = new List<WalletAdjustmentEntity>();
        foreach (var a in src.WalletAdjustments ?? new())
        {
            if (string.IsNullOrWhiteSpace(a.Id)) { warn.Add("تعديل محفظة بدون Id - اتخطّى"); continue; }
            if (!aptIds.Contains(a.AptId))
            { warn.Add($"تعديل محفظة {a.Id} لشقة غير موجودة '{a.AptId}' - اتخطّى"); continue; }
            if (!MonthKeyRx.IsMatch(a.MonthKey ?? ""))
            { warn.Add($"تعديل محفظة {a.Id} شهره '{a.MonthKey}' غير صالح - اتخطّى"); continue; }
            adjustments.Add(new WalletAdjustmentEntity
            {
                BuildingId = bid, Id = Cut(a.Id, 64), ApartmentId = a.AptId, Amount = Dec(a.Amount),
                Reason = Cut(a.Reason, 500), MonthKey = a.MonthKey!,
                CreatedAt = ParseTs(a.CreatedAt) ?? NowSec(), CreatedBy = Cut(a.CreatedBy, 128)
            });
        }
        Sync(db, await db.WalletAdjustments.Where(x => x.BuildingId == bid).ToListAsync(ct),
            adjustments, x => x.Id, warn, "walletAdjustments");

        // ---------------- 8) الإشعارات ----------------
        var notifs = new List<NotificationEntity>();
        foreach (var n in src.Notifications ?? new())
        {
            if (string.IsNullOrWhiteSpace(n.Id)) { warn.Add("إشعار بدون Id - اتخطّى"); continue; }
            if (n.RecipientType is not ("admin" or "resident"))
            { warn.Add($"إشعار {n.Id} نوع مستلمه '{n.RecipientType}' غير معروف - اتخطّى"); continue; }
            string? aptRef = null;
            if (n.RecipientType == "resident")
            {
                if (string.IsNullOrWhiteSpace(n.RecipientId) || !aptIds.Contains(n.RecipientId))
                { warn.Add($"إشعار {n.Id} لشقة غير موجودة '{n.RecipientId}' - اتخطّى"); continue; }
                aptRef = n.RecipientId;
            }
            notifs.Add(new NotificationEntity
            {
                BuildingId = bid, Id = Cut(n.Id, 64), Ts = ParseTs(n.Ts) ?? NowSec(),
                RecipientType = n.RecipientType, RecipientApartmentId = aptRef,
                Type = Cut(n.Type, 30), Icon = Cut(n.Icon, 20), Title = Cut(n.Title, 300),
                Body = Cut(n.Body, 2000), IsRead = n.Read, ReadAt = ParseTs(n.ReadAt)
            });
        }
        Sync(db, await db.Notifications.Where(x => x.BuildingId == bid).ToListAsync(ct),
            notifs, x => x.Id, warn, "notifications");

        // ---------------- 9) التصويت ----------------
        var pollsIn = new List<PollEntity>();
        var optsIn = new List<PollOptionEntity>();
        var votesIn = new List<PollVoteEntity>();
        foreach (var p in src.Polls ?? new())
        {
            if (string.IsNullOrWhiteSpace(p.Id)) { warn.Add("تصويت بدون Id - اتخطّى"); continue; }
            pollsIn.Add(new PollEntity
            {
                BuildingId = bid, Id = Cut(p.Id, 64), Question = Cut(p.Question, 500),
                Description = Cut(p.Description, 2000), CreatedBy = Cut(p.CreatedBy, 128),
                CreatedAt = ParseTs(p.CreatedAt) ?? NowSec(), Deadline = ParseTs(p.Deadline), IsClosed = p.Closed
            });
            var optIds = new HashSet<string>();
            var i = 0;
            foreach (var o in p.Options ?? new())
            {
                if (string.IsNullOrWhiteSpace(o.Id)) { warn.Add($"تصويت {p.Id}: اختيار بدون Id - اتخطّى"); continue; }
                if (!optIds.Add(o.Id)) continue;
                optsIn.Add(new PollOptionEntity
                { BuildingId = bid, PollId = p.Id, Id = Cut(o.Id, 64), Text = Cut(o.Text, 500), SortOrder = i++ });
            }
            foreach (var (aptId, optId) in p.Votes ?? new())
            {
                if (!aptIds.Contains(aptId) || !optIds.Contains(optId))
                { warn.Add($"تصويت {p.Id}: صوت الشقة '{aptId}' للاختيار '{optId}' غير صالح - اتخطّى"); continue; }
                votesIn.Add(new PollVoteEntity
                { BuildingId = bid, PollId = p.Id, ApartmentId = aptId, OptionId = optId });
            }
        }
        Sync(db, await db.Polls.Where(x => x.BuildingId == bid).ToListAsync(ct),
            pollsIn, x => x.Id, warn, "polls");
        Sync(db, await db.PollOptions.Where(x => x.BuildingId == bid).ToListAsync(ct),
            optsIn, x => (x.PollId, x.Id), warn, "pollOptions");
        Sync(db, await db.PollVotes.Where(x => x.BuildingId == bid).ToListAsync(ct),
            votesIn, x => (x.PollId, x.ApartmentId), warn, "pollVotes");

        // ---------------- 10) الصيانة ----------------
        var maint = new List<MaintenanceRecordEntity>();
        foreach (var r in src.MaintenanceLog ?? new())
        {
            if (string.IsNullOrWhiteSpace(r.Id)) { warn.Add("سجل صيانة بدون Id - اتخطّى"); continue; }
            if (r.Status is not ("open" or "inprogress" or "done" or "cancelled"))
            { warn.Add($"صيانة {r.Id} حالتها '{r.Status}' غير معروفة - اتخطّت"); continue; }
            var cost = Dec(r.Cost);
            if (cost < 0) { warn.Add($"صيانة {r.Id}: تكلفة سالبة اتحوّلت لصفر"); cost = 0; }
            maint.Add(new MaintenanceRecordEntity
            {
                BuildingId = bid, Id = Cut(r.Id, 64), Title = Cut(r.Title, 300),
                Description = Cut(r.Description, 2000),
                MaintenanceDate = ParseDate(r.Date) ?? DateOnly.FromDateTime(DateTime.UtcNow),
                Cost = cost, Vendor = Cut(r.Vendor, 200), Status = r.Status, CreatedBy = Cut(r.CreatedBy, 128),
                CreatedAt = ParseTs(r.CreatedAt) ?? NowSec(), AddedToExpense = r.AddedToExpense,
                AddedToExpenseAt = ParseTs(r.AddedToExpenseAt)
            });
        }
        Sync(db, await db.MaintenanceRecords.Where(x => x.BuildingId == bid).ToListAsync(ct),
            maint, x => x.Id, warn, "maintenance");

        // ---------------- 11) Aggregate audit: append-only target events ----------------
        var knownAudit = (await db.AuditEvents
                .Where(x => x.BuildingId == bid && x.EventCategory == "BuildingAggregate" &&
                            x.ResourceType == "BuildingAuditEntry" && x.ResourceReference != null)
                .Select(x => x.ResourceReference!).ToListAsync(ct)).ToHashSet(StringComparer.Ordinal);
        foreach (var e in src.AuditLog ?? new())
        {
            var key = AuditKey(e);
            if (!knownAudit.Add(key)) continue;
            var severity = string.IsNullOrWhiteSpace(e.Severity) ? "info" : e.Severity;
            db.AuditEvents.Add(new AuditEventEntity
            {
                AuditEventId = Guid.NewGuid(),
                OccurredAtUtc = ParseTs(e.Ts) ?? NowSec(),
                ActorReference = Cut(e.Actor, 160),
                BuildingId = bid,
                EventCategory = "BuildingAggregate",
                EventType = Cut(e.Action, 120),
                ResourceType = "BuildingAuditEntry",
                ResourceReference = key,
                Outcome = AuditOutcome(severity),
                SafeDetailsJson = JsonSerializer.Serialize(new
                {
                    label = Cut(e.Label, 300),
                    actorRole = Cut(e.ActorRole, 20),
                    details = RedactAuditDetails(e.Details),
                    month = Cut(e.Month, 7),
                    buildingNumber = e.BuildingNumber == null ? null : Cut(e.BuildingNumber, 50),
                    aptNumber = e.AptNumber
                })
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        src.RowVersion = be.RowVersion.ToArray();
        return warn;
    }

    internal static AuditLogEntry MapAggregateAuditEvent(AuditEventEntity auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        using var payload = JsonDocument.Parse(
            string.IsNullOrWhiteSpace(auditEvent.SafeDetailsJson) ? "{}" : auditEvent.SafeDetailsJson);
        var root = payload.RootElement;
        return new AuditLogEntry
        {
            Id = auditEvent.ResourceReference ?? auditEvent.AuditEventId.ToString("N"),
            Ts = FormatTs(auditEvent.OccurredAtUtc) ?? "",
            Action = auditEvent.EventType,
            Label = JsonString(root, "label") ?? "",
            Actor = auditEvent.ActorReference ?? "",
            ActorRole = JsonString(root, "actorRole") ?? "",
            Details = JsonString(root, "details") ?? "",
            Month = JsonString(root, "month") ?? "",
            BuildingNumber = JsonNullableString(root, "buildingNumber"),
            AptNumber = JsonNullableInt(root, "aptNumber"),
            Severity = AuditSeverity(auditEvent.Outcome)
        };
    }

    internal static string RedactAuditDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return "";
        return Regex.Replace(
            details,
            @"(?i)(password|secret|token|credential|pin|recovery(?:code|codes)?)(\s*[:=]\s*)(?:""[^""]*""|'[^']*'|[^,\s;]+)",
            "$1$2[REDACTED]");
    }

    private static string? JsonString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static string? JsonNullableString(JsonElement root, string name) => JsonString(root, name);

    private static int? JsonNullableInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static string AuditSeverity(byte outcome) => outcome switch
    {
        1 => "warning",
        2 => "error",
        3 => "critical",
        _ => "info"
    };

    private static byte AuditOutcome(string? severity) => severity?.Trim().ToLowerInvariant() switch
    {
        "warning" or "warn" => 1,
        "error" => 2,
        "critical" or "fatal" => 3,
        _ => 0
    };

    internal static void MarkAggregateWrite(AppDbContext db, BuildingEntity building)
    {
        db.Entry(building).Property(x => x.DataVersion).IsModified = true;
    }

    internal static void ValidateAggregateConcurrency(byte[]? expected, byte[] current)
    {
        if (expected is not { Length: 8 } || current is not { Length: 8 })
            throw new DbUpdateConcurrencyException(
                "Building concurrency token is missing. Reload the building before saving.");

        if (!CryptographicOperations.FixedTimeEquals(expected, current))
            throw new DbUpdateConcurrencyException(
                "Building data changed after it was loaded. Reload and retry the operation.");
    }

    private static bool SameEdits(List<DepositEditEntity> a, List<DepositEditEntity> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            var x = a[i]; var y = b[i];
            if (x.Ts != y.Ts || x.ByUser != y.ByUser || x.Reason != y.Reason ||
                x.OldAmount != y.OldAmount || x.NewAmount != y.NewAmount ||
                x.OldNote != y.OldNote || x.NewNote != y.NewNote) return false;
        }
        return true;
    }
}
