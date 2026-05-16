using Microsoft.AspNetCore.Http;

namespace Sdmp.Monolith.Reliability;

/// <summary>
/// Makes unsafe operations safe to retry. A client sends an <c>Idempotency-Key</c> header on a
/// mutating request; if we have already processed that key we replay the stored response instead of
/// performing the operation twice. This is the same mechanism payment APIs (e.g. Stripe) use so a
/// network retry never double-charges a customer.
///
/// Storage is provided by <see cref="IIdempotencyStore"/>: in-memory for a single instance, or Redis
/// so the guarantee holds across a horizontally scaled fleet.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private readonly RequestDelegate _next;
    private readonly IIdempotencyStore _store;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        IIdempotencyStore store,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _store = store;
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
        var ct = context.RequestAborted;

        if (await _store.TryGetAsync(key, ct) is { } cached)
        {
            _logger.LogInformation("Replaying idempotent response for key {Key}", key);
            context.Response.StatusCode = cached.StatusCode;
            context.Response.ContentType = cached.ContentType;
            context.Response.Headers["Idempotency-Replayed"] = "true";
            await context.Response.Body.WriteAsync(cached.Body, ct);
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
            await _store.SaveAsync(key,
                new IdempotentResponse(context.Response.StatusCode,
                    context.Response.ContentType ?? "application/json", bytes),
                Ttl, ct);
        }

        context.Response.Body = originalBody;
        await context.Response.Body.WriteAsync(bytes, ct);
    }
}
