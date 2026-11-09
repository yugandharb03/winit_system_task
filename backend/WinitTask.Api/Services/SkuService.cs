using WinitTask.Api.DTOs;
using WinitTask.Api.Repositories;

namespace WinitTask.Api.Services;

public class SkuService : ISkuService
{
    private readonly ISkuRepository _skus;

    public SkuService(ISkuRepository skus)
    {
        _skus = skus;
    }

    public async Task<PagedResult<SkuListItemDto>> SearchAsync(
        SkuQueryParameters query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _skus.SearchAsync(query, cancellationToken);

        var codes = items.Select(s => s.Code).ToList();
        var uomsBySku = await GetUomsBySkuAsync(codes, cancellationToken);

        return new PagedResult<SkuListItemDto>
        {
            Items = items.Select(s => new SkuListItemDto
            {
                Uid = s.UID,
                Code = s.Code,
                Name = s.Name,
                Uoms = uomsBySku.TryGetValue(s.Code, out var uoms) ? uoms : Array.Empty<UomDto>()
            }).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<IReadOnlyList<UomDto>> GetUomCatalogueAsync(CancellationToken cancellationToken = default)
    {
        var uoms = await _skus.GetUomsAsync(Array.Empty<string>(), cancellationToken);

        return uoms
            .GroupBy(u => u.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => new UomDto { Code = g.Key, Name = g.First().Name })
            .OrderBy(u => u.Code)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<UomDto>>> GetUomsBySkuAsync(
        IEnumerable<string> skuCodes, CancellationToken cancellationToken = default)
    {
        var codes = skuCodes.Distinct().ToList();
        if (codes.Count == 0)
        {
            return new Dictionary<string, IReadOnlyList<UomDto>>();
        }

        var uoms = await _skus.GetUomsAsync(codes, cancellationToken);

        return uoms
            .GroupBy(u => u.SKUCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UomDto>)g.Select(u => new UomDto { Code = u.Code, Name = u.Name }).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }
}
