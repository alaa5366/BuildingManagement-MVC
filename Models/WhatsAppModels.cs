namespace BuildingManagementMvc.Models;

public sealed class WhatsAppApartmentOption
{
    public string Id { get; set; } = "";
    public int Number { get; set; }
    public string Owner { get; set; } = "";
}

public sealed class WhatsAppIndexViewModel
{
    public string BuildingName { get; set; } = "";
    public List<WhatsAppApartmentOption> Apartments { get; set; } = new();
    public List<SavedWhatsAppTemplateOption> SavedTemplates { get; set; } = new();
}

public sealed class SavedWhatsAppTemplateOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Message { get; set; } = "";
}

public sealed class WhatsAppResultViewModel
{
    public string TemplateName { get; set; } = "";
    public int ApartmentNumber { get; set; }
    public string Message { get; set; } = "";
    public string Link { get; set; } = "";
}
