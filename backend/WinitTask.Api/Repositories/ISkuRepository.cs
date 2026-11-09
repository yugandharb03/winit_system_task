using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Repositories;

public interface ISkuRepository
{
    /// <summary>Paged SKU search for the "Add Products" popup, honouring the exclude list.</summary>
    Task<(IReadOnlyList<SKU> Items, int TotalCount)> SearchAsync(
        SkuQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>All SKUUOM rows for the given SKU codes; pass none to get every row.</summary>
    Task<IReadOnlyList<SKUUOM>> GetUomsAsync(
        IEnumerable<string> skuCodes, CancellationToken cancellationToken = default);

    /// <summary>Returns the subset of <paramref name="codes"/> that actually exist in SKU.</summary>
    Task<IReadOnlyList<string>> GetExistingCodesAsync(
        IEnumerable<string> codes, CancellationToken cancellationToken = default);

    /// <summary>Code → Name lookup used when projecting template lines.</summary>
    Task<IReadOnlyDictionary<string, string>> GetNamesByCodeAsync(
        IEnumerable<string> codes, CancellationToken cancellationToken = default);
}
