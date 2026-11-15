namespace BuildingManagementMvc.Models;

/// <summary>
/// A stored receipt image with its OCR guesses.
/// </summary>
public class Receipt
{
    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    public string Url { get; set; } = "";
    /// <summary>
    /// Gets or sets the identifier of the public.
    /// </summary>
    public string PublicId { get; set; } = "";
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = "";
    /// <summary>
    /// Gets or sets the uploaded date and time.
    /// </summary>
    public string UploadedAt { get; set; } = "";
    /// <summary>
    /// Gets or sets the OCR text.
    /// </summary>
    public string? OcrText { get; set; }
    /// <summary>
    /// Gets or sets the OCR amount guess.
    /// </summary>
    public decimal? OcrAmountGuess { get; set; }
}