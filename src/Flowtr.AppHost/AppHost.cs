var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var database = sql.AddDatabase("flowtr");

var blobs = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent))
    .AddBlobs("blobs");

builder.AddProject<Projects.Flowtr_Server>("server")
    .WithReference(database)
    .WithReference(blobs)
    .WaitFor(database)
    .WaitFor(blobs);

builder.Build().Run();
