using System.Buffers.Binary;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// Plays the authenticator app (D-059): a plain RFC 6238 TOTP generator (HMAC-SHA1, 30-second
/// steps, 6 digits), written from the RFC and not from Identity, so the tests check that the API
/// speaks the standard any app speaks.
/// </summary>
internal static class TestAuthenticator
{
    public sealed record Setup(bool Enabled, string? SharedKey, string? AuthenticatorUri);

    public sealed record RecoveryCodes(IReadOnlyList<string> Codes);

    /// <summary>The current code for a Base32 key (spaces allowed, any case).</summary>
    public static string Code(string sharedKey, DateTimeOffset? at = null)
    {
        var key = Base32Decode(sharedKey);
        var step = (at ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30;
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
#pragma warning disable CA5350 // RFC 6238's default, which authenticator apps use
        var hash = HMACSHA1.HashData(key, counter);
#pragma warning restore CA5350
        var offset = hash[^1] & 0x0F;
        var binary = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset)) & 0x7FFFFFFF;
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>A code from a different time window, so it is wrong now.</summary>
    public static string WrongCode(string sharedKey) => Code(sharedKey, DateTimeOffset.UtcNow.AddMinutes(-10));

    /// <summary>Sets up the authenticator for the account logged in on <paramref name="client"/>.</summary>
    /// <returns>The setup key and the recovery codes.</returns>
    public static async Task<(string Key, IReadOnlyList<string> RecoveryCodes)> SetUpAsync(HttpClient client)
    {
        var setup = await client.GetFromJsonAsync<Setup>("/api/identity/authenticator");
        Assert.IsNotNull(setup?.SharedKey);
        using var enable = await client.PostAsJsonAsync("/api/identity/authenticator", new { code = Code(setup.SharedKey) });
        Assert.AreEqual(HttpStatusCode.OK, enable.StatusCode, await enable.Content.ReadAsStringAsync());
        var codes = await enable.Content.ReadFromJsonAsync<RecoveryCodes>();
        return (setup.SharedKey, codes!.Codes);
    }

    public static async Task<HttpResponseMessage> LogInWithCodeAsync(HttpClient client, string code, bool isRecoveryCode = false) =>
        await client.PostAsJsonAsync("/api/identity/login/two-factor", new { code, isRecoveryCode });

    /// <summary>Password, then a code from the app: a full staff login.</summary>
    public static async Task LogInAsync(HttpClient client, string email, string password, string sharedKey)
    {
        using var login = await TestAccounts.LogInAsync(client, email, password);
        Assert.AreEqual(HttpStatusCode.Forbidden, login.StatusCode, await login.Content.ReadAsStringAsync());
        using var code = await LogInWithCodeAsync(client, Code(sharedKey));
        Assert.AreEqual(HttpStatusCode.NoContent, code.StatusCode, await code.Content.ReadAsStringAsync());
    }

    private static byte[] Base32Decode(string text)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = text.Replace(" ", "", StringComparison.Ordinal).TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in clean)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(c, StringComparison.Ordinal);
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)(buffer >> bits));
                buffer &= (1 << bits) - 1;
            }
        }

        return [.. bytes];
    }
}
