using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// ترجمة حرفية لـ js/core/notifications.js (إشعارات داخل التطبيق)
public class NotificationsService
{
    public Notification Push(Building building, string recipientType, string? recipientId, string type,
        string icon, string title, string body)
    {
        building.Notifications ??= new List<Notification>();

        var n = new Notification
        {
            Id = "n-" + Guid.NewGuid().ToString("N"),
            Ts = DateTime.UtcNow.ToString("o"),
            RecipientType = recipientType,
            RecipientId = recipientId,
            Type = type,
            Icon = icon,
            Title = title,
            Body = body,
            Read = false
        };

        building.Notifications.Add(n);

        // حد أقصى 200 إشعار
        if (building.Notifications.Count > 200)
            building.Notifications = building.Notifications.Skip(building.Notifications.Count - 200).ToList();

        return n;
    }

    public List<Notification> GetForAdmin(Building building, int max = 20) =>
        building.Notifications.Where(n => n.RecipientType == "admin")
            .OrderByDescending(n => n.Ts).Take(max).ToList();

    public List<Notification> GetForResident(Building building, string aptId, int max = 20) =>
        building.Notifications.Where(n => n.RecipientType == "resident" && n.RecipientId == aptId)
            .OrderByDescending(n => n.Ts).Take(max).ToList();

    public int UnreadCountForAdmin(Building building) =>
        building.Notifications.Count(n => n.RecipientType == "admin" && !n.Read);

    public int UnreadCountForResident(Building building, string aptId) =>
        building.Notifications.Count(n => n.RecipientType == "resident" && n.RecipientId == aptId && !n.Read);

    public bool MarkRead(Building building, string notifId)
    {
        var n = building.Notifications.FirstOrDefault(x => x.Id == notifId);
        if (n == null) return false;
        n.Read = true;
        n.ReadAt = DateTime.UtcNow.ToString("o");
        return true;
    }

    public void MarkAllReadForAdmin(Building building)
    {
        foreach (var n in building.Notifications.Where(n => n.RecipientType == "admin" && !n.Read))
        {
            n.Read = true;
            n.ReadAt = DateTime.UtcNow.ToString("o");
        }
    }

    public void MarkAllReadForResident(Building building, string aptId)
    {
        foreach (var n in building.Notifications.Where(n => n.RecipientType == "resident" && n.RecipientId == aptId && !n.Read))
        {
            n.Read = true;
            n.ReadAt = DateTime.UtcNow.ToString("o");
        }
    }

    // -------- Helpers لنفس إشعارات الأصل الجاهزة --------
    public void NotifyAdminNewDeposit(Building building, Deposit deposit, Apartment apt) =>
        Push(building, "admin", null, "deposit_new", "💰", "دفعة جديدة",
            $"شقة {apt.Number}: {deposit.Amount:0.##} ج.م — {building.Name}");

    public void NotifyResidentDepositConfirmed(Building building, Deposit deposit, Apartment apt) =>
        Push(building, "resident", apt.Id, "deposit_confirmed", "✅", "تم تأكيد الدفعة",
            $"{deposit.Amount:0.##} ج.م — {building.Name}");

    public void NotifyResidentDepositCancelled(Building building, Deposit deposit, Apartment apt, string? reason) =>
        Push(building, "resident", apt.Id, "deposit_cancelled", "❌", "تم إلغاء الدفعة",
            $"{deposit.Amount:0.##} ج.م — السبب: {(string.IsNullOrWhiteSpace(reason) ? "—" : reason)}");

    public void NotifyResidentsNewExpense(Building building, string categoryName, double amount)
    {
        foreach (var apt in building.Apartments.Where(a => !a.Closed))
            Push(building, "resident", apt.Id, "expense_new", "💸", "مصروف جديد",
                $"{categoryName}: {amount:0.##} ج.م — {building.Name}");
    }

    // ============================================================
    // ✅ Phase 21: إشعار الساكن إن دفعة معلقة اتعدلت
    // ============================================================
    public void NotifyResidentDepositUpdated(
        Building building,
        Deposit deposit,
        Apartment apt,
        double oldAmount,
        string? reason)
    {
        var body = $"دفعتك {deposit.Number} اتعدلت من {oldAmount:0.##} ج.م إلى {deposit.Amount:0.##} ج.م";
        if (!string.IsNullOrWhiteSpace(reason))
            body += $" — السبب: {reason}";
        body += $" — {building.Name}";

        Push(building, "resident", apt.Id, "deposit_updated", "✏️", "تم تعديل الدفعة", body);
    }
    // ✅ Phase 24.5 — إشعار الساكن بفتح الشقة
    public void NotifyResidentApartmentReopened(
        Building building,
        Apartment apt,
        string? reason)
    {
        var body = $"تم إعادة تفعيل شقتك رقم {apt.Number} في {building.Name}";
        if (!string.IsNullOrWhiteSpace(reason))
            body += $" — {reason}";
        body += ". هتبدأ تدفع من الشهر الجاي.";

        Push(building, "resident", apt.Id, "apartment_reopened", "🔓", "تم إعادة تفعيل شقتك", body);
    }

    // ✅ Phase 24.5 — إشعار الساكن بإغلاق الشقة
    public void NotifyResidentApartmentClosed(
        Building building,
        Apartment apt,
        string? reason)
    {
        var body = $"تم إغلاق شقتك رقم {apt.Number} في {building.Name}";
        if (!string.IsNullOrWhiteSpace(reason))
            body += $" — السبب: {reason}";
        body += ". مش هتتضمن في المصروفات دلوقتي.";

        Push(building, "resident", apt.Id, "apartment_closed", "🔒", "تم إغلاق شقتك", body);
    }
    // ✅ تعطيل الشقة
    public void NotifyResidentApartmentDisabled(
        Building building,
        Apartment apt,
        string? reason)
    {
        var body = $"تم تعطيل شقتك رقم {apt.Number} في {building.Name}";
        if (!string.IsNullOrWhiteSpace(reason))
            body += $" — السبب: {reason}";
        body += ". مش هتقدر تدخل التطبيق دلوقتي. كلم الإدارة.";

        Push(building, "resident", apt.Id, "apartment_disabled", "🚫", "تم تعطيل شقتك", body);
    }

    // ✅ تفعيل الشقة
    public void NotifyResidentApartmentEnabled(
        Building building,
        Apartment apt,
        string? reason)
    {
        var body = $"تم إعادة تفعيل شقتك رقم {apt.Number} في {building.Name}";
        if (!string.IsNullOrWhiteSpace(reason))
            body += $" — {reason}";
        body += ". أهلاً بيك تاني!";

        Push(building, "resident", apt.Id, "apartment_enabled", "✅", "تم إعادة تفعيل شقتك", body);
    }
}