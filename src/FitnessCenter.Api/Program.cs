using FitnessCenter.Api.Database;
using FitnessCenter.Modules.Identity;
using FitnessCenter.Modules.Members;

var builder = WebApplication.CreateBuilder(args);

// Service discovery, resilience, health checks and OpenTelemetry (Extensions.cs).
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddFitnessCenterDatabase(builder.Configuration);

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
