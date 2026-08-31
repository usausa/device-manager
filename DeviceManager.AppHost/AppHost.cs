// ReSharper disable StringLiteralTypo
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DeviceManager_Server>("devicemanager")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
