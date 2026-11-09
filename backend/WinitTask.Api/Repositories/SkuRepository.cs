using Microsoft.EntityFrameworkCore;
using WinitTask.Api.Data;
using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Repositories;

public class SkuRepository : ISkuRepository
{
    private readonly AppDbContext _db;

    public SkuRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<SKU> Items, int TotalCount)> SearchAsync(
        SkuQueryParameters query, CancellationToken cancellationToken = default)
    {
        var skus = _db.SKUs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            skus = skus.Where(s => EF.Functions.Like(s.Code, $"%{search}%")
                                || EF.Functions.Like(s.Name, $"%{search}%"));
        }

        // Products already in the grid must not be offered again.
        var excluded = query.ExcludedCodeList;
        if (excluded.Length > 0)
        {
            skus = skus.Where(s => !excluded.Contains(s.Code));
        }

        var totalCount = await skus.CountAsync(cancellationToken);

        var items = await skus
            .OrderBy(s => s.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<SKUUOM>> GetUomsAsync(
        IEnumerable<string> skuCodes, CancellationToken cancellationToken = default)
    {
        var codes = skuCodes.Distinct().ToArray();

        var uoms = _db.SKUUOMs.AsNoTracking();
        if (codes.Length > 0)
        {
            uoms = uoms.Where(u => codes.Contains(u.SKUCode));
        }

        return await uoms.OrderBy(u => u.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetExistingCodesAsync(
        IEnumerable<string> codes, CancellationToken cancellationToken = default)
    {
        var wanted = codes.Distinct().ToArray();
        if (wanted.Length == 0)
        {
            return Array.Empty<string>();
        }

        return await _db.SKUs.AsNoTracking()
            .Where(s => wanted.Contains(s.Code))
            .Select(s => s.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetNamesByCodeAsync(
        IEnumerable<string> codes, CancellationToken cancellationToken = default)
    {
        var wanted = codes.Distinct().ToArray();
        if (wanted.Length == 0)
        {
            return new Dictionary<string, string>();
        }

        return await _db.SKUs.AsNoTracking()
            .Where(s => wanted.Contains(s.Code))
            .ToDictionaryAsync(s => s.Code, s => s.Name, cancellationToken);
    }
}
