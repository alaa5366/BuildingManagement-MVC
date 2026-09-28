namespace BuildingManagementMvc.Models;

// طلب الـ Pagination — بيتقرا من الـ Query String
public class PaginationRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; }
    public string? SortDir { get; set; } = "desc";
    public string? Search { get; set; }

    // ✅ حدود منطقية
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