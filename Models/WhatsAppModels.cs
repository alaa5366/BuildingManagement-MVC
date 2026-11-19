namespace BuildingManagementMvc.Models;

    /// <summary>Represents an apartment option available for WhatsApp messaging.</summary>
public sealed class WhatsAppApartmentOption
{
    public string Id { get; set; } = "";
    public int Number { get; set; }
    public string Owner { get; set; } = "";
}

    /// <summary>Provides the building, apartment, and saved-template data for the WhatsApp page.</summary>
public sealed class WhatsAppIndexViewModel
{
    public string BuildingName { get; set; } = "";
    public List<WhatsAppApartmentOption> Apartments { get; set; } = new();
    public List<SavedWhatsAppTemplateOption> SavedTemplates { get; set; } = new();
}

    /// <summary>Represents a saved WhatsApp message template.</summary>
public sealed class SavedWhatsAppTemplateOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Message { get; set; } = "";
}

    /// <summary>Contains the generated WhatsApp message and destination link.</summary>
public sealed class WhatsAppResultViewModel
{
    public string TemplateName { get; set; } = "";
    public int ApartmentNumber { get; set; }
    public string Message { get; set; } = "";
    public string Link { get; set; } = "";
}
