using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WinitTask.Api.Exceptions;
using WinitTask.Api.Middleware;

namespace WinitTask.Api.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task NotFound_Becomes404ProblemJson()
    {
        var context = await InvokeAsync(_ => throw NotFoundException.For("Load template", "tpl-1"));

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        using var json = await ReadAsync(context);
        Assert.Equal(404, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", json.RootElement.GetProperty("title").GetString());
        Assert.Equal("Load template 'tpl-1' was not found.", json.RootElement.GetProperty("detail").GetString());
        Assert.Equal("/api/load-templates/tpl-1", json.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task BusinessValidation_Becomes400_WithFieldErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["TemplateName"] = new[] { "Template name is required." },
            ["Lines"] = new[] { "Unknown SKU code(s): NOPE.", "UOM 'XX' is not valid for SKU MILK." }
        };

        var context = await InvokeAsync(_ => throw new BusinessValidationException(errors));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        using var json = await ReadAsync(context);
        Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred.", json.RootElement.GetProperty("title").GetString());
        var bodyErrors = json.RootElement.GetProperty("errors");
        Assert.Equal("Template name is required.", bodyErrors.GetProperty("TemplateName")[0].GetString());
        Assert.Equal(2, bodyErrors.GetProperty("Lines").GetArrayLength());
    }

    [Fact]
    public async Task UnexpectedException_Becomes500_WithoutTheExceptionText()
    {
        var context = await InvokeAsync(_ => throw new InvalidOperationException("secret connection string"));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        using var json = await ReadAsync(context);
        var detail = json.RootElement.GetProperty("detail").GetString();
        Assert.Equal("Internal Server Error", json.RootElement.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred. Please try again.", detail);
        Assert.DoesNotContain("secret connection string", json.RootElement.ToString());
    }

    private static async Task<DefaultHttpContext> InvokeAsync(RequestDelegate next)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/load-templates/tpl-1";
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        return context;
    }

    private static async Task<JsonDocument> ReadAsync(HttpContext context) =>
        await JsonDocument.ParseAsync(context.Response.Body);
}
