using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PostgresMcp.Models;
using PostgresMcp.Services;

namespace PostgresMcp.Endpoints;

/// <summary>
/// MCP (Model Context Protocol) endpoints implementing the full MCP specification.
/// </summary>
public static class McpProtocolEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Maps all MCP protocol endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapMcpProtocolEndpoints(this IEndpointRouteBuilder endpoints)
    {
            // Root endpoint with server information
            endpoints.MapGet("/", GetServerInfo);

            // Well-known discovery endpoint
            endpoints.MapGet("/.well-known/mcp.json", GetDiscoveryInfo);

            // Main JSON-RPC endpoint for MCP protocol
            var mcpGroup = endpoints.MapGroup("/mcp/v1")
                .WithTags("MCP Protocol");

            mcpGroup.MapPost("/messages", HandleJsonRpcRequest)
                .WithName("McpJsonRpcMessages")
                .WithDescription("Main MCP JSON-RPC 2.0 endpoint for all protocol methods")
                .Produces<JsonRpcResponse>()
                .Produces<JsonRpcBatchResponse>();

            // SSE endpoint for server-to-client notifications
            mcpGroup.MapGet("/sse", HandleSseConnection)
                .WithName("McpSseEndpoint")
                .WithDescription("Server-Sent Events endpoint for real-time notifications")
                .Produces(StatusCodes.Status200OK, contentType: "text/event-stream");

            // Legacy compatibility endpoints
            var legacyGroup = endpoints.MapGroup("/mcp")
                .WithTags("Legacy API");

            legacyGroup.MapPost("/initialize", LegacyInitialize)
                .WithName("LegacyInitialize");

            legacyGroup.MapGet("/configuration", LegacyGetConfiguration)
                .WithName("LegacyGetConfiguration");

            legacyGroup.MapGet("/tools", LegacyGetTools)
                .WithName("LegacyGetTools");

            legacyGroup.MapPost("/tools/call", LegacyCallTool)
                .WithName("LegacyCallTool");

            return endpoints;
    }

    #region JSON-RPC Handler

    private static async Task<IResult> HandleJsonRpcRequest(
        HttpContext context,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        IQueryService queryService,
        IResourceProvider resourceProvider,
        IPromptProvider promptProvider,
        ISseNotificationService sseService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(context.Request.Body);
            var requestBody = await reader.ReadToEndAsync(cancellationToken);

            // Check if it's a batch request (starts with '[')
            if (requestBody.TrimStart().StartsWith('['))
            {
                var batchRequests = JsonSerializer.Deserialize<List<JsonRpcRequest>>(requestBody, JsonOptions);
                if (batchRequests == null || batchRequests.Count == 0)
                {
                    return Results.Json(CreateErrorResponse(null, JsonRpcErrorCodes.InvalidRequest,
                        "Invalid batch request"));
                }

                var batchResponses = new List<JsonRpcResponse>();
                foreach (var request in batchRequests)
                {
                    var response = await ProcessJsonRpcRequest(request, connectionBuilder, schemaService,
                        queryService, resourceProvider, promptProvider, sseService, logger, cancellationToken);
                    batchResponses.Add(response);
                }

                return Results.Json(batchResponses);
            }
            else
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(requestBody, JsonOptions);
                if (request == null)
                {
                    return Results.Json(CreateErrorResponse(null, JsonRpcErrorCodes.ParseError,
                        "Failed to parse JSON-RPC request"));
                }

                var response = await ProcessJsonRpcRequest(request, connectionBuilder, schemaService,
                    queryService, resourceProvider, promptProvider, sseService, logger, cancellationToken);

                return Results.Json(response);
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "JSON parsing error");
            return Results.Json(CreateErrorResponse(null, JsonRpcErrorCodes.ParseError,
                "Invalid JSON", ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing JSON-RPC request");
            return Results.Json(CreateErrorResponse(null, JsonRpcErrorCodes.InternalError,
                "Internal server error", ex.Message));
        }
    }

    private static async Task<JsonRpcResponse> ProcessJsonRpcRequest(
        JsonRpcRequest request,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        IQueryService queryService,
        IResourceProvider resourceProvider,
        IPromptProvider promptProvider,
        ISseNotificationService sseService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Processing JSON-RPC method: {Method}", request.Method);

            var result = request.Method switch
            {
                "initialize" => await HandleInitializeAsync(request, connectionBuilder, logger),
                "initialized" => HandleInitialized(request, logger),
                "ping" => HandlePing(),
                "tools/list" => HandleToolsList(),
                "tools/call" => await HandleToolsCallAsync(request, connectionBuilder, schemaService,
                    queryService, logger, cancellationToken),
                "resources/list" => await HandleResourcesListAsync(request, resourceProvider),
                "resources/read" => await HandleResourcesReadAsync(request, resourceProvider),
                "resources/subscribe" => await HandleResourcesSubscribeAsync(request, resourceProvider),
                "resources/unsubscribe" => await HandleResourcesUnsubscribeAsync(request, resourceProvider),
                "prompts/list" => await HandlePromptsListAsync(promptProvider),
                "prompts/get" => await HandlePromptsGetAsync(request, promptProvider),
                _ => throw new InvalidOperationException($"Unknown method: {request.Method}")
            };

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid parameters for method: {Method}", request.Method);
            return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InvalidParams, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Method not found or invalid operation: {Method}", request.Method);
            return CreateErrorResponse(request.Id, JsonRpcErrorCodes.MethodNotFound, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing method: {Method}", request.Method);
            return CreateErrorResponse(request.Id, JsonRpcErrorCodes.InternalError,
                "Internal error", ex.Message);
        }
    }

    #endregion

    #region MCP Protocol Methods

    private static Task<object> HandleInitializeAsync(
        JsonRpcRequest request,
        IConnectionBuilderService connectionBuilder,
        ILogger<Program> logger)
    {
        var initializeParams = DeserializeParams<InitializeParams>(request.Params);

        logger.LogInformation("MCP Server initializing with protocol version: {Version}",
            initializeParams?.ProtocolVersion ?? "unknown");

        var result = new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            ServerInfo = new Implementation
            {
                Name = "PostgreSQL MCP Server",
                Version = "2.0.0"
            },
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = true },
                Resources = new ResourcesCapability
                {
                    Subscribe = true,
                    ListChanged = true
                },
                Prompts = new PromptsCapability { ListChanged = true }
            }
        };

        return Task.FromResult<object>(result);
    }

    private static object HandleInitialized(JsonRpcRequest request, ILogger<Program> logger)
    {
        logger.LogInformation("MCP Server initialized notification received");
        return new { };
    }

    private static object HandlePing()
    {
        return new { };
    }

    private static object HandleToolsList()
    {
        var tools = new List<Tool>
        {
            new()
            {
                Name = "scan_database_structure",
                Description = "Scan and analyze PostgreSQL database structure including tables, columns, indexes, foreign keys, and relationships. This is a read-only operation.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        database = new
                        {
                            type = "string",
                            description = "Database name to scan (e.g., 'mydb', 'production_db')"
                        }
                    },
                    required = new[] { "database" }
                }
            },
            new()
            {
                Name = "query_database",
                Description = "Execute a read-only SELECT query against the PostgreSQL database. Only SELECT queries are allowed - no data or schema modifications permitted.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        database = new
                        {
                            type = "string",
                            description = "Database name to query (e.g., 'mydb', 'production_db')"
                        },
                        query = new
                        {
                            type = "string",
                            description = "SQL SELECT query to execute (must be read-only)"
                        }
                    },
                    required = new[] { "database", "query" }
                }
            }
        };

        return new ListToolsResult { Tools = tools };
    }

    private static async Task<object> HandleToolsCallAsync(
        JsonRpcRequest request,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        IQueryService queryService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var callParams = DeserializeParams<CallToolParams>(request.Params);
        if (callParams == null)
        {
            throw new ArgumentException("Invalid tool call parameters");
        }

        if (!connectionBuilder.IsConfigured)
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new Content
                    {
                        Type = "text",
                        Text = "Server not configured. Call initialize with PostgreSQL connection details first."
                    }
                ]
            };
        }

        logger.LogInformation("Calling tool: {ToolName}", callParams.Name);

        return callParams.Name switch
        {
            "scan_database_structure" => await ExecuteScanDatabaseAsync(callParams.Arguments,
                connectionBuilder, schemaService, cancellationToken),
            "query_database" => await ExecuteQueryDatabaseAsync(callParams.Arguments,
                connectionBuilder, queryService, cancellationToken),
            _ => new CallToolResult
            {
                IsError = true,
                Content = [new Content { Type = "text", Text = $"Unknown tool: {callParams.Name}" }]
            }
        };
    }

    private static async Task<object> HandleResourcesListAsync(
        JsonRpcRequest request,
        IResourceProvider resourceProvider)
    {
        var listParams = DeserializeParams<ListResourcesParams>(request.Params);
        return await resourceProvider.ListResourcesAsync(listParams?.Cursor);
    }

    private static async Task<object> HandleResourcesReadAsync(
        JsonRpcRequest request,
        IResourceProvider resourceProvider)
    {
        var readParams = DeserializeParams<ReadResourceParams>(request.Params);
        if (readParams == null || string.IsNullOrEmpty(readParams.Uri))
        {
            throw new ArgumentException("Resource URI is required");
        }

        return await resourceProvider.ReadResourceAsync(readParams.Uri);
    }

    private static async Task<object> HandleResourcesSubscribeAsync(
        JsonRpcRequest request,
        IResourceProvider resourceProvider)
    {
        var subscribeParams = DeserializeParams<SubscribeResourceParams>(request.Params);
        if (subscribeParams == null || string.IsNullOrEmpty(subscribeParams.Uri))
        {
            throw new ArgumentException("Resource URI is required");
        }

        await resourceProvider.SubscribeAsync(subscribeParams.Uri);
        return new { };
    }

    private static async Task<object> HandleResourcesUnsubscribeAsync(
        JsonRpcRequest request,
        IResourceProvider resourceProvider)
    {
        var unsubscribeParams = DeserializeParams<UnsubscribeResourceParams>(request.Params);
        if (unsubscribeParams == null || string.IsNullOrEmpty(unsubscribeParams.Uri))
        {
            throw new ArgumentException("Resource URI is required");
        }

        await resourceProvider.UnsubscribeAsync(unsubscribeParams.Uri);
        return new { };
    }

    private static async Task<object> HandlePromptsListAsync(IPromptProvider promptProvider)
    {
        return await promptProvider.ListPromptsAsync();
    }

    private static async Task<object> HandlePromptsGetAsync(
        JsonRpcRequest request,
        IPromptProvider promptProvider)
    {
        var getParams = DeserializeParams<GetPromptParams>(request.Params);
        if (getParams == null || string.IsNullOrEmpty(getParams.Name))
        {
            throw new ArgumentException("Prompt name is required");
        }

        return await promptProvider.GetPromptAsync(getParams.Name, getParams.Arguments);
    }

    #endregion

    #region Tool Implementations

    private static async Task<CallToolResult> ExecuteScanDatabaseAsync(
        Dictionary<string, object?>? arguments,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        CancellationToken cancellationToken)
    {
        var database = GetRequiredArgument<string>(arguments, "database");
        var connectionString = connectionBuilder.BuildConnectionString(database);

        var schema = await schemaService.ScanDatabaseSchemaAsync(connectionString, cancellationToken);

        var schemaJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new CallToolResult
        {
            IsError = false,
            Content = [new Content { Type = "text", Text = schemaJson }]
        };
    }

    private static async Task<CallToolResult> ExecuteQueryDatabaseAsync(
        Dictionary<string, object?>? arguments,
        IConnectionBuilderService connectionBuilder,
        IQueryService queryService,
        CancellationToken cancellationToken)
    {
        var database = GetRequiredArgument<string>(arguments, "database");
        var query = GetRequiredArgument<string>(arguments, "query");

        var connectionString = connectionBuilder.BuildConnectionString(database);

        // Validate query safety first
        if (!queryService.ValidateQuerySafety(query))
        {
            return new CallToolResult
            {
                IsError = true,
                Content =
                [
                    new Content
                    {
                        Type = "text",
                        Text = "Query validation failed: Only SELECT queries are allowed. Data modifications (INSERT/UPDATE/DELETE) and schema modifications (CREATE/ALTER/DROP) are blocked."
                    }
                ]
            };
        }

        var result = await queryService.ExecuteQueryAsync(connectionString, query, cancellationToken);

        var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new CallToolResult
        {
            IsError = false,
            Content = [new Content { Type = "text", Text = resultJson }]
        };
    }

    #endregion

    #region SSE Endpoint

    private static async Task HandleSseConnection(
        HttpContext context,
        ISseNotificationService sseService,
        ILogger<Program> logger)
    {
        var clientId = context.Request.Query["clientId"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        logger.LogInformation("SSE connection established for client: {ClientId}", clientId);

        // Set SSE headers
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        await using var writer = new StreamWriter(context.Response.Body);

        try
        {
            await sseService.RegisterClientAsync(clientId, writer, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SSE connection closed for client: {ClientId}", clientId);
        }
    }

    #endregion

    #region Legacy Compatibility Endpoints

    private static IResult LegacyInitialize(
        [FromBody] ServerConnectionOptions options,
        IConnectionBuilderService connectionBuilder,
        ILogger<Program> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
            {
                return Results.BadRequest(new { error = "Username and password are required" });
            }

            connectionBuilder.ConfigureServer(options);
            logger.LogInformation("Legacy initialize called for {Host}:{Port}", options.Host, options.Port);

            return Results.Ok(new
            {
                success = true,
                message = "Server configured successfully",
                configuration = new
                {
                    host = options.Host,
                    port = options.Port,
                    username = options.Username
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in legacy initialize");
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static IResult LegacyGetConfiguration(IConnectionBuilderService connectionBuilder)
    {
        if (!connectionBuilder.IsConfigured)
        {
            return Results.Ok(new { configured = false });
        }

        var config = connectionBuilder.GetServerConfiguration();
        return Results.Ok(new
        {
            configured = true,
            host = config.Host,
            port = config.Port,
            username = config.Username
        });
    }

    private static IResult LegacyGetTools()
    {
        var result = HandleToolsList();
        return Results.Ok(result);
    }

    private static async Task<IResult> LegacyCallTool(
        [FromBody] CallToolParams request,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        IQueryService queryService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var jsonRpcRequest = new JsonRpcRequest
        {
            Method = "tools/call",
            Params = request
        };

        var result = await HandleToolsCallAsync(jsonRpcRequest, connectionBuilder, schemaService,
            queryService, logger, cancellationToken);

        return Results.Ok(result);
    }

    #endregion

    #region Helper Methods

    private static IResult GetServerInfo()
    {
        return Results.Json(new
        {
            name = "PostgreSQL MCP Server",
            version = "2.0.0",
            protocolVersion = "2024-11-05",
            description = "Read-only Model Context Protocol server for PostgreSQL database operations",
            capabilities = new
            {
                readOnly = true,
                dataModificationsBlocked = true,
                schemaModificationsBlocked = true,
                tools = true,
                resources = true,
                prompts = true,
                serverSentEvents = true
            },
            endpoints = new
            {
                messages = "/mcp/v1/messages",
                sse = "/mcp/v1/sse",
                discovery = "/.well-known/mcp.json",
                health = "/_health",
                documentation = "/scalar/v1"
            }
        });
    }

    private static IResult GetDiscoveryInfo()
    {
        return Results.Json(new
        {
            mcpServers = new
            {
                postgres = new
                {
                    endpoint = "/mcp/v1/messages",
                    sse = "/mcp/v1/sse",
                    transport = "http",
                    capabilities = new[] { "tools", "resources", "prompts" }
                }
            }
        });
    }

    private static T? DeserializeParams<T>(object? paramsObj) where T : class
    {
        if (paramsObj == null) return null;

        if (paramsObj is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), JsonOptions);
        }

        if (paramsObj is T typedParams)
        {
            return typedParams;
        }

        var json = JsonSerializer.Serialize(paramsObj, JsonOptions);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static T GetRequiredArgument<T>(Dictionary<string, object?>? arguments, string key)
    {
        if (arguments == null || !arguments.TryGetValue(key, out var value) || value == null)
        {
            throw new ArgumentException($"Required argument '{key}' is missing");
        }

        if (value is JsonElement jsonElement)
        {
            var result = JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            return result ?? throw new ArgumentException($"Cannot deserialize argument '{key}'");
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            throw new ArgumentException($"Cannot convert argument '{key}' to type {typeof(T).Name}");
        }
    }

    private static JsonRpcResponse CreateErrorResponse(object? id, int code, string message, object? data = null)
    {
        return new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            }
        };
    }

    #endregion
}
