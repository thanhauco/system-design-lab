using Sdmp.Monolith.Observability;

namespace Sdmp.Monolith.Messaging;

/// <summary>
/// Background processor that drains the Outbox: it polls for pending messages, "publishes" each
/// (here, in-process — in Phase 4 this becomes a Kafka produce), and marks it processed. Because the
/// message was committed with the business change, this loop can retry safely until it succeeds —
/// the event is guaranteed to be delivered at least once.
/// </summary>
public sealed class OutboxProcessor : BackgroundService
{
    private static readonly System.Diagnostics.Metrics.Counter<long> Published =
        Telemetry.Meter.CreateCounter<long>("sdmp_outbox_published_total",
            description: "Total Outbox messages published.");

    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(1);
    private const int BatchSize = 50;

    public OutboxProcessor(IServiceProvider services, ILogger<OutboxProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DrainAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // Never let the loop die — log and retry next tick.
                _logger.LogError(ex, "Outbox drain failed; will retry.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task DrainAsync(CancellationToken ct)
    {
        // Scope per cycle so the EF-backed outbox gets a fresh DbContext.
        await using var scope = _services.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        var pending = await outbox.ListPendingAsync(BatchSize, ct);
        foreach (var message in pending)
        {
            using var activity = Telemetry.ActivitySource.StartActivity("OutboxPublish");
            activity?.SetTag("outbox.type", message.Type);
            activity?.SetTag("outbox.id", message.Id);

            // In-process "publish". Phase 4 replaces this with a real broker produce.
            _logger.LogInformation("Publishing outbox message {Type} {Id}", message.Type, message.Id);

            await outbox.MarkProcessedAsync(message.Id, ct);
            Published.Add(1);
        }
    }
}
