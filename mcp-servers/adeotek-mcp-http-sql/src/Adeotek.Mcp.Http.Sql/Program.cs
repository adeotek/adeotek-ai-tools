using Adeotek.Mcp.Http.Sql.Extensions;
using Adeotek.Mcp.Http.Sql.Models;
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
            .Get<DatabaseServerOptions>();
        // Register database services based on the selected database server type

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
