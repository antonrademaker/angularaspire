var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL container for primary data storage
var postgres = builder.AddPostgreSQL("postgres")
    .WithImage("postgres", "17")
    .WithDatabase("eventmanagement");

// Add Redis container for queue management and caching  
var redis = builder.AddRedis("redis")
    .WithImage("redis", "7");

// Add Azure Blob Storage emulator for file storage
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator()
    .AddBlobs("blobs");

builder.Build().Run();
