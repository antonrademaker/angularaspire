var builder = DistributedApplication.CreateBuilder(args);

// Register PublicApi and PrivateApi projects
var publicApi = builder.AddProject<Projects.PublicApi>("publicapi");
var privateApi = builder.AddProject<Projects.PrivateApi>("privateapi");

builder.Build().Run();
