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

var webfrontend = builder.AddViteApp("webfrontend", "../../frontend")
    .WithBun()
    .WithReference(api)
    .WaitFor(api);

api.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
