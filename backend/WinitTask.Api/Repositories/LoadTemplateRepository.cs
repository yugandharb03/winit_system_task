using Microsoft.EntityFrameworkCore;
using WinitTask.Api.Data;
using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;

namespace WinitTask.Api.Repositories;

public class LoadTemplateRepository : ILoadTemplateRepository
{
    private readonly AppDbContext _db;

    public LoadTemplateRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<LoadTemplate> Items, int TotalCount)> GetPagedAsync(
        LoadTemplateQueryParameters query, CancellationToken cancellationToken = default)
    {
        var templates = _db.LoadTemplates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim();
            templates = templates.Where(t => EF.Functions.Like(t.TemplateName, $"%{name}%"));
        }

        // Count before paging so the pager knows the real total.
        var totalCount = await templates.CountAsync(cancellationToken);

        templates = ApplySort(templates, query.SortBy, query.SortDir);

        var items = await templates
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Whitelisted sorting — an unknown column falls back to insertion order rather than
    /// letting arbitrary input reach the query.
    /// </summary>
    private static IQueryable<LoadTemplate> ApplySort(IQueryable<LoadTemplate> source, string? sortBy, string? sortDir)
    {
        var descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLowerInvariant()) switch
        {
            "templatename" => descending
                ? source.OrderByDescending(t => t.TemplateName).ThenBy(t => t.Id)
                : source.OrderBy(t => t.TemplateName).ThenBy(t => t.Id),

            "templatedescription" => descending
                ? source.OrderByDescending(t => t.TemplateDescription).ThenBy(t => t.Id)
                : source.OrderBy(t => t.TemplateDescription).ThenBy(t => t.Id),

            "itemcount" => descending
                ? source.OrderByDescending(t => t.ItemCount).ThenBy(t => t.Id)
                : source.OrderBy(t => t.ItemCount).ThenBy(t => t.Id),

            _ => descending ? source.OrderByDescending(t => t.Id) : source.OrderBy(t => t.Id)
        };
    }

    public async Task<LoadTemplate?> GetByUidAsync(string uid, bool includeLines,
        CancellationToken cancellationToken = default)
    {
        var templates = _db.LoadTemplates.AsQueryable();

        if (includeLines)
        {
            templates = templates.Include(t => t.Lines);
        }

        return await templates.FirstOrDefaultAsync(t => t.UID == uid, cancellationToken);
    }

    public async Task AddAsync(LoadTemplate template, CancellationToken cancellationToken = default) =>
        await _db.LoadTemplates.AddAsync(template, cancellationToken);

    public void Remove(LoadTemplate template) => _db.LoadTemplates.Remove(template);

    public void RemoveLines(IEnumerable<LoadTemplateLine> lines) => _db.LoadTemplateLines.RemoveRange(lines);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
