using Adeotek.Mcp.Http.Sql.Extensions;
using Adeotek.Mcp.Http.Sql.Models;
using Adeotek.Mcp.Http.Sql.Services;
using Serilog;

await HostBuilderExtensions.WebApplicationRunAsync(args,
    builder =>
    {
        // Configure options
        builder.Services.Configure<DatabaseServerOptions>(
            builder.Configuration.GetSection(DatabaseServerOptions.SectionName));

        builder.AddServiceDefaults();

        var dbServerType = builder.Configuration
            .GetSection(DatabaseServerOptions.SectionName)
            .Get<DatabaseServerOptions>()
            ?.DbServerType;
        // Register database services based on the selected database server type
        switch (dbServerType)
        {
            case DatabaseServerType.Postgres:
                builder.Services.AddScoped<ISqlQueryService, PostgresQueryService>();
                break;
            case DatabaseServerType.MsSql:
                builder.Services.AddScoped<ISqlQueryService, MsSqlQueryService>();
                break;
            default:
                throw new NotSupportedException($"Unsupported database server type: {dbServerType}");
        }

        builder.Services.AddMcpServer(mcpOptions =>
            {
                Log.Information("Adeotek.Mcp.Http.Sql MCP Protocol Version: multi-version (latest: {McpProtocolVersion}", mcpOptions.ProtocolVersion);
            })
            .WithHttpTransport()
            .WithToolsFromAssembly();
    },
    configureAfterHttpsRedirectionMiddlewares: app =>
    {
        app.MapDefaultEndpoints();

        app.MapMcp();
    });
