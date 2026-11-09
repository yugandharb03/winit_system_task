using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WinitTask.Api.Controllers;
using WinitTask.Api.DTOs;
using WinitTask.Api.Exceptions;
using WinitTask.Api.Services;

namespace WinitTask.Api.Tests.Controllers;

public class LoadTemplatesControllerTests
{
    private readonly Mock<ILoadTemplateService> _service = new();
    private readonly LoadTemplatesController _controller;

    public LoadTemplatesControllerTests()
    {
        _controller = new LoadTemplatesController(_service.Object);
    }

    [Fact]
    public async Task GetAll_Returns200_WithThePagedListFromTheService()
    {
        var query = new LoadTemplateQueryParameters
        {
            Name = "Weekly",
            SortBy = "templateName",
            SortDir = "asc",
            Page = 2,
            PageSize = 10
        };
        var expected = new PagedResult<LoadTemplateListItemDto>
        {
            Items = new[]
            {
                new LoadTemplateListItemDto
                {
                    Uid = "tpl-1",
                    TemplateName = "Weekly",
                    TemplateDescription = "Mon-Sun",
                    ItemCount = 3
                }
            },
            TotalCount = 12,
            Page = 2,
            PageSize = 10
        };
        using var cts = new CancellationTokenSource();
        _service
            .Setup(s => s.GetPagedAsync(query, cts.Token))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll(query, cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Same(expected, ok.Value);
        _service.Verify(s => s.GetPagedAsync(query, cts.Token), Times.Once);
    }

    [Fact]
    public async Task GetByUid_Returns200_WithTheTemplateDetail()
    {
        var detail = SampleDetail("tpl-9");
        using var cts = new CancellationTokenSource();
        _service.Setup(s => s.GetByUidAsync("tpl-9", cts.Token)).ReturnsAsync(detail);

        var result = await _controller.GetByUid("tpl-9", cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Same(detail, ok.Value);
    }

    [Fact]
    public async Task GetByUid_WhenTheTemplateDoesNotExist_LetsNotFoundPropagate()
    {
        _service
            .Setup(s => s.GetByUidAsync("missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(NotFoundException.For("Load template", "missing"));

        var ex = await Assert.ThrowsAsync<NotFoundException>(
            () => _controller.GetByUid("missing", CancellationToken.None));

        Assert.Equal("Load template 'missing' was not found.", ex.Message);
    }

    [Fact]
    public async Task Create_Returns201_WithLocationPointingAtTheNewTemplate()
    {
        var request = new SaveLoadTemplateRequest { TemplateName = "New route" };
        var created = SampleDetail("new-uid");
        using var cts = new CancellationTokenSource();
        _service.Setup(s => s.CreateAsync(request, cts.Token)).ReturnsAsync(created);

        var result = await _controller.Create(request, cts.Token);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(LoadTemplatesController.GetByUid), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.Equal("new-uid", createdResult.RouteValues["uid"]);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task Create_WhenBusinessRulesFail_LetsValidationPropagate()
    {
        var request = new SaveLoadTemplateRequest { TemplateName = "Bad" };
        var errors = new Dictionary<string, string[]>
        {
            ["Lines"] = new[] { "Unknown SKU code(s): NOPE." }
        };
        _service
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessValidationException(errors));

        var ex = await Assert.ThrowsAsync<BusinessValidationException>(
            () => _controller.Create(request, CancellationToken.None));

        Assert.Equal("Unknown SKU code(s): NOPE.", ex.Errors["Lines"].Single());
    }

    [Fact]
    public async Task Update_Returns200_WithTheSavedTemplate()
    {
        var request = new SaveLoadTemplateRequest { TemplateName = "Edited" };
        var updated = SampleDetail("tpl-4");
        using var cts = new CancellationTokenSource();
        _service.Setup(s => s.UpdateAsync("tpl-4", request, cts.Token)).ReturnsAsync(updated);

        var result = await _controller.Update("tpl-4", request, cts.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task Update_WhenTheTemplateDoesNotExist_LetsNotFoundPropagate()
    {
        _service
            .Setup(s => s.UpdateAsync("gone", It.IsAny<SaveLoadTemplateRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(NotFoundException.For("Load template", "gone"));

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _controller.Update("gone", new SaveLoadTemplateRequest { TemplateName = "X" }, CancellationToken.None));

        Assert.Equal("Load template 'gone' was not found.", ex.Message);
    }

    [Fact]
    public async Task Delete_Returns204_AndAsksTheServiceToDeleteThatTemplate()
    {
        using var cts = new CancellationTokenSource();
        _service.Setup(s => s.DeleteAsync("tpl-3", cts.Token)).Returns(Task.CompletedTask);

        var result = await _controller.Delete("tpl-3", cts.Token);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, ((NoContentResult)result).StatusCode);
        _service.Verify(s => s.DeleteAsync("tpl-3", cts.Token), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenTheTemplateDoesNotExist_LetsNotFoundPropagate()
    {
        _service
            .Setup(s => s.DeleteAsync("gone", It.IsAny<CancellationToken>()))
            .ThrowsAsync(NotFoundException.For("Load template", "gone"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => _controller.Delete("gone", CancellationToken.None));
    }

    private static LoadTemplateDetailDto SampleDetail(string uid) => new()
    {
        Uid = uid,
        TemplateName = "Weekly",
        TemplateDescription = "Standard",
        ItemCount = 1,
        Lines = new[]
        {
            new LoadTemplateLineDto
            {
                Uid = "line-1",
                SkuCode = "MILK",
                SkuName = "Fresh Milk",
                UomCode = "EA",
                UomName = "Each",
                MonQty = 1
            }
        }
    };
}
