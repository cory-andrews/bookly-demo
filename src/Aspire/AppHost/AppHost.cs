var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("SqlServer")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var database = sqlServer.AddDatabase("Database");

var api = builder.AddProject<Projects.Bookly_Api>("API")
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

var apiMigrations = api.AddEFMigrations("API-DatabaseMigrations")
    .RunDatabaseUpdateOnStart();

api.WaitForCompletion(apiMigrations);

builder.AddViteApp("Frontend", "../../Frontend")
    .WithReference(api)
    .WaitFor(api);

await builder.Build().RunAsync();
