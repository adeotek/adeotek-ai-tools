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

        // Add Open API support
        builder.Services.AddOpenApi();

        // Add health checks
        builder.Services.AddHealthChecks();
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
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        });

        app.MapHealthChecks("/_health");
    },
    app =>
    {
        // Apply rate limiting
        var securityOptions = app.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>();
        if (securityOptions?.EnableRateLimiting == true)
        {
            app.UseIpRateLimiting();
        }

        app.UseCors();
        // app.UseAuthorization();

        // Map MCP endpoints
        app
            .MapRootEndpoint()
            .MapMcpEndpoints();
    });
