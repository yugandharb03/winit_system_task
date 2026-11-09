using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Repositories;

public interface ILoadTemplateRepository
{
    /// <summary>Server-side filter + sort + page over LoadTemplate.</summary>
    Task<(IReadOnlyList<LoadTemplate> Items, int TotalCount)> GetPagedAsync(
        LoadTemplateQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>Loads one template; <paramref name="includeLines"/> also loads its lines.</summary>
    Task<LoadTemplate?> GetByUidAsync(string uid, bool includeLines, CancellationToken cancellationToken = default);

    Task AddAsync(LoadTemplate template, CancellationToken cancellationToken = default);

    void Remove(LoadTemplate template);

    void RemoveLines(IEnumerable<LoadTemplateLine> lines);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
