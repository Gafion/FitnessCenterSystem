using System.Text.Json.Serialization;
using FitnessCenter.Api;
using FitnessCenter.Api.Database;
using Microsoft.AspNetCore.HttpOverrides;
using FitnessCenter.Modules.Access;
using FitnessCenter.Modules.Booking;
using FitnessCenter.Modules.Identity;
using FitnessCenter.Modules.Identity.Contracts;
using FitnessCenter.Modules.Members;
using NodaTime;

var builder = WebApplication.CreateBuilder(args);

// Service discovery, resilience, health checks and OpenTelemetry (Extensions.cs).
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Numbers are JSON numbers only. The default also accepts strings, which makes the
// OpenAPI document type every number as "integer | string".
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

builder.Services.AddFitnessCenterDatabase(builder.Configuration);

// Domain code reads time only from this clock (rule 2); tests replace it with a fake.
builder.Services.AddSingleton<IClock>(SystemClock.Instance);

// Modules (D-008, D-031).
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddMembersModule();
builder.Services.AddBookingModule();
builder.Services.AddAccessModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // The Vite dev server proxies /api from the browser's address; Google login needs
    // that address, not the API's own. Only loopback proxies are trusted (the default).
    // Production hosting, and any proxy in front of it, is still open (SA4).
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto,
        AllowedHosts = ["localhost"],
    });

    // Production migrations wait for the deployment decision (SA4).
    await app.Services.MigrateDatabaseAsync();

    // One synthetic staff account per role (D-049), when a development password is set.
    // The integration tests turn this off, except the ones that test the staff policies.
    AccountId? developmentInstructor = null;
    if (app.Configuration.GetValue("Identity:SeedDevelopmentStaff", defaultValue: true))
    {
        developmentInstructor = await app.Services.SeedDevelopmentStaffAsync(CancellationToken.None);
    }

    // Synthetic classes to click through locally (D-022, D-039), taught by the development
    // instructor (D-060), so the staff come first. The integration tests turn this off: they
    // schedule their own classes, and some replace the clock with one that fails.
    if (app.Configuration.GetValue("Booking:SeedDevelopmentSchedule", defaultValue: true))
    {
        await app.Services.SeedDevelopmentScheduleAsync(developmentInstructor, CancellationToken.None);
    }

    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();
app.MapMembersEndpoints();
app.MapBookingEndpoints();
app.MapAccessEndpoints();
app.MapSimulationEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program;
