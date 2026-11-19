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

        var savedTemplates = await db.SavedWhatsAppTemplates.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SavedWhatsAppTemplateOption { Id = x.Id, Name = x.Name, Message = x.Message })
            .ToListAsync(cancellationToken);

        return new WhatsAppIndexViewModel
        {
            BuildingName = building.Name,
            Apartments = apartments,
            SavedTemplates = savedTemplates
        };
    }


    public async Task<Guid> SaveTemplateAsync(
        string buildingId, string name, string message, string? createdByUid,
        Guid? templateId = null, CancellationToken cancellationToken = default)
    {
        name = (name ?? "").Trim();
        message = (message ?? "").Trim();
        if (name.Length is < 2 or > 100)
            throw new ArgumentException("اسم القالب يجب أن يكون بين حرفين و100 حرف.");
        if (message.Length is < 2 or > 4000)
            throw new ArgumentException("نص الرسالة يجب أن يكون بين حرفين و4000 حرف.");

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        SavedWhatsAppTemplateEntity? entity = null;
        if (templateId.HasValue)
        {
            entity = await db.SavedWhatsAppTemplates
                .SingleOrDefaultAsync(x => x.Id == templateId.Value && x.BuildingId == buildingId, cancellationToken);
            if (entity is null) throw new InvalidOperationException("القالب المطلوب غير موجود.");
        }
        else
        {
            entity = new SavedWhatsAppTemplateEntity
            {
                Id = Guid.NewGuid(),
                BuildingId = buildingId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUid = createdByUid
            };
            db.SavedWhatsAppTemplates.Add(entity);
        }

        entity.Name = name;
        entity.Message = message;
        entity.IsActive = true;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> DeleteTemplateAsync(
        string buildingId, Guid templateId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.SavedWhatsAppTemplates
            .SingleOrDefaultAsync(x => x.Id == templateId && x.BuildingId == buildingId, cancellationToken);
        if (entity is null) return false;
        entity.IsActive = false;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WhatsAppResultViewModel?> GenerateAsync(
        string buildingId, string apartmentId, string templateId,
        IReadOnlyDictionary<string, string> extra, string baseUrl,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        string templateName;
        string templateText;
        if (templateId.StartsWith("saved:", StringComparison.Ordinal))
        {
            if (!Guid.TryParse(templateId[6..], out var savedId))
                throw new ArgumentException("قالب الرسالة المحفوظ غير صالح.", nameof(templateId));
            var saved = await db.SavedWhatsAppTemplates.AsNoTracking()
                .Where(x => x.Id == savedId && x.BuildingId == buildingId && x.IsActive)
                .Select(x => new { x.Name, x.Message })
                .FirstOrDefaultAsync(cancellationToken);
            if (saved is null) throw new ArgumentException("قالب الرسالة غير موجود أو غير متاح.", nameof(templateId));
            templateName = saved.Name;
            templateText = saved.Message;
        }
        else if (WhatsAppTemplateService.Templates.TryGetValue(templateId, out var builtIn))
        {
            templateName = builtIn.Name;
            templateText = builtIn.Text;
        }
        else
        {
            throw new ArgumentException("قالب رسالة WhatsApp غير صالح.", nameof(templateId));
        }
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
            ["login_url"] = WhatsAppTemplateService.BuildLoginUrl(baseUrl),
            ["amount"] = "",
            ["note"] = "",
            ["share"] = "",
            ["message"] = ""
        };
        foreach (var pair in extra)
            if (values.ContainsKey(pair.Key)) values[pair.Key] = pair.Value ?? "";

        var message = templateText;
        foreach (var pair in values)
            message = message.Replace("{" + pair.Key + "}", pair.Value, StringComparison.Ordinal);

        var digits = new string((apartment.Phone ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
            throw new InvalidOperationException("رقم هاتف الشقة غير صالح لإنشاء رابط WhatsApp.");

        return new WhatsAppResultViewModel
        {
            TemplateName = templateName,
            ApartmentNumber = apartment.Number,
            Message = message,
            Link = WhatsAppTemplateService.BuildWaLink(digits, message)
        };
    }
}
