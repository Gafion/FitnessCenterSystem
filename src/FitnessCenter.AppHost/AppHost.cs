var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddDotnetProject("api", "../FitnessCenter.Api/FitnessCenter.Api.csproj")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../../frontend")
    .WithBun()
    .WithReference(api)
    .WaitFor(api);

api.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
