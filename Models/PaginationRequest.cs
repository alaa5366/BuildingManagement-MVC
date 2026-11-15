namespace BuildingManagementMvc.Models;

// طلب الـ Pagination — بيتقرا من الـ Query String
/// <summary>
/// Paging, sorting and search parameters read from the query string.
/// </summary>
public class PaginationRequest
{
    /// <summary>
    /// Gets or sets the page.
    /// </summary>
    public int Page { get; set; } = 1;
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 50;
    /// <summary>
    /// Gets or sets the sort by.
    /// </summary>
    public string? SortBy { get; set; }
    /// <summary>
    /// Gets or sets the sort dir.
    /// </summary>
    public string? SortDir { get; set; } = "desc";
    /// <summary>
    /// Gets or sets the search.
    /// </summary>
    public string? Search { get; set; }

    // ✅ حدود منطقية
    /// <summary>
    /// Normalizes the operation on the pagination request.
    /// </summary>
    public void Normalize()
    {
        if (Page < 1) Page = 1;
        if (PageSize < 10) PageSize = 10;
        if (PageSize > 500) PageSize = 500;
        if (string.IsNullOrWhiteSpace(SortDir)) SortDir = "desc";
        SortDir = SortDir.ToLower();
        if (SortDir != "asc" && SortDir != "desc") SortDir = "desc";
    }
}