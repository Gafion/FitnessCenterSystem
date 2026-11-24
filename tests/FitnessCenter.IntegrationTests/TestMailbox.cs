using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// A throwaway Mailpit container, the same test mailbox Aspire runs locally (D-038). The API
/// sends real SMTP to it, and tests read the emails back through Mailpit's HTTP API.
/// </summary>
internal static partial class TestMailbox
{
    private const int SmtpPort = 1025;
    private const int HttpPort = 8025;

    private static IContainer? _container;
    private static HttpClient? _api;

    public static string SmtpHost => Container.Hostname;

    public static int SmtpMappedPort => Container.GetMappedPublicPort(SmtpPort);

    private static IContainer Container =>
        _container ?? throw new InvalidOperationException("The Mailpit container has not started.");

    public static async Task StartAsync()
    {
        _container = new ContainerBuilder("docker.io/axllent/mailpit:v1.33.1")
            .WithPortBinding(SmtpPort, assignRandomHostPort: true)
            .WithPortBinding(HttpPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(request => request.ForPort(HttpPort).ForPath("/livez")))
            .Build();
        await _container.StartAsync();

        _api = new HttpClient
        {
            BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpPort)}"),
        };
    }

    public static async Task StopAsync()
    {
        _api?.Dispose();
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>The newest confirmation link sent to <paramref name="email"/>; waits a little for it to arrive.</summary>
    public static async Task<Uri> ConfirmationLinkForAsync(string email)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var text = await NewestTextToAsync(email);
            if (text is not null && ConfirmationLink().Match(text) is { Success: true } match)
            {
                return new Uri(match.Value);
            }

            await Task.Delay(100);
        }

        throw new AssertFailedException($"No confirmation email reached {email}.");
    }

    public static async Task<int> CountTo(string email)
    {
        var search = await SearchAsync(email);
        return search.Messages.Count;
    }

    /// <summary>How many emails with this subject reached <paramref name="email"/>.</summary>
    public static async Task<int> CountToAsync(string email, string subject) =>
        (await QueryAsync($"to:\"{email}\" subject:\"{subject}\"")).Messages.Count;

    /// <summary>The text of the newest email with this subject to <paramref name="email"/>; waits a little for it to arrive.</summary>
    public static async Task<string> TextToAsync(string email, string subject)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var search = await QueryAsync($"to:\"{email}\" subject:\"{subject}\"");
            if (search.Messages.Count > 0)
            {
                var message = await _api!.GetFromJsonAsync<Message>($"/api/v1/message/{search.Messages[0].Id}");
                return message!.Text;
            }

            await Task.Delay(100);
        }

        throw new AssertFailedException($"No email \"{subject}\" reached {email}.");
    }

    private static async Task<string?> NewestTextToAsync(string email)
    {
        var search = await SearchAsync(email);
        if (search.Messages.Count == 0)
        {
            return null;
        }

        // Mailpit lists the newest message first.
        var message = await _api!.GetFromJsonAsync<Message>($"/api/v1/message/{search.Messages[0].Id}");
        return message?.Text;
    }

    private static Task<SearchResult> SearchAsync(string email) => QueryAsync($"to:\"{email}\"");

    private static async Task<SearchResult> QueryAsync(string query) =>
        (await _api!.GetFromJsonAsync<SearchResult>($"/api/v1/search?query={Uri.EscapeDataString(query)}"))!;

    [GeneratedRegex(@"\S+/confirm-email\?\S+")]
    private static partial Regex ConfirmationLink();

    private sealed record SearchResult(IReadOnlyList<MessageSummary> Messages);

    private sealed record MessageSummary(string Id);

    private sealed record Message(string Text);
}
