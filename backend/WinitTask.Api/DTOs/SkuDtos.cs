namespace WinitTask.Api.DTOs;

/// <summary>One row of the "Add Products" popup.</summary>
public class SkuListItemDto
{
    public string Uid { get; init; } = null!;
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;

    /// <summary>UOMs available for this SKU, used to seed the dropdown of a newly added row.</summary>
    public IReadOnlyList<UomDto> Uoms { get; init; } = Array.Empty<UomDto>();
}

/// <summary>
/// A unit of measure. Per the task brief the dropdown shows <see cref="Name"/>,
/// while <see cref="Code"/> is what gets stored on LoadTemplateLine.UOMCode.
/// </summary>
public class UomDto
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
}
