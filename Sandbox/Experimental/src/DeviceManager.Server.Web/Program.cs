using DeviceManager.Server.Core;
using DeviceManager.Server.Web.Components;
using DeviceManager.Server.Web.Hubs;
using DeviceManager.Server.Web.Services;
using DeviceManager.Shared;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to support both HTTP/1.1 (Blazor/REST) and HTTP/2 (gRPC)
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP port - HTTP/1.1 + HTTP/2 (cleartext for dev)
    options.ListenLocalhost(5178, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    // HTTPS port - HTTP/1.1 + HTTP/2
    options.ListenLocalhost(7125, o =>
    {
        o.Protocols = HttpProtocols.Http1AndHttp2;
        o.UseHttps();
    });
});

// Serilog
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR
builder.Services.AddSignalR();

// gRPC
builder.Services.AddGrpc();
builder.Services.AddSingleton<GrpcEventDispatcher>();

// API Controllers
builder.Services.AddControllers();

// OpenAPI
builder.Services.AddOpenApi();

// DeviceManager Core Services
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
builder.Services.AddDeviceManagerCore(dataDirectory);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// OpenAPI endpoint
app.MapOpenApi();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAntiforgery();

app.MapStaticAssets();

// Map API controllers
app.MapControllers();

// Map gRPC Service
app.MapGrpcService<DeviceGrpcService>();

// Map SignalR Hub
app.MapHub<DeviceHub>(HubConstants.DeviceHubPath);

// Map Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
