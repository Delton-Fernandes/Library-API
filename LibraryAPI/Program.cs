using System.Reflection;
using LibraryAPI.Data;
using LibraryAPI.Services;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Single instance so all controllers share the same in-memory state
builder.Services.AddSingleton<MockData>();
builder.Services.AddSingleton<ILibraryService, LibraryService>();

builder.Services.AddHealthChecks();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Library Tracking API",
        Version = "v1",
        Description = "HTTP API for tracking library books, members, and checkouts.",
        Contact = new OpenApiContact { Name = "Library System", Email = "admin@library.example.com" }
    });

    // Pull XML doc comments into Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Library API v1");
    c.RoutePrefix = string.Empty;   // Swagger UI at app root
});

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
    });
});

app.UseHttpsRedirection();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Expose for test project
public partial class Program { }