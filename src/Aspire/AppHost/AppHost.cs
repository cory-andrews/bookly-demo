var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddSqlServer("Database")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddProject<Projects.Bookly_Api>("API")
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Scalar (HTTPS)";
        url.Url = "/scalar";
    })
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar (HTTP)";
        url.Url = "/scalar";
    })
    .WithReference(database)
    .WaitFor(database);

await builder.Build().RunAsync();
