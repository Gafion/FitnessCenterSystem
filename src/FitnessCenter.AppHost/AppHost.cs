var builder = DistributedApplication.CreateBuilder(args);

// One PostgreSQL database, one schema per module (D-014). Data survives restarts in a Docker volume.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();
var database = postgres.AddDatabase("fitnesscenter");

var api = builder.AddDotnetProject("api", "../FitnessCenter.Api/FitnessCenter.Api.csproj")
    .WithReference(database)
    .WaitFor(database)
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
