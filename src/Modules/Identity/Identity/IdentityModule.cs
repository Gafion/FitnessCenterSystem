using FitnessCenter.Modules.Identity.Application;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Identity.Endpoints;
using FitnessCenter.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace FitnessCenter.Modules.Identity;

/// <summary>
/// Entry point of the Identity module. The API host calls these methods and
/// nothing else in this project; everything else stays <c>internal</c>.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        // The shared, request-scoped connection lets one transaction span modules (D-015).
        services.AddDbContext<IdentityDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlConnection>(), npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", IdentityDbContext.Schema).UseNodaTime()));

        var authentication = services.AddAuthentication(IdentityConstants.ApplicationScheme);
        authentication.AddIdentityCookies();
        AddGoogle(authentication, configuration.GetSection("Authentication:Google"));
        services.AddAuthorization(AddStaffPolicies);

        services.AddIdentityCore<Account>(options =>
            {
                // ASVS L1 6.2.1 and 6.2.5: at least 8 characters, no composition rules
                // (research/authentication.md, D-035).
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;

                // D-038: no login until the email is confirmed. Google accounts are created
                // confirmed, because Google verified the address.
                options.SignIn.RequireConfirmedEmail = true;
            })
            // Roles go into the login cookie as role claims, which the staff policies check (D-049).
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "fc.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

            // The SPA expects status codes, not redirects to a login page.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<StaffInvites>();
        services.AddScoped<IAuthorizationHandler, AuthenticatorSetUpHandler>();
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<IEmailSender<Account>>(provider => provider.GetRequiredService<SmtpEmailSender>());
        services.AddHealthChecks().AddDbContextCheck<IdentityDbContext>("identity-db");

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapAccountEndpoints().MapExternalLoginEndpoints().MapStaffInviteEndpoints().MapStaffEndpoints()
            .MapAuthenticatorEndpoints();
    }

    /// <summary>
    /// One policy per staff role, plus one for any staff role (D-049). Members match none.
    /// Roles stack (D-062): a role's policy also admits the roles above it, so the Manager
    /// policy admits Admin, and the FrontDesk policy admits Manager and Admin. Instructor
    /// stands apart. Every staff policy also needs the authenticator app set up (D-013, D-059).
    /// </summary>
    private static void AddStaffPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(StaffPolicies.Admin, policy => RolePolicy(policy, StaffRoles.Admin));
        options.AddPolicy(StaffPolicies.Manager, policy => RolePolicy(policy, StaffRoles.Manager));
        options.AddPolicy(StaffPolicies.FrontDesk, policy => RolePolicy(policy, StaffRoles.FrontDesk));
        options.AddPolicy(StaffPolicies.Instructor, policy => RolePolicy(policy, StaffRoles.Instructor));
        options.AddPolicy(StaffPolicies.AnyStaff, policy => StaffPolicy(policy, [.. StaffRoles.All]));
    }

    private static void RolePolicy(AuthorizationPolicyBuilder policy, string role) =>
        StaffPolicy(policy, [.. StaffRoleRules.AdmittedBy(role)]);

    private static void StaffPolicy(AuthorizationPolicyBuilder policy, params string[] roles) =>
        policy.RequireRole(roles).AddRequirements(new AuthenticatorSetUpRequirement());

    /// <summary>
    /// Google login (D-011), only when a client is configured: locally from user-secrets
    /// (FIT-51). Without one, the API runs with email/password login only, as in CI.
    /// </summary>
    private static void AddGoogle(AuthenticationBuilder authentication, IConfiguration google)
    {
        var clientId = google["ClientId"];
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        authentication.AddGoogle(ExternalLogins.Google, options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = google["ClientSecret"]
                ?? throw new InvalidOperationException("Authentication:Google:ClientSecret is missing.");
            options.SignInScheme = IdentityConstants.ExternalScheme;

            // Registered in the Google console (FIT-51). Under /api so it goes through the
            // frontend's proxy and the cookie lands on the address the member uses.
            options.CallbackPath = "/api/identity/signin-google";
            options.ClaimActions.MapJsonKey(ExternalLogins.EmailVerifiedClaim, "email_verified");
        });
    }

    /// <summary>
    /// One synthetic staff account per role, for local development only (D-049). Needs a
    /// password in <c>Identity:DevelopmentStaffPassword</c> (user-secrets); without one it logs
    /// how to set it and seeds nothing. With a key in <c>Identity:DevelopmentStaffAuthenticatorKey</c>,
    /// the accounts also get the authenticator app turned on with that key (FIT-109).
    /// </summary>
    /// <returns>The development instructor's account when it exists, for the development classes (D-060).</returns>
    public static async Task<AccountId?> SeedDevelopmentStaffAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var users = provider.GetRequiredService<UserManager<Account>>();
        await DevelopmentStaff.EnsureAsync(
            users,
            provider.GetRequiredService<IUserStore<Account>>(),
            provider.GetRequiredService<IConfiguration>()[DevelopmentStaff.PasswordKey],
            provider.GetRequiredService<IConfiguration>()[DevelopmentStaff.AuthenticatorKeyKey],
            provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevelopmentStaff)));
        return await users.FindByEmailAsync(DevelopmentStaff.InstructorEmail) is { } instructor
            ? new AccountId(instructor.Id)
            : null;
    }

    /// <summary>Applies this module's migrations. The host calls it at startup in Development.</summary>
    public static async Task MigrateIdentityDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
