namespace BuildingManagementMvc.Models;

// نتيجة paginated
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalItems { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

    public bool HasPrevious => CurrentPage > 1;
    public bool HasNext => CurrentPage < TotalPages;

    public int FirstItemIndex => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int LastItemIndex => Math.Min(CurrentPage * PageSize, TotalItems);

    // ✅ Helper: بناء PagedResult من أي قائمة
    public static PagedResult<T> Create(IEnumerable<T> source, int page, int pageSize)
    {
        var list = source.ToList();
        var total = list.Count;

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;

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