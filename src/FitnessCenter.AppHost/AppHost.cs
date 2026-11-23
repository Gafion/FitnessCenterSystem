var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL database, one schema per module (D-014). Data survives restarts in a Docker volume.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("fitnesscenter");

// A local test mailbox (D-038): it catches every email the API sends, and its web page
// lists them. Nothing reaches a real inbox. The real email provider is still open.
var mailpit = builder.AddContainer("mailpit", "docker.io/axllent/mailpit", "v1.33.1")
    .WithHttpEndpoint(targetPort: 8025, name: "http")
    .WithEndpoint(targetPort: 1025, name: "smtp", scheme: "tcp");
var smtp = mailpit.GetEndpoint("smtp");

var api = builder.AddDotnetProject("api", "../FitnessCenter.Api/FitnessCenter.Api.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Email__Smtp__Host", smtp.Property(EndpointProperty.Host))
    .WithEnvironment("Email__Smtp__Port", smtp.Property(EndpointProperty.Port))
    .WaitFor(mailpit)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// A fixed port, because Google's redirect URI must match exactly (FIT-51).
var webfrontend = builder.AddViteApp("webfrontend", "../../frontend")
    .WithBun()
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithReference(api)
    .WaitFor(api);

api.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
