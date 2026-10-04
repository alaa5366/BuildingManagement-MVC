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
using System.Text.RegularExpressions;
using BuildingManagementMvc.Data.Entities;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Data;

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

    private static decimal Dec(double d) =>
        double.IsFinite(d) ? Math.Round((decimal)d, 2, MidpointRounding.AwayFromZero) : 0m;

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
    public static async Task<Building?> LoadAsync(
        AppDbContext db, string id, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        var be = await db.Buildings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (be == null) return null;

        var floors = await db.Floors.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var apts = await db.Apartments.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var cats = await db.FinancialCategories.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var expenses = await db.Expenses.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var revenues = await db.Revenues.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var shares = await db.MonthlyApartmentShares.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var deposits = await db.Deposits.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var edits = await db.DepositEditHistory.AsNoTracking().Where(x => x.BuildingId == id)
            .OrderBy(x => x.EditId).ToListAsync(ct);
        var adjustments = await db.WalletAdjustments.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var notifs = await db.Notifications.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var polls = await db.Polls.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var options = await db.PollOptions.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var votes = await db.PollVotes.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var maint = await db.MaintenanceRecords.AsNoTracking().Where(x => x.BuildingId == id).ToListAsync(ct);
        var admins = await db.BuildingAdmins.AsNoTracking().Where(x => x.BuildingId == id)
            .Select(x => x.AdminUid).ToListAsync(ct);
        var audit = await db.AuditLog.AsNoTracking().Where(x => x.BuildingId == id)
            .OrderByDescending(x => x.Ts).ThenByDescending(x => x.AuditId)
            .Take(AuditCap).ToListAsync(ct);
        audit.Reverse(); // الأقدم الأول، زي AuditLogService.Push

        var b = new Building
        {
            Id = be.Id,
            BuildingNumber = be.BuildingNumber,
            Name = be.Name,
            AdminPin = be.AdminPin,
            DataVersion = be.DataVersion,
            LogoUrl = be.LogoUrl,
            AdminWhatsapp = be.AdminWhatsapp,
            CreatedAt = FormatTs(be.CreatedAt) ?? "",
            PaymentInfo = new PaymentInfo
            {
                Label = be.PaymentLabel,
                AccountNumber = be.PaymentAccountNumber,
                Phone = be.PaymentPhone,
                Notes = be.PaymentNotes
            },
            AdminUids = admins,
            Floors = floors.OrderBy(f => f.SortOrder)
                .Select(f => new Floor { Id = f.Id, Label = f.Label, Order = f.SortOrder }).ToList(),
            Apartments = apts.Select(a => new Apartment
            {
                Id = a.Id,
                FloorId = a.FloorId,
                Number = a.Number,
                Owner = a.Owner,
                Phone = a.Phone,
                MonthlyFee = (double)a.MonthlyFee,
                Pin = a.Pin,
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
            ExpenseCategories = cats.Where(c => c.Kind == "expense").OrderBy(c => c.SortOrder)
                .Select(ToCategory).ToList(),
            RevenueCategories = cats.Where(c => c.Kind == "revenue").OrderBy(c => c.SortOrder)
                .Select(ToCategory).ToList(),
            WalletAdjustments = adjustments.OrderBy(a => a.CreatedAt).Select(a => new WalletAdjustment
            {
                Id = a.Id,
                AptId = a.ApartmentId,
                Amount = (double)a.Amount,
                Reason = a.Reason,
                MonthKey = a.MonthKey,
                CreatedAt = FormatTs(a.CreatedAt) ?? "",
                CreatedBy = a.CreatedBy
            }).ToList(),
            Notifications = notifs.OrderBy(n => n.Ts).Select(n => new Notification
            {
                Id = n.Id,
                Ts = FormatTs(n.Ts) ?? "",
                RecipientType = n.RecipientType,
                RecipientId = n.RecipientApartmentId,
                Type = n.Type,
                Icon = n.Icon,
                Title = n.Title,
                Body = n.Body,
                Read = n.IsRead,
                ReadAt = FormatTs(n.ReadAt)
            }).ToList(),
            MaintenanceLog = maint.OrderBy(m => m.MaintenanceDate).ThenBy(m => m.CreatedAt)
                .Select(m => new MaintenanceRecord
                {
                    Id = m.Id,
                    Title = m.Title,
                    Description = m.Description,
                    Date = FormatDate(m.MaintenanceDate) ?? "",
                    Cost = (double)m.Cost,
                    Vendor = m.Vendor,
                    Status = m.Status,
                    CreatedBy = m.CreatedBy,
                    CreatedAt = FormatTs(m.CreatedAt) ?? "",
                    AddedToExpense = m.AddedToExpense,
                    AddedToExpenseAt = FormatTs(m.AddedToExpenseAt)
                }).ToList(),
            AuditLog = audit.Select(a => new AuditLogEntry
            {
                Id = a.SourceId ?? a.AuditId.ToString(Inv),
                Ts = FormatTs(a.Ts) ?? "",
                Action = a.Action,
                Label = a.Label,
                Actor = a.Actor,
                ActorRole = a.ActorRole,
                Details = a.Details,
                Month = a.MonthKey,
                BuildingNumber = a.BuildingNumber,
                AptNumber = a.AptNumber
            }).ToList()
        };

        // ---- الشهور ----
        MonthData Month(string key)
        {
            if (!b.Months.TryGetValue(key, out var m)) b.Months[key] = m = new MonthData();
            return m;
        }

        foreach (var e in expenses.OrderBy(x => x.ExpenseDate).ThenBy(x => x.Id))
            Month(e.MonthKey).Expenses.Add(new Expense
            {
                Id = e.Id,
                CategoryId = e.CategoryId ?? "",
                Note = e.Note,
                Amount = (double)e.Amount,
                Date = FormatDate(e.ExpenseDate) ?? "",
                Receipt = e.ReceiptUrl
            });

        foreach (var r in revenues.OrderBy(x => x.RevenueDate).ThenBy(x => x.Id))
            Month(r.MonthKey).Revenues.Add(new Revenue
            {
                Id = r.Id,
                CategoryId = r.CategoryId ?? "",
                Note = r.Note,
                Amount = (double)r.Amount,
                Date = FormatDate(r.RevenueDate) ?? ""
            });

        // ملحوظة: أي شقة ليها صف share بتظهر في الـ 3 dictionaries (القيمة 0 لو ماكانتش موجودة).
        foreach (var s in shares)
        {
            var m = Month(s.MonthKey);
            m.Distribution[s.ApartmentId] = (double)s.ExpenseShare;
            m.RevenueDistribution[s.ApartmentId] = (double)s.RevenueShare;
            m.CarryOver[s.ApartmentId] = (double)s.CarryOver;
        }

        var editsByDep = edits.GroupBy(x => x.DepositId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var d in deposits.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
        {
            var dep = new Deposit
            {
                Id = d.Id,
                Number = d.Number,
                AptId = d.ApartmentId,
                Amount = (double)d.Amount,
                Note = d.Note,
                Status = d.Status,
                Receipt = HasReceipt(d)
                    ? new ReceiptData
                    {
                        Url = d.ReceiptUrl ?? "",
                        Path = d.ReceiptPath ?? "",
                        FileName = d.ReceiptFileName ?? "",
                        FileSize = d.ReceiptFileSize ?? 0,
                        FileType = d.ReceiptFileType ?? "",
                        Success = d.ReceiptSuccess ?? false
                    }
                    : null,
                CreatedAt = FormatTs(d.CreatedAt) ?? "",
                CreatedBy = d.CreatedBy,
                ConfirmedAt = FormatTs(d.ConfirmedAt),
                ConfirmedBy = d.ConfirmedBy,
                CancelledAt = FormatTs(d.CancelledAt),
                CancelledBy = d.CancelledBy,
                CancelledReason = d.CancelledReason,
                UpdatedAt = FormatTs(d.UpdatedAt),
                UpdatedBy = d.UpdatedBy,
                UpdateReason = d.UpdateReason,
                EditHistory = editsByDep.TryGetValue(d.Id, out var list)
                    ? list.Select(x => new DepositEditEntry
                    {
                        Ts = FormatTs(x.Ts) ?? "",
                        By = x.ByUser,
                        Reason = x.Reason,
                        OldAmount = (double)x.OldAmount,
                        NewAmount = (double)x.NewAmount,
                        OldNote = x.OldNote,
                        NewNote = x.NewNote
                    }).ToList()
                    : null
            };
            Month(d.MonthKey).Deposits.Add(dep);
        }

        // ---- التصويت ----
        var optsByPoll = options.GroupBy(o => o.PollId).ToDictionary(g => g.Key, g => g.OrderBy(o => o.SortOrder).ToList());
        var votesByPoll = votes.GroupBy(v => v.PollId).ToDictionary(g => g.Key, g => g.ToList());
        b.Polls = polls.OrderBy(p => p.CreatedAt).Select(p => new Poll
        {
            Id = p.Id,
            Question = p.Question,
            Description = p.Description,
            CreatedBy = p.CreatedBy,
            CreatedAt = FormatTs(p.CreatedAt) ?? "",
            Deadline = FormatTs(p.Deadline),
            Closed = p.IsClosed,
            Options = optsByPoll.TryGetValue(p.Id, out var os)
                ? os.Select(o => new PollOption { Id = o.Id, Text = o.Text }).ToList()
                : new List<PollOption>(),
            Votes = votesByPoll.TryGetValue(p.Id, out var vs)
                ? vs.ToDictionary(v => v.ApartmentId, v => v.OptionId)
                : new Dictionary<string, string>()
        }).ToList();

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
        be.BuildingNumber = Cut(src.BuildingNumber, 50);
        be.Name = Cut(src.Name, 200);
        be.AdminPin = Cut(src.AdminPin, 256);
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
                Pin = Cut(a.Pin, 256),
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

        // ---------------- 11) سجل المراجعة: Append-only ----------------
        var knownAudit = (await db.AuditLog.Where(x => x.BuildingId == bid && x.SourceId != null)
                .Select(x => x.SourceId!).ToListAsync(ct)).ToHashSet();
        foreach (var e in src.AuditLog ?? new())
        {
            var key = AuditKey(e);
            if (!knownAudit.Add(key)) continue;
            db.AuditLog.Add(new AuditLogEntity
            {
                SourceId = key, BuildingId = bid, Ts = ParseTs(e.Ts) ?? NowSec(),
                Action = Cut(e.Action, 100), Label = Cut(e.Label, 300), Actor = Cut(e.Actor, 200),
                ActorRole = Cut(e.ActorRole, 20), Details = e.Details ?? "", MonthKey = Cut(e.Month, 7),
                BuildingNumber = e.BuildingNumber == null ? null : Cut(e.BuildingNumber, 50),
                AptNumber = e.AptNumber
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return warn;
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
