using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;

using DeviceManager.Server.Accessor;
using DeviceManager.Server.Web;
using DeviceManager.Server.Web.Application;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting.WindowsServices;

using MudBlazor.Services;

using Prometheus;

using Serilog;

using Smart.AspNetCore;
using Smart.AspNetCore.ApplicationModels;
using Smart.AspNetCore.Filters;
using Smart.Data;
using Smart.Data.Accessor;
using Smart.Data.Accessor.Extensions.DependencyInjection;

#pragma warning disable CA1852

//--------------------------------------------------------------------------------
// Configure builder
//--------------------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
});

// Service
builder.Host
    .UseWindowsService()
    .UseSystemd();

// Add framework Services.
builder.Services.AddHttpContextAccessor();

// Log
builder.Logging.ClearProviders();
builder.Services.AddSerilog(option =>
{
    option.ReadFrom.Configuration(builder.Configuration);
});

// Route
builder.Services.Configure<RouteOptions>(static options =>
{
    options.AppendTrailingSlash = true;
});

// Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddMudServices(config =>
{
    // Snackbar
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 3000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Outlined;
});

builder.Services.AddSingleton<IErrorBoundaryLogger, ErrorBoundaryLogger>();

// API
builder.Services.AddExceptionLogging();
builder.Services.AddTimeLogging(static options =>
{
    options.Threshold = 10_000;
});
builder.Services.AddSingleton<ExceptionStatusFilter>();

builder.Services
    .AddControllers(static options =>
    {
        options.Filters.AddExceptionLogging();
        options.Filters.AddTimeLogging();
        options.Filters.AddService<ExceptionStatusFilter>();
        options.Conventions.Add(new LowercaseControllerModelConvention());
    })
    .AddJsonOptions(static options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        options.JsonSerializerOptions.Converters.Add(new DeviceManager.Server.Components.Json.DateTimeConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// Validation
ValidatorOptions.Global
    .UseDisplayName()
    .UseCustomLocalizeMessage();
ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Continue;
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

// Data
var connectionStringBuilder = new SqliteConnectionStringBuilder
{
    DataSource = "Data.db",
    Pooling = true,
    Cache = SqliteCacheMode.Shared
};
var connectionString = connectionStringBuilder.ConnectionString;
builder.Services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
builder.Services.AddSingleton<IDialect>(new DelegateDialect(
    static ex => ex is SqliteException { SqliteErrorCode: 1555 },
    static x => Regex.Replace(x, "[%_]", "[$0]")));
builder.Services.AddDataAccessor();

// Mapper
builder.Services.AddSingleton<IMapper>(new Mapper(new MapperConfiguration(c =>
{
    c.AddProfile<MappingProfile>();
})));

// Service
builder.Services.AddSingleton<DataService>();

// Health
builder.Services.AddHealthChecks();

// Develop
if (!builder.Environment.IsProduction())
{
    // Swagger
    builder.Services.AddSwaggerGen();
}

//--------------------------------------------------------------------------------
// Configure the HTTP request pipeline
//--------------------------------------------------------------------------------
var app = builder.Build();

// Startup information
ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
app.Logger.InfoServiceStart();
app.Logger.InfoServiceSettingsEnvironment(typeof(Program).Assembly.GetName().Version, Environment.Version, Environment.CurrentDirectory);
app.Logger.InfoServiceSettingsGC(GCSettings.IsServerGC, GCSettings.LatencyMode, GCSettings.LargeObjectHeapCompactionMode);
app.Logger.InfoServiceSettingsThreadPool(workerThreads, completionPortThreads);

// Prepare
if (!File.Exists(connectionStringBuilder.DataSource))
{
    var accessor = app.Services.GetRequiredService<IAccessorResolver<IDataAccessor>>().Accessor;
    accessor.Create();
}

// Serilog
if (!app.Environment.IsProduction())
{
    app.UseSerilogRequestLogging(static options =>
    {
        options.IncludeQueryInRequestPath = true;
    });
}

// Forwarded headers
app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// HTTPS redirection
app.UseHttpsRedirection();

// Health
app.UseHealthChecks("/health");

// Metrics
app.UseHttpMetrics();

// Develop
if (app.Environment.IsDevelopment())
{
    // Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Static files
app.UseStaticFiles();

// Routing
app.UseRouting();

// API
app.MapControllers();

// Metrics
app.MapMetrics();

// Blazor
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Run
app.Run();
