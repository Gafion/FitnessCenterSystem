using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitnessCenter.Modules.Booking.Application;

/// <summary>
/// Expires waitlist offers that ran out and offers their spots to the next person, when no
/// one makes a request (D-067). Every booking request brings its own class's offers up to
/// date first, so this sweep only covers the quiet time between requests. The rules and the
/// time come from the domain and the injected clock; the timer only sets how soon the next
/// person hears about a spot.
/// </summary>
internal sealed class WaitlistOfferJob(
    IServiceScopeFactory scopes, IConfiguration configuration, ILogger<WaitlistOfferJob> logger) : BackgroundService
{
    /// <summary>False turns the job off. The integration tests turn it off and run the sweep themselves.</summary>
    public const string EnabledSetting = "Booking:WaitlistOffers:Enabled";

    /// <summary>Time between runs, e.g. <c>00:01:00</c>.</summary>
    public const string IntervalSetting = "Booking:WaitlistOffers:Interval";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledSetting, defaultValue: true))
        {
            return;
        }

        var interval = configuration.GetValue(IntervalSetting, TimeSpan.FromMinutes(1));
        // Let the host finish starting before the first run.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var offered = await scope.ServiceProvider.GetRequiredService<BookingHandler>().UpdateOffersAsync(stoppingToken);
                if (offered > 0)
                {
                    logger.LogInformation("Made {Count} waitlist offers.", offered);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // One failed run must not stop the job; the next run tries again.
                logger.LogError(exception, "The waitlist offer run failed.");
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
