using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL database, one schema per module (D-014). Data survives restarts in a Docker volume.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("fitnesscenter");

// A local test mailbox (D-038): it catches every email the API sends, and its web page
// lists them. Nothing reaches a real inbox. It is the default. To send real email through
// Resend instead (D-066), set Email:UseMailpit to false in the AppHost's user-secrets and the
// Email:Smtp and Email:From settings in the API's user-secrets: environment variables would
// override those, so the AppHost then leaves Email:Smtp alone.
var mailpit = builder.AddContainer("mailpit", "docker.io/axllent/mailpit", "v1.33.1")
    .WithHttpEndpoint(targetPort: 8025, name: "http")
    .WithEndpoint(targetPort: 1025, name: "smtp", scheme: "tcp");
var smtp = mailpit.GetEndpoint("smtp");

var useMailpit = builder.Configuration.GetValue("Email:UseMailpit", defaultValue: true);

var api = builder.AddDotnetProject("api", "../FitnessCenter.Api/FitnessCenter.Api.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

if (useMailpit)
{
    api.WithEnvironment("Email__Smtp__Host", smtp.Property(EndpointProperty.Host))
        .WithEnvironment("Email__Smtp__Port", smtp.Property(EndpointProperty.Port))
        .WaitFor(mailpit);
}

// A fixed port, because Google's redirect URI must match exactly (FIT-51).
var webfrontend = builder.AddViteApp("webfrontend", "../../frontend")
    .WithBun()
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithReference(api)
    .WaitFor(api);

api.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
