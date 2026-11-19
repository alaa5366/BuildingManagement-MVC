using BuildingManagementMvc.Data;
using BuildingManagementMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagementMvc.Services;

/// <summary>
/// Generates WhatsApp drafts from canonical Target Schema building and apartment data.
/// This workflow opens a wa.me draft; it does not send messages or expose API credentials.
/// </summary>
public sealed class TargetWhatsAppService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public TargetWhatsAppService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<WhatsAppIndexViewModel?> GetIndexAsync(string buildingId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var building = await db.Buildings.AsNoTracking()
            .Where(x => x.Id == buildingId).Select(x => new { x.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (building is null) return null;

        var apartments = await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && !x.IsClosed && !x.IsDisabled)
            .OrderBy(x => x.Number)
            .Select(x => new WhatsAppApartmentOption { Id = x.Id, Number = x.Number, Owner = x.Owner })
            .ToListAsync(cancellationToken);

        return new WhatsAppIndexViewModel { BuildingName = building.Name, Apartments = apartments };
    }

    public async Task<WhatsAppResultViewModel?> GenerateAsync(
        string buildingId, string apartmentId, string templateId,
        IReadOnlyDictionary<string, string> extra, string baseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!WhatsAppTemplateService.Templates.TryGetValue(templateId, out var template))
            throw new ArgumentException("قالب رسالة WhatsApp غير صالح.", nameof(templateId));

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var enabled = await db.GlobalSettings.AsNoTracking()
            .Where(x => x.Id == "global").Select(x => (bool?)x.EnableWhatsApp)
            .FirstOrDefaultAsync(cancellationToken);
        if (enabled == false)
            throw new InvalidOperationException("خدمة WhatsApp معطلة من إعدادات النظام.");

        var building = await db.Buildings.AsNoTracking()
            .Where(x => x.Id == buildingId)
            .Select(x => new { x.Name, x.PaymentLabel, x.PaymentAccountNumber, x.PaymentPhone, x.PaymentNotes })
            .FirstOrDefaultAsync(cancellationToken);
        if (building is null) return null;

        var apartment = await db.Apartments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.Id == apartmentId && !x.IsClosed && !x.IsDisabled)
            .Select(x => new { x.Number, x.Owner, x.Phone, x.FloorId })
            .FirstOrDefaultAsync(cancellationToken);
        if (apartment is null) return null;

        var floor = await db.Floors.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.Id == apartment.FloorId)
            .Select(x => x.Label).FirstOrDefaultAsync(cancellationToken);

        var paymentParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(building.PaymentLabel)) paymentParts.Add($"الطريقة: {building.PaymentLabel}");
        if (!string.IsNullOrWhiteSpace(building.PaymentAccountNumber)) paymentParts.Add($"رقم الحساب: {building.PaymentAccountNumber}");
        if (!string.IsNullOrWhiteSpace(building.PaymentPhone)) paymentParts.Add($"رقم الهاتف: {building.PaymentPhone}");
        if (!string.IsNullOrWhiteSpace(building.PaymentNotes)) paymentParts.Add(building.PaymentNotes);

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["owner"] = string.IsNullOrWhiteSpace(apartment.Owner) ? $"ساكن شقة {apartment.Number}" : apartment.Owner,
            ["building"] = building.Name,
            ["apt"] = apartment.Number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["month"] = ReportsService.MonthLabel(WalletService.CurrentMonthKey()),
            ["payment_info"] = string.Join("\n", paymentParts),
            ["floor"] = floor ?? "—",
            ["phone"] = apartment.Phone ?? "",
            ["setpin_link"] = "",
            ["quick_link"] = "",
            ["login_url"] = WhatsAppTemplateService.BuildLoginUrl(baseUrl)
        };
        foreach (var pair in extra)
            if (values.ContainsKey(pair.Key)) values[pair.Key] = pair.Value ?? "";

        var message = template.Text;
        foreach (var pair in values)
            message = message.Replace("{" + pair.Key + "}", pair.Value, StringComparison.Ordinal);

        var digits = new string((apartment.Phone ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
            throw new InvalidOperationException("رقم هاتف الشقة غير صالح لإنشاء رابط WhatsApp.");

        return new WhatsAppResultViewModel
        {
            ApartmentNumber = apartment.Number,
            Message = message,
            Link = WhatsAppTemplateService.BuildWaLink(digits, message)
        };
    }
}
