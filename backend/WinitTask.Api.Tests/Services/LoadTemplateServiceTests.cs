using Moq;
using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;
using WinitTask.Api.Exceptions;
using WinitTask.Api.Repositories;
using WinitTask.Api.Services;

namespace WinitTask.Api.Tests.Services;

public class LoadTemplateServiceTests
{
    private readonly Mock<ILoadTemplateRepository> _templates = new();
    private readonly Mock<ISkuRepository> _skus = new();
    private readonly Dictionary<string, string> _names = new(StringComparer.Ordinal);
    private readonly List<SKUUOM> _uoms = new();
    private readonly LoadTemplateService _service;
    private LoadTemplate? _added;

    public LoadTemplateServiceTests()
    {
        _skus
            .Setup(s => s.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> codes, CancellationToken _) =>
            {
                var found = codes
                    .Select(code => _names.Keys.FirstOrDefault(key =>
                        string.Equals(key, code, StringComparison.OrdinalIgnoreCase)))
                    .Where(key => key is not null)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Cast<string>()
                    .ToList();
                return Task.FromResult<IReadOnlyList<string>>(found);
            });

        _skus
            .Setup(s => s.GetUomsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> codes, CancellationToken _) =>
            {
                var requested = codes.ToList();
                IEnumerable<SKUUOM> match = requested.Count == 0
                    ? _uoms
                    : _uoms.Where(uom => requested.Contains(uom.SKUCode, StringComparer.OrdinalIgnoreCase));
                return Task.FromResult<IReadOnlyList<SKUUOM>>(match.ToList());
            });

        _skus
            .Setup(s => s.GetNamesByCodeAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Returns((IEnumerable<string> codes, CancellationToken _) =>
            {
                var names = new Dictionary<string, string>();
                foreach (var code in codes.Distinct())
                {
                    if (_names.TryGetValue(code, out var name))
                    {
                        names[code] = name;
                    }
                }

                return Task.FromResult<IReadOnlyDictionary<string, string>>(names);
            });

        _templates
            .Setup(r => r.AddAsync(It.IsAny<LoadTemplate>(), It.IsAny<CancellationToken>()))
            .Callback<LoadTemplate, CancellationToken>((template, _) => _added = template)
            .Returns(Task.CompletedTask);
        _templates
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        Register("MILK", "Fresh Milk");
        Register("BREAD", "Milk Bread");
        Register("JUICE", "Juice");

        _service = new LoadTemplateService(_templates.Object, _skus.Object);
    }

    [Fact]
    public async Task GetPaged_MapsRowsAndKeepsTheRequestedPage()
    {
        var query = new LoadTemplateQueryParameters
        {
            Name = "week",
            SortBy = "itemCount",
            SortDir = "desc",
            Page = 3,
            PageSize = 10
        };
        var rows = new List<LoadTemplate>
        {
            new()
            {
                UID = "tpl-1",
                TemplateName = "Weekly",
                TemplateDescription = "Standard",
                ItemCount = 4
            }
        };
        _templates
            .Setup(r => r.GetPagedAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<LoadTemplate>)rows, 21));

        var page = await _service.GetPagedAsync(query);

        var item = Assert.Single(page.Items);
        Assert.Equal("tpl-1", item.Uid);
        Assert.Equal("Weekly", item.TemplateName);
        Assert.Equal("Standard", item.TemplateDescription);
        Assert.Equal(4, item.ItemCount);
        Assert.Equal(21, page.TotalCount);
        Assert.Equal(3, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPrevious);
        Assert.False(page.HasNext);
    }

    [Fact]
    public async Task GetByUid_ReturnsLinesOrderedById_WithNamesAndUomChoices()
    {
        var template = Template("tpl-1", "Weekly", "Standard",
            LineEntity(2, "line-bread", "tpl-1", "BREAD", "CT", mon: 4),
            LineEntity(1, "line-milk", "tpl-1", "MILK", "EA", mon: 2, sun: 1));
        template.ItemCount = 2;
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var detail = await _service.GetByUidAsync("tpl-1");

        Assert.Equal("tpl-1", detail.Uid);
        Assert.Equal("Weekly", detail.TemplateName);
        Assert.Equal("Standard", detail.TemplateDescription);
        Assert.Equal(2, detail.ItemCount);
        Assert.Equal(new[] { "MILK", "BREAD" }, detail.Lines.Select(l => l.SkuCode).ToArray());

        var milk = detail.Lines[0];
        Assert.Equal("line-milk", milk.Uid);
        Assert.Equal("Fresh Milk", milk.SkuName);
        Assert.Equal("EA", milk.UomCode);
        Assert.Equal("Each", milk.UomName);
        Assert.Equal(2, milk.MonQty);
        Assert.Equal(1, milk.SunQty);
        Assert.Equal(new[] { "EA", "OU", "CT" }, milk.AvailableUoms.Select(u => u.Code).ToArray());
        Assert.Equal(new[] { "Each", "Outer", "Carton" }, milk.AvailableUoms.Select(u => u.Name).ToArray());
    }

    [Fact]
    public async Task GetByUid_WhenSkuOrUomMasterDataIsMissing_FallsBackToTheStoredCode()
    {
        var template = Template("tpl-1", "Weekly", null,
            LineEntity(1, "line-1", "tpl-1", "GHOST", "ZZ"));
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var detail = await _service.GetByUidAsync("tpl-1");

        var line = Assert.Single(detail.Lines);
        Assert.Equal("GHOST", line.SkuName);
        Assert.Equal("ZZ", line.UomName);
        Assert.Empty(line.AvailableUoms);
    }

    [Fact]
    public async Task GetByUid_WhenTheTemplateDoesNotExist_ThrowsNotFound()
    {
        _templates
            .Setup(r => r.GetByUidAsync("missing", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoadTemplate?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.GetByUidAsync("missing"));

        Assert.Equal("Load template 'missing' was not found.", ex.Message);
    }

    [Fact]
    public async Task Create_TrimsText_StoresQuantities_AndShowsTheUomName()
    {
        var request = Request(
            "  Weekly load  ",
            "  Monday route  ",
            Line(" MILK ", " ea ", mon: 1.5m, tue: 2, wed: 3, thu: 4, fri: 5, sat: 6, sun: 7));

        var detail = await _service.CreateAsync(request);

        Assert.NotNull(_added);
        Assert.Equal(_added.UID, detail.Uid);
        Assert.True(Guid.TryParse(_added.UID, out _));
        Assert.Equal("Weekly load", _added.TemplateName);
        Assert.Equal("Monday route", _added.TemplateDescription);
        Assert.Equal(1, _added.ItemCount);

        var stored = Assert.Single(_added.Lines);
        Assert.Equal(_added.UID, stored.LoadTemplateUID);
        Assert.True(Guid.TryParse(stored.UID, out _));
        Assert.Equal("MILK", stored.SKUCode);
        Assert.Equal("ea", stored.UOMCode);
        Assert.Equal(1.5m, stored.MonQty);
        Assert.Equal(2, stored.TueQty);
        Assert.Equal(3, stored.WedQty);
        Assert.Equal(4, stored.ThuQty);
        Assert.Equal(5, stored.FriQty);
        Assert.Equal(6, stored.SatQty);
        Assert.Equal(7, stored.SunQty);

        Assert.Equal("Fresh Milk", detail.Lines[0].SkuName);
        Assert.Equal("Each", detail.Lines[0].UomName);
        Assert.Equal("ea", detail.Lines[0].UomCode);
        _templates.Verify(r => r.AddAsync(_added, It.IsAny<CancellationToken>()), Times.Once);
        _templates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_BlankDescription_IsStoredAsNull_AndEmptyLinesAreAllowed()
    {
        var detail = await _service.CreateAsync(Request("  Empty template  ", "   "));

        Assert.NotNull(_added);
        Assert.Equal("Empty template", _added.TemplateName);
        Assert.Null(_added.TemplateDescription);
        Assert.Equal(0, _added.ItemCount);
        Assert.Empty(_added.Lines);
        Assert.Empty(detail.Lines);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WithoutATemplateName_IsRejectedAndNothingIsSaved(string? name)
    {
        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _service.CreateAsync(Request(name!, "desc", Line("MILK", "EA"))));

        Assert.Equal("Template name is required.", Assert.Single(ex.Errors["TemplateName"]));
        _templates.Verify(r => r.AddAsync(It.IsAny<LoadTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
        _templates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_RejectsTheSameProductTwice_IgnoringCase()
    {
        var request = Request("Weekly", "desc", Line("MILK", "EA"), Line("milk", "CT"));

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => _service.CreateAsync(request));

        Assert.Contains(ex.Errors["Lines"], message => message.Contains("cannot be added twice", StringComparison.Ordinal));
        _templates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_RejectsUnknownSkus_MissingUoms_InvalidUoms_AndBlankSkuCodesTogether()
    {
        var request = Request(
            "   ",
            "desc",
            Line("NOPE", "EA"),
            Line("BREAD", ""),
            Line("JUICE", "XX"),
            Line("  ", "EA"));

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() => _service.CreateAsync(request));

        Assert.Equal("Template name is required.", Assert.Single(ex.Errors["TemplateName"]));
        var lines = ex.Errors["Lines"];
        Assert.Contains(lines, message => message.Contains("Unknown SKU code(s): NOPE.", StringComparison.Ordinal));
        Assert.Contains(lines, message => message.Contains("A UOM must be selected for SKU BREAD.", StringComparison.Ordinal));
        Assert.Contains(lines, message => message.Contains("UOM 'XX' is not valid for SKU JUICE.", StringComparison.Ordinal));
        Assert.Contains(lines, message => message.Contains("Every product line must have a SKU code.", StringComparison.Ordinal));
        _templates.Verify(r => r.AddAsync(It.IsAny<LoadTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithOnlyBlankSkuCodes_DoesNotLookUpTheCatalogue()
    {
        var request = Request("Weekly", null, new SaveLoadTemplateLineRequest { SkuCode = null!, UomCode = "EA" });

        await Assert.ThrowsAsync<BusinessValidationException>(() => _service.CreateAsync(request));

        _skus.Verify(s => s.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _skus.Verify(s => s.GetUomsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_EditsAnExistingLineInPlace_AndKeepsItsIdentity()
    {
        var milk = LineEntity(7, "keep-me", "tpl-1", "MILK", "EA", mon: 1);
        var template = Template("tpl-1", "  old  ", "old desc", milk);
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var detail = await _service.UpdateAsync("tpl-1", Request(
            "  Weekly  ",
            "  revised  ",
            Line("milk", "ou", mon: 9, tue: 8, wed: 7, thu: 6, fri: 5, sat: 4, sun: 3)));

        Assert.Equal("Weekly", template.TemplateName);
        Assert.Equal("revised", template.TemplateDescription);
        Assert.Equal(1, template.ItemCount);
        Assert.Same(milk, Assert.Single(template.Lines));
        Assert.Equal("keep-me", milk.UID);
        Assert.Equal("MILK", milk.SKUCode);
        Assert.Equal("ou", milk.UOMCode);
        Assert.Equal(9, milk.MonQty);
        Assert.Equal(8, milk.TueQty);
        Assert.Equal(7, milk.WedQty);
        Assert.Equal(6, milk.ThuQty);
        Assert.Equal(5, milk.FriQty);
        Assert.Equal(4, milk.SatQty);
        Assert.Equal(3, milk.SunQty);
        Assert.Equal("Outer", detail.Lines[0].UomName);
        _templates.Verify(r => r.RemoveLines(It.IsAny<IEnumerable<LoadTemplateLine>>()), Times.Never);
        _templates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_RemovesProductsLeftOutOfTheRequest()
    {
        var milk = LineEntity(1, "line-milk", "tpl-1", "MILK", "EA");
        var bread = LineEntity(2, "line-bread", "tpl-1", "BREAD", "EA");
        var template = Template("tpl-1", "Weekly", "desc", milk, bread);
        IReadOnlyList<LoadTemplateLine>? removed = null;
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templates
            .Setup(r => r.RemoveLines(It.IsAny<IEnumerable<LoadTemplateLine>>()))
            .Callback<IEnumerable<LoadTemplateLine>>(lines => removed = lines.ToList());

        await _service.UpdateAsync("tpl-1", Request("Weekly", "desc", Line("MILK", "CT", mon: 2)));

        Assert.NotNull(removed);
        Assert.Same(bread, Assert.Single(removed));
        Assert.Same(milk, Assert.Single(template.Lines));
        Assert.Equal("CT", milk.UOMCode);
        Assert.Equal(2, milk.MonQty);
        Assert.Equal(1, template.ItemCount);
    }

    [Fact]
    public async Task Update_AddsAProductThatWasNotOnTheTemplate()
    {
        var milk = LineEntity(1, "line-milk", "tpl-1", "MILK", "EA");
        var template = Template("tpl-1", "Weekly", "desc", milk);
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        await _service.UpdateAsync("tpl-1", Request("Weekly", "desc", Line("MILK", "EA"), Line(" BREAD ", "CT", fri: 4)));

        Assert.Equal(2, template.Lines.Count);
        Assert.Equal(2, template.ItemCount);
        var added = template.Lines.Single(line => line.SKUCode == "BREAD");
        Assert.NotEqual(milk.UID, added.UID);
        Assert.True(Guid.TryParse(added.UID, out _));
        Assert.Equal("tpl-1", added.LoadTemplateUID);
        Assert.Equal("CT", added.UOMCode);
        Assert.Equal(4, added.FriQty);
        _templates.Verify(r => r.RemoveLines(It.IsAny<IEnumerable<LoadTemplateLine>>()), Times.Never);
    }

    [Fact]
    public async Task Update_CanClearEveryProduct_AndABlankDescription()
    {
        var milk = LineEntity(1, "line-milk", "tpl-1", "MILK", "EA");
        var template = Template("tpl-1", "Weekly", "desc", milk);
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var detail = await _service.UpdateAsync("tpl-1", Request("Weekly", "   "));

        Assert.Empty(template.Lines);
        Assert.Equal(0, template.ItemCount);
        Assert.Null(template.TemplateDescription);
        Assert.Empty(detail.Lines);
    }

    [Fact]
    public async Task Update_WhenValidationFails_DoesNotChangeTheStoredTemplate()
    {
        var milk = LineEntity(1, "line-milk", "tpl-1", "MILK", "EA", mon: 6);
        var template = Template("tpl-1", "Weekly", "desc", milk);
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _service.UpdateAsync("tpl-1", Request("Renamed", "desc", Line("MILK", "XX", mon: 99), Line("MILK", "EA"))));

        Assert.Contains(ex.Errors["Lines"], message => message.Contains("cannot be added twice", StringComparison.Ordinal));
        Assert.Equal("Weekly", template.TemplateName);
        Assert.Equal("EA", milk.UOMCode);
        Assert.Equal(6, milk.MonQty);
        _templates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _templates.Verify(r => r.RemoveLines(It.IsAny<IEnumerable<LoadTemplateLine>>()), Times.Never);
    }

    [Fact]
    public async Task Update_WhenTheTemplateDoesNotExist_ThrowsNotFoundAndDoesNotValidate()
    {
        _templates
            .Setup(r => r.GetByUidAsync("gone", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoadTemplate?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateAsync("gone", Request("Weekly", null, Line("NOPE", "XX"))));

        Assert.Equal("Load template 'gone' was not found.", ex.Message);
        _skus.Verify(s => s.GetExistingCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _templates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_RemovesLinesThenTheTemplate_ThenSaves()
    {
        var template = Template("tpl-1", "Weekly", "desc",
            LineEntity(1, "line-milk", "tpl-1", "MILK", "EA"),
            LineEntity(2, "line-bread", "tpl-1", "BREAD", "OU"));
        var operations = new List<string>();
        _templates
            .Setup(r => r.GetByUidAsync("tpl-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _templates
            .Setup(r => r.RemoveLines(It.IsAny<IEnumerable<LoadTemplateLine>>()))
            .Callback<IEnumerable<LoadTemplateLine>>(lines =>
                operations.Add("lines:" + string.Join(",", lines.Select(line => line.SKUCode))));
        _templates
            .Setup(r => r.Remove(It.IsAny<LoadTemplate>()))
            .Callback<LoadTemplate>(item => operations.Add("template:" + item.UID));
        _templates
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("save"))
            .ReturnsAsync(1);

        await _service.DeleteAsync("tpl-1");

        Assert.Equal(new[] { "lines:MILK,BREAD", "template:tpl-1", "save" }, operations);
    }

    [Fact]
    public async Task Delete_WhenTheTemplateDoesNotExist_ThrowsNotFound()
    {
        _templates
            .Setup(r => r.GetByUidAsync("gone", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoadTemplate?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("gone"));

        Assert.Equal("Load template 'gone' was not found.", ex.Message);
        _templates.Verify(r => r.Remove(It.IsAny<LoadTemplate>()), Times.Never);
        _templates.Verify(r => r.RemoveLines(It.IsAny<IEnumerable<LoadTemplateLine>>()), Times.Never);
        _templates.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void Register(string code, string name)
    {
        _names[code] = name;
        _uoms.Add(new SKUUOM { UID = code + "-EA", SKUCode = code, Code = "EA", Name = "Each" });
        _uoms.Add(new SKUUOM { UID = code + "-OU", SKUCode = code, Code = "OU", Name = "Outer" });
        _uoms.Add(new SKUUOM { UID = code + "-CT", SKUCode = code, Code = "CT", Name = "Carton" });
    }

    private static LoadTemplate Template(string uid, string name, string? description, params LoadTemplateLine[] lines)
    {
        return new LoadTemplate
        {
            UID = uid,
            TemplateName = name,
            TemplateDescription = description,
            ItemCount = lines.Length,
            Lines = lines.ToList()
        };
    }

    private static LoadTemplateLine LineEntity(
        int id, string uid, string templateUid, string sku, string uom,
        decimal mon = 0, decimal tue = 0, decimal wed = 0, decimal thu = 0,
        decimal fri = 0, decimal sat = 0, decimal sun = 0)
    {
        return new LoadTemplateLine
        {
            Id = id,
            UID = uid,
            LoadTemplateUID = templateUid,
            SKUCode = sku,
            UOMCode = uom,
            MonQty = mon,
            TueQty = tue,
            WedQty = wed,
            ThuQty = thu,
            FriQty = fri,
            SatQty = sat,
            SunQty = sun
        };
    }

    private static SaveLoadTemplateRequest Request(
        string name, string? description, params SaveLoadTemplateLineRequest[] lines)
    {
        return new SaveLoadTemplateRequest
        {
            TemplateName = name,
            TemplateDescription = description,
            Lines = lines.ToList()
        };
    }

    private static SaveLoadTemplateLineRequest Line(
        string sku, string uom,
        decimal mon = 0, decimal tue = 0, decimal wed = 0, decimal thu = 0,
        decimal fri = 0, decimal sat = 0, decimal sun = 0)
    {
        return new SaveLoadTemplateLineRequest
        {
            SkuCode = sku,
            UomCode = uom,
            MonQty = mon,
            TueQty = tue,
            WedQty = wed,
            ThuQty = thu,
            FriQty = fri,
            SatQty = sat,
            SunQty = sun
        };
    }
}
