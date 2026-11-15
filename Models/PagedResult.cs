namespace BuildingManagementMvc.Models;

// نتيجة paginated
/// <summary>
/// One page of items together with paging information (total items, page size, page number).
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// Gets or sets the items.
    /// </summary>
    public List<T> Items { get; set; } = new();
    /// <summary>
    /// Gets or sets the current page.
    /// </summary>
    public int CurrentPage { get; set; } = 1;
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; } = 50;
    /// <summary>
    /// Gets or sets the total items.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets the total pages.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    /// <summary>
    /// Gets a value indicating whether it has previous.
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;
    /// <summary>
    /// Gets a value indicating whether it has next.
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;

    /// <summary>
    /// Gets the first item index.
    /// </summary>
    public int FirstItemIndex => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    /// <summary>
    /// Gets the last item index.
    /// </summary>
    public int LastItemIndex => Math.Min(CurrentPage * PageSize, TotalItems);

    // ✅ Helper: بناء PagedResult من أي قائمة
    /// <summary>
    /// Creates the operation on the paged result.
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        var list = source.ToList();
        var total = list.Count;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;

        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = items,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    // ✅ Helper: بدون تقسيم (لو عايز تعرض نفس القائمة)
    /// <summary>
    /// Returns the single.
    /// </summary>
    public static PagedResult<T> Single(IEnumerable<T> source, int pageSize = 50)
    {
        var list = source.ToList();
        return new PagedResult<T>
        {
            Items = list,
            CurrentPage = 1,
            PageSize = pageSize,
            TotalItems = list.Count
        };
    }
}