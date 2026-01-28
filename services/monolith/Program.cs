using Sdmp.Monolith.Observability;
using Sdmp.Monolith.Domain;
using Sdmp.Monolith.Infrastructure;
using Sdmp.Monolith.Features.Users;
using Sdmp.Monolith.Features.Products;
using Sdmp.Monolith.Features.Orders;

var builder = WebApplication.CreateBuilder(args);

// Observability standard: metrics, traces, structured JSON logs, health checks, correlation ids.
builder.AddObservability();

// Persistence: in-memory by default (zero external deps). Swap to Postgres by changing this line.
builder.Services.AddSingleton<IRepository<User>, InMemoryRepository<User>>();
builder.Services.AddSingleton<IRepository<Product>, InMemoryRepository<Product>>();
builder.Services.AddSingleton<IRepository<Order>, InMemoryRepository<Order>>();

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

// Domain slices.
app.MapUsers();
app.MapProducts();
app.MapOrders();

// Seed demo data for the in-memory stores.
await SeedData.SeedAsync(
    app.Services.GetRequiredService<IRepository<User>>(),
    app.Services.GetRequiredService<IRepository<Product>>());

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
