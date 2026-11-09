using Microsoft.AspNetCore.Mvc;
using WinitTask.Api.DTOs;
using WinitTask.Api.Services;

namespace WinitTask.Api.Controllers;

/// <summary>Maintain Load Request Template — list, add, edit and delete.</summary>
[ApiController]
[Route("api/load-templates")]
[Produces("application/json")]
public class LoadTemplatesController : ControllerBase
{
    private readonly ILoadTemplateService _service;

    public LoadTemplatesController(ILoadTemplateService service)
    {
        _service = service;
    }

    /// <summary>Paged, sorted and name-filtered list for the main grid.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LoadTemplateListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LoadTemplateListItemDto>>> GetAll(
        [FromQuery] LoadTemplateQueryParameters query, CancellationToken cancellationToken)
    {
        var result = await _service.GetPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>One template with all of its product lines, for the Edit page.</summary>
    [HttpGet("{uid}")]
    [ProducesResponseType(typeof(LoadTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoadTemplateDetailDto>> GetByUid(string uid, CancellationToken cancellationToken)
    {
        var result = await _service.GetByUidAsync(uid, cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates a template together with its lines.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(LoadTemplateDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoadTemplateDetailDto>> Create(
        [FromBody] SaveLoadTemplateRequest request, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByUid), new { uid = created.Uid }, created);
    }

    /// <summary>Updates the header and replaces the line set, recomputing ItemCount.</summary>
    [HttpPut("{uid}")]
    [ProducesResponseType(typeof(LoadTemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoadTemplateDetailDto>> Update(
        string uid, [FromBody] SaveLoadTemplateRequest request, CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(uid, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Deletes a template and its lines. The UI confirms before calling this.</summary>
    [HttpDelete("{uid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string uid, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(uid, cancellationToken);
        return NoContent();
    }
}
