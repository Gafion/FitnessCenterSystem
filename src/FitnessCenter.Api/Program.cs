using System.Text.Json.Serialization;
using FitnessCenter.Api.Database;
using FitnessCenter.Modules.Identity;
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
builder.Services.AddIdentityModule();
builder.Services.AddMembersModule();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Production migrations wait for the deployment decision (SA4).
    await app.Services.MigrateDatabaseAsync();
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();
app.MapMembersEndpoints();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program;
