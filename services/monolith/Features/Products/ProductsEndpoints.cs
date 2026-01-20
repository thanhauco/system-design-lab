using Sdmp.Monolith.Domain;

namespace Sdmp.Monolith.Features.Products;

public sealed record CreateProductRequest(string Sku, string Name, decimal Price, int Stock);

/// <summary>Products vertical slice: the catalog read/write surface.</summary>
public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProducts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapGet("/", async (IRepository<Product> repo, CancellationToken ct) =>
                Results.Ok(await repo.ListAsync(ct)))
            .WithName("ListProducts")
            .WithSummary("List catalog products");

        group.MapGet("/{id:guid}", async (Guid id, IRepository<Product> repo, CancellationToken ct) =>
                await repo.GetAsync(id, ct) is { } product ? Results.Ok(product) : Results.NotFound())
            .WithName("GetProduct")
            .WithSummary("Get a product by id");

        group.MapPost("/", async (CreateProductRequest req, IRepository<Product> repo, CancellationToken ct) =>
            {
                if (req.Price < 0 || req.Stock < 0)
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["price/stock"] = ["Price and stock must be non-negative."]
                    });

                var product = new Product(Guid.NewGuid(), req.Sku.Trim(), req.Name.Trim(), req.Price, req.Stock);
                await repo.AddAsync(product, ct);
                return Results.Created($"/api/v1/products/{product.Id}", product);
            })
            .WithName("CreateProduct")
            .WithSummary("Create a catalog product");

        return app;
    }
}
