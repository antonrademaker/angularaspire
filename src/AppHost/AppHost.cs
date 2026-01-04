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

// Add Angular applications
// PublicApp - public-facing Angular frontend on port 4200
var publicApp = builder.AddNpmApp("publicapp", "../PublicApp", "start")
    .WithReference(publicApi)
    .WaitFor(publicApi)
    .WithHttpEndpoint(port: 4200, env: "PORT")
    .WithExternalHttpEndpoints();

// PrivateApp - admin Angular frontend on port 4201
var privateApp = builder.AddNpmApp("privateapp", "../PrivateApp", "start")
    .WithReference(privateApi)
    .WaitFor(privateApi)
    .WithHttpEndpoint(port: 4201, targetPort: 4201, env: "PORT", isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
