using WinitTask.Api.DTOs;

namespace WinitTask.Api.Services;

public interface ISkuService
{
    /// <summary>Paged SKU search for the "Add Products" popup.</summary>
    Task<PagedResult<SkuListItemDto>> SearchAsync(
        SkuQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>Distinct UOM catalogue (EA / OU / CT) for generic dropdowns.</summary>
    Task<IReadOnlyList<UomDto>> GetUomCatalogueAsync(CancellationToken cancellationToken = default);

    /// <summary>UOMs available per SKU, keyed by SKU code.</summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<UomDto>>> GetUomsBySkuAsync(
        IEnumerable<string> skuCodes, CancellationToken cancellationToken = default);
}
