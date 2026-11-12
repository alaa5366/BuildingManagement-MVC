namespace BuildingManagementMvc.Models;

public class Receipt
{
    public string Url { get; set; } = "";
    public string PublicId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string UploadedAt { get; set; } = "";
    public string? OcrText { get; set; }
    public double? OcrAmountGuess { get; set; }
}