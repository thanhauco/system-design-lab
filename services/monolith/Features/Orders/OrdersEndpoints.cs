using Sdmp.Monolith.Domain;
using Sdmp.Monolith.Observability;

namespace Sdmp.Monolith.Features.Orders;

public sealed record CreateOrderLine(Guid ProductId, int Quantity);
public sealed record CreateOrderRequest(Guid UserId, IReadOnlyList<CreateOrderLine> Lines);

/// <summary>
/// Orders vertical slice. Order creation is the canonical "interesting" operation: it validates the
/// user, prices lines from the catalog, checks stock, and emits a custom span + metric. In Phase 4
/// this becomes event-sourced; the contract here is deliberately stable so that move is incremental.
/// </summary>
public static class OrdersEndpoints
{
    private static readonly System.Diagnostics.Metrics.Counter<long> OrdersCreated =
        Telemetry.Meter.CreateCounter<long>("sdmp_orders_created_total", description: "Total orders created.");

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/orders").WithTags("Orders");

        group.MapGet("/", async (IRepository<Order> repo, CancellationToken ct) =>
                Results.Ok(await repo.ListAsync(ct)))
            .WithName("ListOrders")
            .WithSummary("List orders");

        group.MapGet("/{id:guid}", async (Guid id, IRepository<Order> repo, CancellationToken ct) =>
                await repo.GetAsync(id, ct) is { } order ? Results.Ok(order) : Results.NotFound())
            .WithName("GetOrder")
            .WithSummary("Get an order by id");

        group.MapPost("/", async (
                CreateOrderRequest req,
                IRepository<User> users,
                IRepository<Product> products,
                IRepository<Order> orders,
                ILogger<Program> logger,
                CancellationToken ct) =>
            {
                using var activity = Telemetry.ActivitySource.StartActivity("CreateOrder");
                activity?.SetTag("order.userId", req.UserId);

                if (await users.GetAsync(req.UserId, ct) is null)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["userId"] = ["Unknown user."]
                    });

                if (req.Lines is null || req.Lines.Count == 0)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["lines"] = ["An order must contain at least one line."]
                    });

                var resolvedLines = new List<OrderLine>(req.Lines.Count);
                foreach (var line in req.Lines)
                {
                    var product = await products.GetAsync(line.ProductId, ct);
                    if (product is null)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["lines"] = [$"Unknown product {line.ProductId}."]
                        });

                    if (line.Quantity <= 0 || line.Quantity > product.Stock)
                        return Results.ValidationProblem(new Dictionary<string, string[]>
                        {
                            ["quantity"] = [$"Invalid quantity for {product.Sku} (in stock: {product.Stock})."]
                        });

                    resolvedLines.Add(new OrderLine(product.Id, line.Quantity, product.Price));
                }

                var order = new Order(Guid.NewGuid(), req.UserId, resolvedLines, OrderStatus.Pending,
                    DateTimeOffset.UtcNow);
                await orders.AddAsync(order, ct);

                OrdersCreated.Add(1);
                activity?.SetTag("order.id", order.Id);
                activity?.SetTag("order.total", order.Total);
                logger.LogInformation("Order {OrderId} created for user {UserId} total {Total}",
                    order.Id, order.UserId, order.Total);

                return Results.Created($"/api/v1/orders/{order.Id}", order);
            })
            .WithName("CreateOrder")
            .WithSummary("Create an order");

        return app;
    }
}
