using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Sdmp.Monolith.Tests;

/// <summary>
/// Boots the real application in-process (default in-memory provider) and exercises the HTTP surface.
/// These tests prove the platform standards end to end: health, seeded catalog, validation, the
/// order happy path, idempotent replay, and the resilience demo endpoint.
/// </summary>
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_is_healthy()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Metrics_endpoint_is_exposed()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Products_are_seeded()
    {
        var client = _factory.CreateClient();
        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/v1/products");
        Assert.NotNull(products);
        Assert.NotEmpty(products!);
    }

    [Fact]
    public async Task Create_user_with_invalid_email_returns_400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/users",
            new { email = "not-an-email", displayName = "X" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Create_order_happy_path_returns_201_with_total()
    {
        var client = _factory.CreateClient();

        // Seeded user + product ids.
        var userId = "11111111-1111-1111-1111-111111111111";
        var productId = "aaaaaaaa-0000-0000-0000-000000000002"; // 10GbE Smart NIC @ 399.00

        var res = await client.PostAsJsonAsync("/api/v1/orders", new
        {
            userId,
            lines = new[] { new { productId, quantity = 2 } }
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var order = await res.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(798.00m, order!.Total);
    }

    [Fact]
    public async Task Create_order_for_unknown_user_returns_400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/orders", new
        {
            userId = Guid.NewGuid(),
            lines = new[] { new { productId = "aaaaaaaa-0000-0000-0000-000000000002", quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Idempotency_key_replays_the_same_response()
    {
        var client = _factory.CreateClient();
        var key = Guid.NewGuid().ToString();
        var payload = new
        {
            userId = "22222222-2222-2222-2222-222222222222",
            lines = new[] { new { productId = "aaaaaaaa-0000-0000-0000-000000000003", quantity = 1 } }
        };

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        { Content = JsonContent.Create(payload) };
        req1.Headers.Add("Idempotency-Key", key);
        var res1 = await client.SendAsync(req1);
        var order1 = await res1.Content.ReadFromJsonAsync<OrderDto>();

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders")
        { Content = JsonContent.Create(payload) };
        req2.Headers.Add("Idempotency-Key", key);
        var res2 = await client.SendAsync(req2);
        var order2 = await res2.Content.ReadFromJsonAsync<OrderDto>();

        // Same id returned twice => the operation was not performed a second time.
        Assert.Equal(order1!.Id, order2!.Id);
        Assert.True(res2.Headers.Contains("Idempotency-Replayed"));
    }

    [Fact]
    public async Task Reliability_call_happy_path_succeeds()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/v1/reliability/call?failRate=0&latencyMs=5");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Response_carries_correlation_id_header()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/v1/products");
        Assert.True(res.Headers.Contains("X-Correlation-Id"));
    }

    private sealed record ProductDto(Guid Id, string Sku, string Name, decimal Price, int Stock);
    private sealed record OrderDto(Guid Id, Guid UserId, decimal Total);
}
