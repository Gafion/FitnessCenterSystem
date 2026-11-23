using System.Text;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace FitnessCenter.Modules.Identity.Application;

/// <summary>Email confirmation links (D-038): the account can't log in until it follows one.</summary>
internal static class EmailConfirmation
{
    /// <summary>The frontend page that reads the link and calls the confirm endpoint.</summary>
    public const string PagePath = "/confirm-email";

    public static async Task SendAsync(
        UserManager<Account> users, IEmailSender<Account> emails, Account account, HttpRequest request)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(account);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        // The browser's address: the Vite proxy forwards it locally, and in production the
        // API serves the frontend itself.
        var link = $"{request.Scheme}://{request.Host}{PagePath}"
            + QueryString.Create(new Dictionary<string, string?> { ["account"] = account.Id.ToString(), ["code"] = code });

        await emails.SendConfirmationLinkAsync(account, account.Email!, link);
    }

    public static string? DecodeCode(string code)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
