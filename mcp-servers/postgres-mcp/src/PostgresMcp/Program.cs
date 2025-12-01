using AspNetCoreRateLimit;
using PostgresMcp.Endpoints;
using PostgresMcp.Models;
using PostgresMcp.Services;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: builder.Configuration["Logging:LogFilePath"] ?? "logs/postgres-mcp-.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configure options
builder.Services.Configure<PostgresOptions>(
    builder.Configuration.GetSection(PostgresOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<LoggingOptions>(
    builder.Configuration.GetSection(LoggingOptions.SectionName));

// Register application services
builder.Services.AddScoped<IDatabaseSchemaService, DatabaseSchemaService>();
builder.Services.AddScoped<IQueryService, QueryService>();

// Configure rate limiting
var securityOptions = builder.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>();
if (securityOptions?.EnableRateLimiting == true)
{
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = 429;
        options.RealIpHeader = "X-Real-IP";
        options.ClientIdHeader = "X-ClientId";
        options.GeneralRules =
        [
            new RateLimitRule
            {
                Endpoint = "*",
                Period = "1m",
                Limit = securityOptions.RequestsPerMinute
            }
        ];
    });

    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

    Log.Information("Rate limiting enabled: {RequestsPerMinute} requests per minute", securityOptions.RequestsPerMinute);
}

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("PostgreSQL MCP Server")
        .WithTheme(ScalarTheme.Default)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseSerilogRequestLogging();

// Apply rate limiting
if (securityOptions?.EnableRateLimiting == true)
{
    app.UseIpRateLimiting();
}

app.UseCors();

app.UseAuthorization();

// Map MCP endpoints
app.MapMcpEndpoints();

app.MapHealthChecks("/health");

// Root endpoint with API information
app.MapGet("/", () => Results.Json(new
{
    name = "PostgreSQL MCP Server",
    version = "1.0.0",
    description = "Read-only Model Context Protocol server for PostgreSQL database operations",
    capabilities = new
    {
        readOnly = true,
        dataModificationsBlocked = true,
        schemaModificationsBlocked = true
    },
    endpoints = new
    {
        tools = "/mcp/tools",
        call = "/mcp/tools/call",
        jsonrpc = "/mcp/jsonrpc",
        health = "/health",
        documentation = "/scalar/v1"
    },
    documentation = "/scalar/v1"
}));

Log.Information("Starting PostgreSQL MCP Server (Read-Only)");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
