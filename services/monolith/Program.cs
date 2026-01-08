using Sdmp.Monolith.Observability;

var builder = WebApplication.CreateBuilder(args);

// Observability standard: metrics, traces, structured JSON logs, health checks, correlation ids.
builder.AddObservability();

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

// Wires correlation-id middleware, /metrics, /health, and /health/ready.
app.UseObservability();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
