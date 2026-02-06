using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;

namespace Sdmp.Monolith.Reliability;

/// <summary>
/// Makes unsafe operations safe to retry. A client sends an <c>Idempotency-Key</c> header on a
/// mutating request; if we have already processed that key we replay the stored response instead of
/// performing the operation twice. This is the same mechanism payment APIs (e.g. Stripe) use so a
/// network retry never double-charges a customer.
///
/// Storage here is an in-memory cache with a TTL for simplicity. In a distributed deployment this
/// moves to Redis so the guarantee holds across instances.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private static readonly ConcurrentDictionary<string, CachedResponse> Cache = new();

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only mutating verbs need idempotency protection.
        var isMutating = HttpMethods.IsPost(context.Request.Method)
                         || HttpMethods.IsPut(context.Request.Method)
                         || HttpMethods.IsPatch(context.Request.Method);

        if (!isMutating || !context.Request.Headers.TryGetValue(HeaderName, out var keyValues))
        {
            await _next(context);
            return;
        }

        var key = keyValues.ToString();

        if (Cache.TryGetValue(key, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Replaying idempotent response for key {Key}", key);
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers["Idempotency-Replayed"] = "true";
            await context.Response.Body.WriteAsync(cached.Body);
            return;
        }

        // Capture the response so we can store it for replay.
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await _next(context);

        buffer.Position = 0;
        var bytes = buffer.ToArray();

        // Only cache successful results; failures should be retryable.
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            Cache[key] = new CachedResponse(context.Response.StatusCode,
                context.Response.ContentType ?? "application/json", bytes,
                DateTimeOffset.UtcNow.Add(Ttl));
        }

        context.Response.Body = originalBody;
        await context.Response.Body.WriteAsync(bytes);
    }

    private readonly record struct CachedResponse(
        int StatusCode, string ContentType, byte[] Body, DateTimeOffset ExpiresAt);
}
