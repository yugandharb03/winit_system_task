using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WinitTask.Api.Controllers;
using WinitTask.Api.DTOs;
using WinitTask.Api.Services;

namespace WinitTask.Api.Tests.Controllers;

public class SkusControllerTests
{
    private readonly Mock<ISkuService> _service = new();
    private readonly SkusController _controller;

    public SkusControllerTests()
    {
        _controller = new SkusController(_service.Object);
    }

    [Fact]
    public async Task Search_Returns200_WithThePopupPage()
    {
        var query = new SkuQueryParameters
        {
            Search = "mil",
            ExcludeCodes = "MILK",
            Page = 1,
            PageSize = 25
        };
        var expected = new PagedResult<SkuListItemDto>
        {
            Items = new[]
            {
                new SkuListItemDto
                {
                    Uid = "sku-1",
                    Code = "BRD",
                    Name = "Milk Bread",
                    Uoms = new[] { new UomDto { Code = "EA", Name = "Each" } }
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 25
        };
        using var cts = new CancellationTokenSource();
        _service.Setup(s => s.SearchAsync(query, cts.Token)).ReturnsAsync(expected);

        var result = await _controller.Search(query, cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Same(expected, ok.Value);
        _service.Verify(s => s.SearchAsync(query, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetUoms_Returns200_WithTheCatalogue()
    {
        IReadOnlyList<UomDto> catalogue = new[]
        {
            new UomDto { Code = "CT", Name = "Carton" },
            new UomDto { Code = "EA", Name = "Each" },
            new UomDto { Code = "OU", Name = "Outer" }
        };
        using var cts = new CancellationTokenSource();
        _service.Setup(s => s.GetUomCatalogueAsync(cts.Token)).ReturnsAsync(catalogue);

        var result = await _controller.GetUoms(cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Same(catalogue, ok.Value);
    }

    [Fact]
    public async Task GetUomsBySku_SplitsTrimsAndDropsEmptyCodes()
    {
        IReadOnlyList<string>? captured = null;
        var expected = new Dictionary<string, IReadOnlyList<UomDto>>
        {
            ["MILK"] = new[] { new UomDto { Code = "EA", Name = "Each" } }
        };
        using var cts = new CancellationTokenSource();
        _service
            .Setup(s => s.GetUomsBySkuAsync(It.IsAny<IEnumerable<string>>(), cts.Token))
            .Callback<IEnumerable<string>, CancellationToken>((codes, _) => captured = codes.ToList())
            .ReturnsAsync(expected);

        var result = await _controller.GetUomsBySku(" MILK, BRD ,, JUICE ", cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Same(expected, ok.Value);
        Assert.Equal(new[] { "MILK", "BRD", "JUICE" }, captured);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(",,,")]
    public async Task GetUomsBySku_WhenNoCodesAreSupplied_AsksForAnEmptyList(string? skuCodes)
    {
        IReadOnlyList<string>? captured = null;
        _service
            .Setup(s => s.GetUomsBySkuAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<string>, CancellationToken>((codes, _) => captured = codes.ToList())
            .ReturnsAsync(new Dictionary<string, IReadOnlyList<UomDto>>());

        var result = await _controller.GetUomsBySku(skuCodes!, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(captured);
        Assert.Empty(captured);
    }
}
