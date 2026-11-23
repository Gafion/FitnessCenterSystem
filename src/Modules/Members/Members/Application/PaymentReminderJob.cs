using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitnessCenter.Modules.Members.Application;

/// <summary>
/// Runs <see cref="PaymentReminders"/> at startup and then on a timer (D-042). The reminders
/// themselves decide what is due, from the injected clock; the timer only sets how soon after
/// midnight the email goes out.
/// </summary>
internal sealed class PaymentReminderJob(
    IServiceScopeFactory scopes, IConfiguration configuration, ILogger<PaymentReminderJob> logger) : BackgroundService
{
    /// <summary>False turns the job off. The integration tests turn it off and run the reminders themselves.</summary>
    public const string EnabledSetting = "Members:PaymentReminders:Enabled";

    /// <summary>Time between runs, e.g. <c>00:15:00</c>.</summary>
    public const string IntervalSetting = "Members:PaymentReminders:Interval";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledSetting, defaultValue: true))
        {
            return;
        }

        var interval = configuration.GetValue(IntervalSetting, TimeSpan.FromMinutes(15));
        // Let the host finish starting before the first run.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var sent = await scope.ServiceProvider.GetRequiredService<PaymentReminders>().SendDueAsync(stoppingToken);
                if (sent > 0)
                {
                    logger.LogInformation("Sent {Count} payment reminders.", sent);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // One failed run must not stop the job; the next run tries again.
                logger.LogError(exception, "The payment reminder run failed.");
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
