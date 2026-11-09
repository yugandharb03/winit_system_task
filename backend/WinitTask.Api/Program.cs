using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WinitTask.Api.Data;
using WinitTask.Api.Middleware;
using WinitTask.Api.Repositories;
using WinitTask.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicy = "ReactDevClients";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Model-binding failures come back in the same ValidationProblemDetails shape as our
// own business-rule failures, so the client has one error format to handle.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Winit Load Template API",
        Version = "v1",
        Description = "Maintain Load Request Templates and the products inside them."
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Data access
builder.Services.AddScoped<ILoadTemplateRepository, LoadTemplateRepository>();
builder.Services.AddScoped<ISkuRepository, SkuRepository>();

// Business logic
builder.Services.AddScoped<ILoadTemplateService, LoadTemplateService>();
builder.Services.AddScoped<ISkuService, SkuService>();

// The React dev server runs on 5173; 5174/5175 cover extra Vite instances.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:5174", "http://localhost:5175" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Seed reference and sample data once the schema exists.
if (builder.Configuration.GetValue("SeedDatabase", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(db, scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger(nameof(SeedData)));
}

// First in the pipeline so it catches everything downstream.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Winit Load Template API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();
