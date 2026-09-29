using BuildingManagementMvc.Models;

namespace BuildingManagementMvc.Services;

// نسخة مبسطة من js/features/whatsapp-templates.js — نفس القوالب الافتراضية
// الخمسة، بس بدون تخصيص/حفظ قوالب معدّلة (يمكن إضافتها لاحقًا).
// الإرسال نفسه بيتم زي الأصل: فتح رابط wa.me جاهز بالنص، والأدمن يدوس إرسال.
public class WhatsAppTemplateService
{
    public static readonly Dictionary<string, (string Name, string Text)> Templates = new()
    {
        ["reminder"] = ("🔔 تذكير بالدفع",
            "مرحباً {owner},\n\nنود تذكيركم برصيد محفظتكم في {building}:\n\n💰 المبلغ المطلوب: {amount} ج.م\n🏠 الشقة: {apt}\n📅 الشهر: {month}\n\n{payment_info}\n\nشكراً لتعاونكم 🙏"),

        ["confirm"] = ("✅ تأكيد دفعة",
            "مرحباً {owner},\n\nتم تأكيد دفعتكم بنجاح ✅\n\n💰 المبلغ: {amount} ج.م\n🏠 الشقة: {apt}\n🏢 العمارة: {building}\n📅 الشهر: {month}\n\nشكراً لكم 🌹"),

        ["expense"] = ("💸 إشعار بمصروف",
            "مرحباً {owner},\n\nتم تسجيل مصروف جديد في {building}:\n\n📝 البيان: {note}\n💰 المبلغ: {amount} ج.م\n🏠 نصيب شقتك: {share} ج.م\n📅 الشهر: {month}\n\nشكراً لتعاونكم 🙏"),

        ["revenue"] = ("📈 إشعار بإيراد",
            "مرحباً {owner},\n\nتم إضافة إيراد جديد في {building}:\n\n📝 البيان: {note}\n💰 المبلغ: {amount} ج.م\n🏠 نصيب شقتك: +{share} ج.م\n📅 الشهر: {month}\n\nشكراً لكم 🌹"),

        ["general"] = ("📢 رسالة عامة",
            "مرحباً {owner},\n\n{message}\n\n🏢 {building}\n\nشكراً لكم 🌹"),

        // ✅ جديد — رسالة ترحيب + بيانات الدخول + رابط QR
        ["welcome"] = ("💌 رسالة ترحيب",
            "🎉 مرحباً {owner}!\n\n" +
            "تم تسجيلك في *{building}* بنجاح.\n\n" +
            "📋 *بياناتك:*\n" +
            "🏢 العمارة: {building}\n" +
            "🏠 الشقة رقم: {apt}\n" +
            "🏗️ الدور: {floor}\n\n" +
            "🔑 *بيانات الدخول:*\n" +
            "📱 رقم الموبايل: {phone}\n" +
            "🔒 PIN: {pin}\n\n" +
            "🚀 *طرق الدخول:*\n" +
            "1️⃣ *دخول سريع:*\n" +
            "امسح الـ QR المرفق أو اضغط الرابط:\n" +
            "{quick_link}\n\n" +
            "2️⃣ *دخول يدوي:*\n" +
            "{login_url}\n\n" +
            "⚠️ *ملاحظات:*\n" +
            "- الرابط صالح لمدة محدودة\n" +
            "- لا تشارك الـ PIN مع أحد\n\n" +
            "أهلاً بك 🌹"),
    };

    public string Render(string templateId, Building building, Apartment apt, Dictionary<string, string> extra)
    {
        if (!Templates.TryGetValue(templateId, out var tpl)) return "";
        var text = tpl.Text;

        var floor = building.Floors.FirstOrDefault(f => f.Id == apt.FloorId);

        var values = new Dictionary<string, string>
        {
            ["owner"] = string.IsNullOrWhiteSpace(apt.Owner) ? $"ساكن شقة {apt.Number}" : apt.Owner,
            ["building"] = building.Name,
            ["apt"] = apt.Number.ToString(),
            ["month"] = ReportsService.MonthLabel(WalletService.CurrentMonthKey()),
            ["payment_info"] = BuildPaymentInfoText(building),

            // ✅ جديد — للترحيب
            ["floor"] = floor?.Label ?? "—",
            ["phone"] = apt.Phone ?? "",
            ["pin"] = apt.Pin ?? "",
            ["quick_link"] = "",
            ["login_url"] = ""
        };

        foreach (var kv in extra)
            values[kv.Key] = kv.Value;

        foreach (var kv in values)
            text = text.Replace("{" + kv.Key + "}", kv.Value);

        return text;
    }

    private static string BuildPaymentInfoText(Building building)
    {
        var pay = building.PaymentInfo;
        if (string.IsNullOrWhiteSpace(pay.AccountNumber) && string.IsNullOrWhiteSpace(pay.Phone)) return "";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(pay.Label)) parts.Add($"الطريقة: {pay.Label}");
        if (!string.IsNullOrWhiteSpace(pay.AccountNumber)) parts.Add($"رقم الحساب: {pay.AccountNumber}");
        if (!string.IsNullOrWhiteSpace(pay.Phone)) parts.Add($"رقم الهاتف: {pay.Phone}");
        return string.Join("\n", parts);
    }

    public static string BuildWaLink(string phone, string message)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return $"https://wa.me/{digits}?text={Uri.EscapeDataString(message)}";
    }

    // ✅ جديد — رابط الدخول اليدوي
    public static string BuildLoginUrl(string baseUrl) =>
        $"{baseUrl.TrimEnd('/')}/Account/UnifiedLogin";
}