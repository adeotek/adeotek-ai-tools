using AspNetCoreRateLimit;
using PostgresMcp.Endpoints;
using PostgresMcp.Extensions;
using PostgresMcp.Models;
using PostgresMcp.Services;
using Scalar.AspNetCore;
using Serilog;

await HostBuilderExtensions.WebApplicationRunAsync(args,
    builder =>
    {
        // Configure options
        builder.Services.Configure<PostgresOptions>(
            builder.Configuration.GetSection(PostgresOptions.SectionName));
        builder.Services.Configure<SecurityOptions>(
            builder.Configuration.GetSection(SecurityOptions.SectionName));
        builder.Services.Configure<McpLoggingOptions>(
            builder.Configuration.GetSection(McpLoggingOptions.SectionName));

        // Register application services
        builder.Services.AddSingleton<IConnectionBuilderService, ConnectionBuilderService>();
        builder.Services.AddScoped<IDatabaseSchemaService, DatabaseSchemaService>();
        builder.Services.AddScoped<IQueryService, QueryService>();

        // Register MCP protocol services
        builder.Services.AddSingleton<ISseNotificationService, SseNotificationService>();
        builder.Services.AddScoped<IResourceProvider, ResourceProvider>();
        builder.Services.AddSingleton<IPromptProvider, PromptProvider>();

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

        // Configure CORS for browser-based MCP clients
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("Content-Type");
            });
        });

        // Add OpenAPI support
        builder.Services.AddOpenApi();

        // Add health checks
        builder.Services.AddHealthChecks();

        Log.Information("PostgreSQL MCP Server configured with MCP Protocol v2024-11-05");
    },
    app =>
    {
        // Configure the HTTP request pipeline
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("PostgreSQL MCP Server")
                .WithTheme(ScalarTheme.Default)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.NetHttp);
        });

        // Health checks endpoint
        app.MapHealthChecks("/_health");

        Log.Information("API documentation available at /scalar/v1");
    },
    app =>
    {
        // Apply rate limiting middleware
        var securityOptions = app.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>();
        if (securityOptions?.EnableRateLimiting == true)
        {
            app.UseIpRateLimiting();
        }

        // Apply CORS middleware
        app.UseCors();

        // Map MCP protocol endpoints
        app.MapMcpProtocolEndpoints();

        Log.Information("MCP Server ready");
        Log.Information("- Main JSON-RPC endpoint: POST /mcp/v1/messages");
        Log.Information("- SSE notifications: GET /mcp/v1/sse");
        Log.Information("- Discovery: GET /.well-known/mcp.json");
        Log.Information("- Health check: GET /_health");
    });
