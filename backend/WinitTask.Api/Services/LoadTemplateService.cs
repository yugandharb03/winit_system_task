using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;
using WinitTask.Api.Exceptions;
using WinitTask.Api.Repositories;

namespace WinitTask.Api.Services;

public class LoadTemplateService : ILoadTemplateService
{
    private readonly ILoadTemplateRepository _templates;
    private readonly ISkuRepository _skus;

    public LoadTemplateService(ILoadTemplateRepository templates, ISkuRepository skus)
    {
        _templates = templates;
        _skus = skus;
    }

    public async Task<PagedResult<LoadTemplateListItemDto>> GetPagedAsync(
        LoadTemplateQueryParameters query, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _templates.GetPagedAsync(query, cancellationToken);

        return new PagedResult<LoadTemplateListItemDto>
        {
            Items = items.Select(t => new LoadTemplateListItemDto
            {
                Uid = t.UID,
                TemplateName = t.TemplateName,
                TemplateDescription = t.TemplateDescription,
                ItemCount = t.ItemCount
            }).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<LoadTemplateDetailDto> GetByUidAsync(string uid, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByUidAsync(uid, includeLines: true, cancellationToken)
            ?? throw NotFoundException.For("Load template", uid);

        return await MapToDetailAsync(template, cancellationToken);
    }

    public async Task<LoadTemplateDetailDto> CreateAsync(
        SaveLoadTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(request, cancellationToken);

        var templateUid = Guid.NewGuid().ToString();

        var template = new LoadTemplate
        {
            UID = templateUid,
            TemplateName = request.TemplateName.Trim(),
            TemplateDescription = string.IsNullOrWhiteSpace(request.TemplateDescription)
                ? null
                : request.TemplateDescription.Trim(),
            ItemCount = 0, // set from the lines below
            Lines = request.Lines.Select(line => new LoadTemplateLine
            {
                UID = Guid.NewGuid().ToString(),
                LoadTemplateUID = templateUid,
                SKUCode = line.SkuCode.Trim(),
                UOMCode = line.UomCode.Trim(),
                MonQty = line.MonQty,
                TueQty = line.TueQty,
                WedQty = line.WedQty,
                ThuQty = line.ThuQty,
                FriQty = line.FriQty,
                SatQty = line.SatQty,
                SunQty = line.SunQty
            }).ToList()
        };

        template.ItemCount = CalculateItemCount(template.Lines);

        await _templates.AddAsync(template, cancellationToken);
        await _templates.SaveChangesAsync(cancellationToken);

        return await MapToDetailAsync(template, cancellationToken);
    }

    public async Task<LoadTemplateDetailDto> UpdateAsync(
        string uid, SaveLoadTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByUidAsync(uid, includeLines: true, cancellationToken)
            ?? throw NotFoundException.For("Load template", uid);

        await ValidateAsync(request, cancellationToken);

        template.TemplateName = request.TemplateName.Trim();
        template.TemplateDescription = string.IsNullOrWhiteSpace(request.TemplateDescription)
            ? null
            : request.TemplateDescription.Trim();

        // Diff the lines by SKU code so untouched rows keep their Id and UID.
        var incoming = request.Lines.ToDictionary(l => l.SkuCode.Trim(), StringComparer.OrdinalIgnoreCase);
        var existing = template.Lines.ToList();

        var removed = existing.Where(l => !incoming.ContainsKey(l.SKUCode)).ToList();
        if (removed.Count > 0)
        {
            _templates.RemoveLines(removed);
            foreach (var line in removed)
            {
                template.Lines.Remove(line);
            }
        }

        foreach (var line in template.Lines)
        {
            var source = incoming[line.SKUCode];
            line.UOMCode = source.UomCode.Trim();
            line.MonQty = source.MonQty;
            line.TueQty = source.TueQty;
            line.WedQty = source.WedQty;
            line.ThuQty = source.ThuQty;
            line.FriQty = source.FriQty;
            line.SatQty = source.SatQty;
            line.SunQty = source.SunQty;
        }

        var existingCodes = existing.Select(l => l.SKUCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var (skuCode, source) in incoming.Where(kv => !existingCodes.Contains(kv.Key)))
        {
            template.Lines.Add(new LoadTemplateLine
            {
                UID = Guid.NewGuid().ToString(),
                LoadTemplateUID = template.UID,
                SKUCode = skuCode,
                UOMCode = source.UomCode.Trim(),
                MonQty = source.MonQty,
                TueQty = source.TueQty,
                WedQty = source.WedQty,
                ThuQty = source.ThuQty,
                FriQty = source.FriQty,
                SatQty = source.SatQty,
                SunQty = source.SunQty
            });
        }

        template.ItemCount = CalculateItemCount(template.Lines);

        await _templates.SaveChangesAsync(cancellationToken);

        return await MapToDetailAsync(template, cancellationToken);
    }

    public async Task DeleteAsync(string uid, CancellationToken cancellationToken = default)
    {
        var template = await _templates.GetByUidAsync(uid, includeLines: true, cancellationToken)
            ?? throw NotFoundException.For("Load template", uid);

        // Lines are removed explicitly as well as by the cascade, so the change is unambiguous.
        _templates.RemoveLines(template.Lines);
        _templates.Remove(template);

        await _templates.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// "No of Items" is the total load quantity of the template: every weekday quantity
    /// of every product line added together, not the number of product rows.
    /// The column is an int, so a fractional total is rounded to the nearest whole number.
    /// </summary>
    public static int CalculateItemCount(IEnumerable<LoadTemplateLine> lines)
    {
        var total = lines.Sum(line =>
            line.MonQty + line.TueQty + line.WedQty + line.ThuQty +
            line.FriQty + line.SatQty + line.SunQty);

        return (int)Math.Round(total, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Business rules beyond the data annotations: no duplicate products in one template,
    /// every SKU must exist, and the chosen UOM must be valid for that SKU.
    /// </summary>
    private async Task ValidateAsync(SaveLoadTemplateRequest request, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.TryGetValue(key, out var list))
            {
                errors[key] = list = new List<string>();
            }

            list.Add(message);
        }

        if (string.IsNullOrWhiteSpace(request.TemplateName))
        {
            AddError(nameof(request.TemplateName), "Template name is required.");
        }

        var codes = request.Lines.Select(l => l.SkuCode?.Trim() ?? string.Empty).ToList();

        var duplicates = codes
            .Where(c => !string.IsNullOrEmpty(c))
            .GroupBy(c => c, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            AddError(nameof(request.Lines),
                $"The same product cannot be added twice: {string.Join(", ", duplicates)}.");
        }

        if (codes.Any(string.IsNullOrEmpty))
        {
            AddError(nameof(request.Lines), "Every product line must have a SKU code.");
        }

        var realCodes = codes.Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        if (realCodes.Count > 0)
        {
            var known = (await _skus.GetExistingCodesAsync(realCodes, cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var unknown = realCodes.Where(c => !known.Contains(c)).ToList();
            if (unknown.Count > 0)
            {
                AddError(nameof(request.Lines), $"Unknown SKU code(s): {string.Join(", ", unknown)}.");
            }

            // UOM must be one of the SKU's own units of measure.
            var allowed = (await _skus.GetUomsAsync(realCodes, cancellationToken))
                .GroupBy(u => u.SKUCode, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(u => u.Code).ToHashSet(StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var line in request.Lines)
            {
                var code = line.SkuCode?.Trim() ?? string.Empty;
                var uom = line.UomCode?.Trim() ?? string.Empty;

                if (string.IsNullOrEmpty(code) || !known.Contains(code))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(uom))
                {
                    AddError(nameof(request.Lines), $"A UOM must be selected for SKU {code}.");
                }
                else if (!allowed.TryGetValue(code, out var uoms) || !uoms.Contains(uom))
                {
                    AddError(nameof(request.Lines), $"UOM '{uom}' is not valid for SKU {code}.");
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new BusinessValidationException(
                errors.ToDictionary(kv => kv.Key, kv => kv.Value.Distinct().ToArray()));
        }
    }

    /// <summary>
    /// Projects an entity to the detail DTO, attaching each line's SKU name and the
    /// full UOM list so the Edit grid can render its dropdowns without extra calls.
    /// </summary>
    private async Task<LoadTemplateDetailDto> MapToDetailAsync(
        LoadTemplate template, CancellationToken cancellationToken)
    {
        var codes = template.Lines.Select(l => l.SKUCode).Distinct().ToList();

        var names = await _skus.GetNamesByCodeAsync(codes, cancellationToken);
        var uomsBySku = (await _skus.GetUomsAsync(codes, cancellationToken))
            .GroupBy(u => u.SKUCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UomDto>)g.Select(u => new UomDto { Code = u.Code, Name = u.Name }).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var lines = template.Lines
            .OrderBy(l => l.Id)
            .Select(l =>
            {
                var available = uomsBySku.TryGetValue(l.SKUCode, out var list)
                    ? list
                    : Array.Empty<UomDto>();

                return new LoadTemplateLineDto
                {
                    Uid = l.UID,
                    SkuCode = l.SKUCode,
                    SkuName = names.TryGetValue(l.SKUCode, out var name) ? name : l.SKUCode,
                    UomCode = l.UOMCode,
                    UomName = available.FirstOrDefault(u =>
                        string.Equals(u.Code, l.UOMCode, StringComparison.OrdinalIgnoreCase))?.Name ?? l.UOMCode,
                    AvailableUoms = available,
                    MonQty = l.MonQty,
                    TueQty = l.TueQty,
                    WedQty = l.WedQty,
                    ThuQty = l.ThuQty,
                    FriQty = l.FriQty,
                    SatQty = l.SatQty,
                    SunQty = l.SunQty
                };
            })
            .ToList();

        return new LoadTemplateDetailDto
        {
            Uid = template.UID,
            TemplateName = template.TemplateName,
            TemplateDescription = template.TemplateDescription,
            ItemCount = template.ItemCount,
            Lines = lines
        };
    }
}
