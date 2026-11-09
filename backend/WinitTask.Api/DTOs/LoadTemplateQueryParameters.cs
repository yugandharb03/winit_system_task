namespace WinitTask.Api.DTOs;

/// <summary>Query string of GET /api/load-templates — search, sort and paging.</summary>
public class LoadTemplateQueryParameters
{
    private const int MaxPageSize = 200;
    private int _pageSize = 10;
    private int _page = 1;

    /// <summary>Filters on TemplateName (contains). Null or blank means "All".</summary>
    public string? Name { get; set; }

    /// <summary>templateName | templateDescription | itemCount. Anything else falls back to insertion order.</summary>
    public string? SortBy { get; set; }

    /// <summary>asc (default) or desc.</summary>
    public string? SortDir { get; set; }

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 10,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}

/// <summary>Query string of GET /api/skus — the "Add Products" popup.</summary>
public class SkuQueryParameters
{
    private const int MaxPageSize = 500;
    private int _pageSize = 25;
    private int _page = 1;

    /// <summary>Matches SKU Code or Name (contains). Driven by key press in the popup.</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Comma-separated SKU codes already in the grid. They are filtered out so the user
    /// cannot add a duplicate, per the task brief.
    /// </summary>
    public string? ExcludeCodes { get; set; }

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 25,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }

    public string[] ExcludedCodeList =>
        string.IsNullOrWhiteSpace(ExcludeCodes)
            ? Array.Empty<string>()
            : ExcludeCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
