using FitnessCenter.Modules.Identity;
using FitnessCenter.Modules.Identity.Infrastructure;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FitnessCenter.IntegrationTests;

/// <summary>
/// The <c>Email</c> settings section (D-066): Mailpit's plain SMTP stays the default, and a
/// login always means TLS. No container needed; the values are synthetic placeholders.
/// </summary>
[TestClass]
public sealed class EmailSettingsTests
{
    private static EmailOptions Bind(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddIdentityModule(configuration);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<EmailOptions>>().Value;
    }

    [TestMethod]
    public void Without_settings_the_defaults_are_plain_smtp_and_the_placeholder_sender()
    {
        var options = Bind([]);

        Assert.IsNull(options.Smtp.Host);
        Assert.AreEqual(25, options.Smtp.Port);
        Assert.IsFalse(options.Smtp.HasCredentials);
        Assert.AreEqual(SecureSocketOptions.None, SmtpEmailSender.SocketOptions(options.Smtp));
        Assert.AreEqual("no-reply@fitnesscenter.invalid", options.From.Address);
        Assert.AreEqual("FitnessCenter", options.From.Name);
    }

    [TestMethod]
    public void All_settings_bind_from_the_email_section()
    {
        var options = Bind(new()
        {
            ["Email:Smtp:Host"] = "smtp.example.com",
            ["Email:Smtp:Port"] = "587",
            ["Email:Smtp:Username"] = "test-user",
            ["Email:Smtp:Password"] = "test-password-placeholder",
            ["Email:Smtp:UseTls"] = "true",
            ["Email:From:Address"] = "sender@example.com",
            ["Email:From:Name"] = "Test Center",
        });

        Assert.AreEqual("smtp.example.com", options.Smtp.Host);
        Assert.AreEqual(587, options.Smtp.Port);
        Assert.AreEqual("test-user", options.Smtp.Username);
        Assert.AreEqual("test-password-placeholder", options.Smtp.Password);
        Assert.IsTrue(options.Smtp.UseTls);
        Assert.AreEqual("sender@example.com", options.From.Address);
        Assert.AreEqual("Test Center", options.From.Name);
    }

    [TestMethod]
    public void The_configured_sender_is_the_from_address_of_the_email()
    {
        var from = new EmailOptions.FromOptions { Address = "sender@example.com", Name = "Test Center" };

        using var message = SmtpEmailSender.CreateMessage(from, "member@example.com", "Subject", "Body");

        Assert.AreEqual("\"Test Center\" <sender@example.com>", message.From.ToString());
        Assert.AreEqual("member@example.com", message.To.ToString());
    }

    [TestMethod]
    public void Use_tls_requires_starttls()
    {
        var smtp = new EmailOptions.SmtpOptions { Host = "smtp.example.com", UseTls = true };

        Assert.AreEqual(SecureSocketOptions.StartTls, SmtpEmailSender.SocketOptions(smtp));
    }

    [TestMethod]
    public void A_login_always_uses_tls_even_if_use_tls_is_off()
    {
        var smtp = new EmailOptions.SmtpOptions
        {
            Host = "smtp.example.com",
            Username = "test-user",
            Password = "test-password-placeholder",
            UseTls = false,
        };

        Assert.AreEqual(SecureSocketOptions.StartTls, SmtpEmailSender.SocketOptions(smtp));
        var credentials = SmtpEmailSender.Credentials(smtp);
        Assert.AreEqual("test-user", credentials.UserName);
    }

    [TestMethod]
    [DataRow("test-user", null)]
    [DataRow(null, "test-password-placeholder")]
    public void Half_a_login_is_refused_without_revealing_the_value(string? username, string? password)
    {
        var smtp = new EmailOptions.SmtpOptions { Host = "smtp.example.com", Username = username, Password = password };

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => SmtpEmailSender.Credentials(smtp));

        Assert.DoesNotContain("test-user", error.Message);
        Assert.DoesNotContain("test-password-placeholder", error.Message);
    }
}
