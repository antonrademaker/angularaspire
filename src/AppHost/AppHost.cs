var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database for persistent storage
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var eventDb = postgres.AddDatabase("eventdb");

// Add Redis for queue management and caching
var redis = builder.AddRedis("redis");

// Add Azure Blob Storage emulator for file storage
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobStorage = storage.AddBlobs("blobs");

// Register PublicApi and PrivateApi projects with infrastructure dependencies
var publicApi = builder.AddProject<Projects.PublicApi>("publicapi")
    .WithReference(eventDb)
    .WithReference(redis)
    .WithReference(blobStorage);

var privateApi = builder.AddProject<Projects.PrivateApi>("privateapi")
    .WithReference(eventDb)
    .WithReference(redis)
    .WithReference(blobStorage);

builder.Build().Run();
