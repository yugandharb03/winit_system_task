using Moq;
using WinitTask.Api.DTOs;
using WinitTask.Api.Entities;
using WinitTask.Api.Repositories;
using WinitTask.Api.Services;

namespace WinitTask.Api.Tests.Services;

public class SkuServiceTests
{
    private readonly Mock<ISkuRepository> _skus = new();
    private readonly SkuService _service;

    public SkuServiceTests()
    {
        _service = new SkuService(_skus.Object);
    }

    [Fact]
    public async Task Search_AttachesEachSkusUoms_AndLeavesUnknownUomsEmpty()
    {
        var query = new SkuQueryParameters { Search = "mil", ExcludeCodes = "JUICE", Page = 2, PageSize = 5 };
        var milk = new SKU { UID = "sku-milk", Code = "MILK", Name = "Fresh Milk" };
        var bread = new SKU { UID = "sku-bread", Code = "BREAD", Name = "Milk Bread" };
        _skus
            .Setup(s => s.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<SKU>)new List<SKU> { milk, bread }, 11));
        _skus
            .Setup(s => s.GetUomsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SKUUOM>
            {
                new() { SKUCode = "MILK", Code = "EA", Name = "Each" },
                new() { SKUCode = "MILK", Code = "CT", Name = "Carton" }
            });

        var page = await _service.SearchAsync(query);

        Assert.Equal(11, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(5, page.PageSize);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);

        var first = page.Items[0];
        Assert.Equal("sku-milk", first.Uid);
        Assert.Equal("MILK", first.Code);
        Assert.Equal("Fresh Milk", first.Name);
        Assert.Equal(new[] { "EA", "CT" }, first.Uoms.Select(uom => uom.Code).ToArray());
        Assert.Equal(new[] { "Each", "Carton" }, first.Uoms.Select(uom => uom.Name).ToArray());

        var second = page.Items[1];
        Assert.Equal("BREAD", second.Code);
        Assert.Empty(second.Uoms);
    }

    [Fact]
    public async Task GetUomCatalogue_ReturnsDistinctCodes_Ordered_KeepingTheFirstName()
    {
        _skus
            .Setup(s => s.GetUomsAsync(It.Is<IEnumerable<string>>(codes => !codes.Any()), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SKUUOM>
            {
                new() { SKUCode = "MILK", Code = "EA", Name = "Each" },
                new() { SKUCode = "BREAD", Code = "ea", Name = "each-duplicate" },
                new() { SKUCode = "MILK", Code = "OU", Name = "Outer" },
                new() { SKUCode = "JUICE", Code = "CT", Name = "Carton" }
            });

        var catalogue = await _service.GetUomCatalogueAsync();

        Assert.Equal(new[] { "CT", "EA", "OU" }, catalogue.Select(uom => uom.Code).ToArray());
        Assert.Equal(new[] { "Carton", "Each", "Outer" }, catalogue.Select(uom => uom.Name).ToArray());
    }

    [Fact]
    public async Task GetUomsBySku_GroupsChoicesBySku_AndIgnoresRepeatedCodes()
    {
        IReadOnlyList<string>? requested = null;
        _skus
            .Setup(s => s.GetUomsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, CancellationToken>((codes, _) => requested = codes.ToList())
            .ReturnsAsync(new List<SKUUOM>
            {
                new() { SKUCode = "MILK", Code = "EA", Name = "Each" },
                new() { SKUCode = "MILK", Code = "OU", Name = "Outer" },
                new() { SKUCode = "BREAD", Code = "CT", Name = "Carton" }
            });

        var result = await _service.GetUomsBySkuAsync(new[] { "MILK", "MILK", "BREAD" });

        Assert.Equal(new[] { "MILK", "BREAD" }, requested);
        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { "EA", "OU" }, result["milk"].Select(uom => uom.Code).ToArray());
        Assert.Equal("Carton", Assert.Single(result["BREAD"]).Name);
    }

    [Fact]
    public async Task GetUomsBySku_WhenNoCodesAreGiven_DoesNotQuery()
    {
        var result = await _service.GetUomsBySkuAsync(Array.Empty<string>());

        Assert.Empty(result);
        _skus.Verify(s => s.GetUomsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
