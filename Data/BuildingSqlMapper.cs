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
            AdminUids = new List<string>(),
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
        var transactionById = postedTransactions.ToDictionary(x => x.LedgerTransactionId);
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

            var sourceKind = transaction.Kind;
            var isReversal = transaction.Kind == LedgerTransactionKind.Reversal;
            if (isReversal && transaction.ReversesTransactionId is Guid originalId &&
                transactionById.TryGetValue(originalId, out var original))
                sourceKind = original.Kind;

            // A reversal is represented as a signed negative detail row in the
            // same financial category stream. Never leave the original amount
            // looking unreversed in the aggregate read model.
            var displayAmount = isReversal ? -amount : amount;
            if (sourceKind == LedgerTransactionKind.Expense)
            {
                month.Expenses.Add(new Expense
                {
                    Id = transactionId,
                    CategoryId = "",
                    Note = isReversal ? "Reversal: " + (transaction.Reason ?? "Expense reversal")
                        : transaction.Reason ?? "Expense",
                    Amount = displayAmount,
                    Date = effectiveDate
                });
            }
            else if (sourceKind == LedgerTransactionKind.Revenue)
            {
                month.Revenues.Add(new Revenue
                {
                    Id = transactionId,
                    CategoryId = "",
                    Note = isReversal ? "Reversal: " + (transaction.Reason ?? "Revenue reversal")
                        : transaction.Reason ?? "Revenue",
                    Amount = displayAmount,
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

        var warnings = new List<string>();
        var buildingId = Cut(src.Id, 64);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var building = await db.Buildings.FirstOrDefaultAsync(x => x.Id == buildingId, ct);
        if (building is null)
        {
            building = new BuildingEntity
            {
                Id = buildingId,
                CreatedAt = ParseTs(src.CreatedAt) ?? NowSec()
            };
            db.Buildings.Add(building);
        }
        else
        {
            ValidateAggregateConcurrency(src.RowVersion, building.RowVersion);
            MarkAggregateWrite(db, building);
        }

        building.BuildingNumber = Cut(src.BuildingNumber, 50);
        building.Name = Cut(src.Name, 200);
        building.AdminPin = "";
        building.DataVersion = Cut(src.DataVersion, 20);
        building.LogoUrl = Cut(src.LogoUrl, 1000);
        building.AdminWhatsapp = Cut(src.AdminWhatsapp, 32);
        var payment = src.PaymentInfo ?? new PaymentInfo();
        building.PaymentLabel = Cut(payment.Label, 200);
        building.PaymentAccountNumber = Cut(payment.AccountNumber, 100);
        building.PaymentPhone = Cut(payment.Phone, 32);
        building.PaymentNotes = Cut(payment.Notes, 500);

        // Identity assignments and financial/operational records are separate
        // target aggregates. Never synchronize them from a stale Building snapshot.
        if (src.AdminUids is { Count: > 0 })
            warnings.Add("Building administrator assignments are managed by target identity/access services.");

        var floors = (src.Floors ?? new())
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .Select(x => new FloorEntity
            {
                BuildingId = buildingId,
                Id = Cut(x.Id, 64),
                Label = Cut(x.Label, 100),
                SortOrder = x.Order
            }).ToList();
        var floorIds = floors.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var existingFloors = await db.Floors.Where(x => x.BuildingId == buildingId).ToListAsync(ct);
        Sync(db, existingFloors, floors, x => x.Id, warnings, "floors");

        var apartments = new List<ApartmentEntity>();
        var apartmentNumbers = new HashSet<int>();
        foreach (var apartment in src.Apartments ?? new())
        {
            if (string.IsNullOrWhiteSpace(apartment.Id))
            {
                warnings.Add("شقة بدون Id - اتخطّت");
                continue;
            }
            if (!floorIds.Contains(apartment.FloorId))
            {
                warnings.Add($"شقة {apartment.Number} ({apartment.Id}): الدور '{apartment.FloorId}' مش موجود - اتخطّت");
                continue;
            }
            if (!apartmentNumbers.Add(apartment.Number))
            {
                warnings.Add($"رقم الشقة {apartment.Number} مكرر - اتخطّت النسخة التانية ({apartment.Id})");
                continue;
            }

            var fee = Dec(apartment.MonthlyFee);
            if (fee < 0)
            {
                warnings.Add($"شقة {apartment.Number}: رسوم سالبة اتحوّلت لصفر");
                fee = 0;
            }
            var settings = apartment.Settings;
            apartments.Add(new ApartmentEntity
            {
                BuildingId = buildingId,
                Id = Cut(apartment.Id, 64),
                FloorId = apartment.FloorId,
                Number = apartment.Number,
                Owner = Cut(apartment.Owner, 200),
                Phone = Cut(apartment.Phone, 32),
                Email = Cut(apartment.Email, 256),
                MonthlyFee = fee,
                Pin = "",
                IsClosed = apartment.Closed,
                Label = Cut(apartment.Label, 100),
                Notes = Cut(apartment.Notes, 1000),
                OpenDate = ParseDate(apartment.OpenDate),
                CloseDate = ParseDate(apartment.CloseDate),
                IsDisabled = apartment.Disabled,
                DisabledReason = Cut(apartment.DisabledReason, 500),
                Language = Cut(settings?.Language ?? "ar", 10),
                NotifyWhatsApp = settings?.NotificationPreferences?.WhatsApp ?? true,
                NotifyEmail = settings?.NotificationPreferences?.Email ?? false,
                NotifyInApp = settings?.NotificationPreferences?.InApp ?? true,
                PreferredPaymentMethod = Cut(settings?.PreferredPaymentMethod ?? "instapay", 30),
                SettingsUpdatedAt = ParseTs(settings?.UpdatedAt),
                SettingsUpdatedBy = settings?.UpdatedBy
            });
        }
        var existingApartments = await db.Apartments.Where(x => x.BuildingId == buildingId).ToListAsync(ct);
        Sync(db, existingApartments, apartments, x => x.Id, warnings, "apartments");

        var categories = new List<FinancialCategoryEntity>();
        void AddCategories(string kind, List<FinancialCategory>? source)
        {
            foreach (var category in source ?? new())
            {
                if (string.IsNullOrWhiteSpace(category.Id))
                {
                    warnings.Add($"فئة {kind} بدون Id - اتخطّت");
                    continue;
                }
                categories.Add(new FinancialCategoryEntity
                {
                    BuildingId = buildingId,
                    Kind = kind,
                    Id = Cut(category.Id, 64),
                    Name = Cut(category.Name, 200),
                    Color = Cut(category.Color, 20),
                    IsActive = category.Active,
                    SortOrder = category.Order
                });
            }
        }
        AddCategories("expense", src.ExpenseCategories);
        AddCategories("revenue", src.RevenueCategories);
        var existingCategories = await db.FinancialCategories
            .Where(x => x.BuildingId == buildingId).ToListAsync(ct);
        Sync(db, existingCategories, categories, x => (x.Kind, x.Id), warnings, "categories");

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        SqlMirrorHealth.RecordSuccess();
        return warnings;
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
            @"(?i)(password|secret|token|credential|pin|recovery(?:code|codes)?)(\\s*[:=]\\s*)(?:""[^""]*""|'[^']*'|[^,\\s;]+)",
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
