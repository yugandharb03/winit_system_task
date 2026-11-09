using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using WinitTask.Api.Controllers;

namespace WinitTask.Api.Tests.Controllers;

/// <summary>
/// Locks the public routes the React client calls. These tests do not boot the host.
/// </summary>
public class ApiRouteContractTests
{
    [Fact]
    public void LoadTemplates_ExposesListGetCreateUpdateAndDelete()
    {
        var controller = typeof(LoadTemplatesController);

        Assert.Equal("api/load-templates", controller.GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.Equal("application/json", controller.GetCustomAttribute<ProducesAttribute>()!.ContentTypes.Single());

        Assert.Null(Method(controller, nameof(LoadTemplatesController.GetAll))
            .GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Equal("{uid}", Method(controller, nameof(LoadTemplatesController.GetByUid))
            .GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.NotNull(Method(controller, nameof(LoadTemplatesController.Create))
            .GetCustomAttribute<HttpPostAttribute>());
        Assert.Equal("{uid}", Method(controller, nameof(LoadTemplatesController.Update))
            .GetCustomAttribute<HttpPutAttribute>()!.Template);
        Assert.Equal("{uid}", Method(controller, nameof(LoadTemplatesController.Delete))
            .GetCustomAttribute<HttpDeleteAttribute>()!.Template);
    }

    [Fact]
    public void Skus_ExposesSearchCatalogueAndPerSkuUoms()
    {
        var controller = typeof(SkusController);

        Assert.Equal("api/skus", controller.GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.Null(Method(controller, nameof(SkusController.Search))
            .GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Equal("uoms", Method(controller, nameof(SkusController.GetUoms))
            .GetCustomAttribute<HttpGetAttribute>()!.Template);
        Assert.Equal("uoms/by-sku", Method(controller, nameof(SkusController.GetUomsBySku))
            .GetCustomAttribute<HttpGetAttribute>()!.Template);
    }

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethod(name) ?? throw new InvalidOperationException($"Missing action {type.Name}.{name}.");
}
