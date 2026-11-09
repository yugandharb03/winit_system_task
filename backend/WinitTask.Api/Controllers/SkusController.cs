using Microsoft.AspNetCore.Mvc;
using WinitTask.Api.DTOs;
using WinitTask.Api.Services;

namespace WinitTask.Api.Controllers;

/// <summary>Product master lookups used by the "Add Products" popup and the UOM dropdowns.</summary>
[ApiController]
[Route("api/skus")]
[Produces("application/json")]
public class SkusController : ControllerBase
{
    private readonly ISkuService _service;

    public SkusController(ISkuService service)
    {
        _service = service;
    }

    /// <summary>
    /// Searches products by code or name. Pass <c>excludeCodes</c> with the SKUs already in
    /// the grid so they are not offered again.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SkuListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SkuListItemDto>>> Search(
        [FromQuery] SkuQueryParameters query, CancellationToken cancellationToken)
    {
        var result = await _service.SearchAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>The distinct UOM catalogue — EA (Each), OU (Outer), CT (Carton).</summary>
    [HttpGet("uoms")]
    [ProducesResponseType(typeof(IReadOnlyList<UomDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UomDto>>> GetUoms(CancellationToken cancellationToken)
    {
        var result = await _service.GetUomCatalogueAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>UOMs available for specific SKUs, keyed by SKU code.</summary>
    [HttpGet("uoms/by-sku")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<string, IReadOnlyList<UomDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyDictionary<string, IReadOnlyList<UomDto>>>> GetUomsBySku(
        [FromQuery] string skuCodes, CancellationToken cancellationToken)
    {
        var codes = string.IsNullOrWhiteSpace(skuCodes)
            ? Array.Empty<string>()
            : skuCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = await _service.GetUomsBySkuAsync(codes, cancellationToken);
        return Ok(result);
    }
}
