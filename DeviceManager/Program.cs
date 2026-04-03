using DeviceManager.Components;
using DeviceManager.Hubs;
using DeviceManager.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Kestrel: support both HTTP/1.1 (Blazor/REST) and HTTP/2 (gRPC)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5178, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenLocalhost(7400, o =>
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

// MudBlazor
builder.Services.AddMudServices();

// SignalR
builder.Services.AddSignalR();

// gRPC
builder.Services.AddGrpc();
builder.Services.AddSingleton<GrpcEventDispatcher>();

// API Controllers
builder.Services.AddControllers();

// DeviceManager Core Services
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "Data");
builder.Services.AddDeviceManagerCore(dataDirectory);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapStaticAssets();

// API controllers
app.MapControllers();

// gRPC Service
app.MapGrpcService<DeviceGrpcService>();

// SignalR Hub
app.MapHub<DeviceHub>(HubConstants.DeviceHubPath);

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
