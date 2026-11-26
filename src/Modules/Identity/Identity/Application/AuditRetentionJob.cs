using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>
/// Deletes audit events older than one year (D-072 amendment), once at startup and then once a
/// day. What counts as expired comes from <see cref="AuditTrail.KeepFrom"/> and the injected clock.
/// </summary>
internal sealed class AuditRetentionJob(
    IServiceScopeFactory scopes, IConfiguration configuration, ILogger<AuditRetentionJob> logger) : BackgroundService
{
    /// <summary>False turns the job off. The integration tests turn it off and call the purge themselves.</summary>
    public const string EnabledSetting = "Identity:AuditRetention:Enabled";

    /// <summary>Time between runs, e.g. <c>1.00:00:00</c>.</summary>
    public const string IntervalSetting = "Identity:AuditRetention:Interval";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledSetting, defaultValue: true))
        {
            return;
        }

        var interval = configuration.GetValue(IntervalSetting, TimeSpan.FromDays(1));
        // Let the host finish starting before the first run.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var deleted = await scope.ServiceProvider.GetRequiredService<AuditTrail>().PurgeExpiredAsync(stoppingToken);
                if (deleted > 0)
                {
                    logger.LogInformation("Deleted {Count} audit events older than one year.", deleted);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // One failed run must not stop the job; the next run tries again.
                logger.LogError(exception, "The audit retention run failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
