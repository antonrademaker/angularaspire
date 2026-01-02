var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database for persistent storage
// WithLifetime(Persistent) keeps it running between debug sessions
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var eventDb = postgres.AddDatabase("eventdb");

// Add Redis for queue management and caching
// WithLifetime(Persistent) keeps it running between debug sessions
var redis = builder.AddRedis("redis")
    .WithLifetime(ContainerLifetime.Persistent);

// Add Azure Blob Storage emulator for file storage
// WithLifetime(Persistent) keeps it running between debug sessions
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Persistent));

var blobStorage = storage.AddBlobs("blobs");

// Register PublicApi and PrivateApi projects with infrastructure dependencies
// WaitFor ensures dependencies are healthy before starting the APIs
var publicApi = builder.AddProject<Projects.PublicApi>("publicapi")
    .WithReference(eventDb)
    .WaitFor(eventDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(blobStorage)
    .WaitFor(blobStorage);

var privateApi = builder.AddProject<Projects.PrivateApi>("privateapi")
    .WithReference(eventDb)
    .WaitFor(eventDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(blobStorage)
    .WaitFor(blobStorage);

builder.Build().Run();
