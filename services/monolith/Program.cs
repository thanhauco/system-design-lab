using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger so every endpoint is documented and explorable.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SDMP Monolith API",
        Version = "v1",
        Description = "Phase 1 reference monolith for the System Design Mastery Platform."
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "SDMP Monolith API v1"));

// Minimal liveness endpoint; full health/readiness arrives with the observability task.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .WithName("Health")
   .WithTags("Platform");

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
