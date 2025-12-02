using AspNetCoreRateLimit;
using Microsoft.SemanticKernel;
using PostgresNaturalLanguageMcp.Endpoints;
using PostgresNaturalLanguageMcp.Models;
using PostgresNaturalLanguageMcp.Services;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json.Serialization;

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

// Configure JSON serialization for Minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

// Configure options
builder.Services.Configure<PostgresOptions>(
    builder.Configuration.GetSection(PostgresOptions.SectionName));
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<LoggingOptions>(
    builder.Configuration.GetSection(LoggingOptions.SectionName));

// Configure Semantic Kernel with AI
var aiOptions = builder.Configuration.GetSection(AiOptions.SectionName).Get<AiOptions>();
if (aiOptions?.Enabled == true)
{
    var kernelBuilder = Kernel.CreateBuilder();
    var provider = aiOptions.Provider.ToLowerInvariant();

    try
    {
        switch (provider)
        {
            case "openai":
                if (string.IsNullOrEmpty(aiOptions.ApiKey))
                {
                    throw new InvalidOperationException("OpenAI API key is required");
                }
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: aiOptions.Model,
                    apiKey: aiOptions.ApiKey);
                Log.Information("AI features enabled with OpenAI model: {Model}", aiOptions.Model);
                break;

            case "azureopenai":
            case "azure":
                if (string.IsNullOrEmpty(aiOptions.ApiKey))
                {
                    throw new InvalidOperationException("Azure OpenAI API key is required");
                }
                if (string.IsNullOrEmpty(aiOptions.AzureEndpoint))
                {
                    throw new InvalidOperationException("Azure OpenAI endpoint is required");
                }
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: aiOptions.AzureDeploymentName ?? aiOptions.Model,
                    endpoint: aiOptions.AzureEndpoint,
                    apiKey: aiOptions.ApiKey);
                Log.Information("AI features enabled with Azure OpenAI deployment: {Deployment}",
                    aiOptions.AzureDeploymentName ?? aiOptions.Model);
                break;

            case "anthropic":
            case "claude":
                if (string.IsNullOrEmpty(aiOptions.ApiKey))
                {
                    throw new InvalidOperationException("Anthropic API key is required");
                }
                // Note: Anthropic connector uses OpenAI-compatible interface through proxy
                // For direct Anthropic API support, we need to use HTTP client
                kernelBuilder.Services.AddHttpClient("AnthropicClient", client =>
                {
                    client.BaseAddress = new Uri("https://api.anthropic.com/v1/");
                    client.DefaultRequestHeaders.Add("x-api-key", aiOptions.ApiKey);
                    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
                });
                // Use OpenAI connector as fallback (will need custom implementation for full support)
                Log.Warning("Anthropic provider is configured but requires custom implementation. Using OpenAI-compatible mode.");
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: aiOptions.Model,
                    apiKey: aiOptions.ApiKey);
                break;

            case "gemini":
            case "google":
                if (string.IsNullOrEmpty(aiOptions.ApiKey))
                {
                    throw new InvalidOperationException("Google Gemini API key is required");
                }
                kernelBuilder.AddGoogleAIGeminiChatCompletion(
                    modelId: aiOptions.Model,
                    apiKey: aiOptions.ApiKey);
                Log.Information("AI features enabled with Google Gemini model: {Model}", aiOptions.Model);
                break;

            case "ollama":
                var ollamaUrl = aiOptions.BaseUrl ?? "http://localhost:11434";
                kernelBuilder.AddOllamaChatCompletion(
                    modelId: aiOptions.Model,
                    endpoint: new Uri(ollamaUrl));
                Log.Information("AI features enabled with Ollama model: {Model} at {Endpoint}",
                    aiOptions.Model, ollamaUrl);
                break;

            case "lmstudio":
            case "lm-studio":
                var lmStudioUrl = aiOptions.BaseUrl ?? "http://localhost:1234";
                // LM Studio uses OpenAI-compatible API
                kernelBuilder.AddOpenAIChatCompletion(
                    modelId: aiOptions.Model,
                    apiKey: "lm-studio", // LM Studio doesn't require a real API key
                    endpoint: new Uri($"{lmStudioUrl.TrimEnd('/')}/v1"));
                Log.Information("AI features enabled with LM Studio model: {Model} at {Endpoint}",
                    aiOptions.Model, lmStudioUrl);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported AI provider: {provider}. " +
                    "Supported providers: openai, azureopenai, anthropic, gemini, ollama, lmstudio");
        }

        var kernel = kernelBuilder.Build();
        builder.Services.AddSingleton(kernel);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to configure AI provider: {Provider}", provider);
        Log.Warning("AI features are disabled due to configuration error");
        builder.Services.AddSingleton<Kernel>(_ => null!);
    }
}
else
{
    Log.Warning("AI features are disabled or not configured");
    builder.Services.AddSingleton<Kernel>(_ => null!);
}

// Register application services
builder.Services.AddSingleton<IConnectionBuilderService, ConnectionBuilderService>();
builder.Services.AddScoped<IDatabaseSchemaService, DatabaseSchemaService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<ISqlGenerationService, SqlGenerationService>();

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

// Configure OpenAPI
builder.Services.AddOpenApi();

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
app.MapOpenApi();
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

// Map MCP Minimal API endpoints
app.MapMcpEndpoints();

app.MapHealthChecks("/health");

// Root endpoint with API information
app.MapGet("/", () => Results.Json(new
{
    name = "PostgreSQL MCP Server",
    version = "1.0.0",
    description = "Model Context Protocol server for PostgreSQL database operations",
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

Log.Information("Starting PostgreSQL MCP Server");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to tests
public partial class Program { }
