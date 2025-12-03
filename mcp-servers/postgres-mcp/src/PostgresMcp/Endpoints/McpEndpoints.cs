using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PostgresMcp.Models;
using PostgresMcp.Services;

namespace PostgresMcp.Endpoints;

/// <summary>
/// MCP (Model Context Protocol) Minimal API endpoints for PostgreSQL database operations.
/// </summary>
public static class McpEndpoints
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps root endpoint to the application.
        /// </summary>
        public IEndpointRouteBuilder MapRootEndpoint()
        {
            // Root endpoint with API information
            endpoints.MapGet("/", () => Results.Json(new
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
                    initialize = "/mcp/initialize",
                    configuration = "/mcp/configuration",
                    tools = "/mcp/tools",
                    call = "/mcp/tools/call",
                    jsonrpc = "/mcp/jsonrpc",
                    health = "/_health",
                    documentation = "/scalar/v1"
                },
                documentation = "/scalar/v1"
            }));

            return endpoints;
        }

        /// <summary>
        /// Maps MCP endpoints to the application.
        /// </summary>
        public IEndpointRouteBuilder MapMcpEndpoints()
        {
            var mcpGroup = endpoints.MapGroup("/mcp")
                .WithTags("MCP");

            mcpGroup.MapPost("/initialize", Initialize)
                .WithName("InitializeMcpServer")
                .WithDescription("Initialize/configure the MCP server with PostgreSQL server connection parameters")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);

            mcpGroup.MapGet("/configuration", GetConfiguration)
                .WithName("GetMcpConfiguration")
                .WithDescription("Get the current server configuration status")
                .Produces(StatusCodes.Status200OK);

            mcpGroup.MapGet("/tools", GetToolsAsync)
                .WithName("GetMcpTools")
                .WithDescription("List all available MCP tools")
                .Produces<McpToolsResponse>();

            mcpGroup.MapPost("/tools/call", CallToolAsync)
                .WithName("CallMcpTool")
                .WithDescription("Call a specific MCP tool")
                .Produces<McpToolCallResponse>();

            mcpGroup.MapPost("/jsonrpc", JsonRpcAsync)
                .WithName("McpJsonRpc")
                .WithDescription("JSON-RPC 2.0 endpoint for MCP protocol")
                .Produces<JsonRpcResponse>();

            return endpoints;
        }
    }

    /// <summary>
    /// Initialize/configure the MCP server with PostgreSQL server connection parameters.
    /// </summary>
    private static Results<Ok<object>, BadRequest<object>> Initialize(
        ServerConnectionOptions options,
        IConnectionBuilderService connectionBuilder,
        ILogger<Program> logger)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(options.Username))
            {
                return TypedResults.BadRequest<object>(new { error = "Username is required" });
            }

            if (string.IsNullOrWhiteSpace(options.Password))
            {
                return TypedResults.BadRequest<object>(new { error = "Password is required" });
            }

            connectionBuilder.ConfigureServer(options);

            logger.LogInformation("MCP server initialized with connection to {Host}:{Port}",
                options.Host, options.Port);

            return TypedResults.Ok<object>(new
            {
                success = true,
                message = "MCP server initialized successfully",
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
            logger.LogError(ex, "Error initializing MCP server");
            return TypedResults.BadRequest<object>(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get the current server configuration status.
    /// </summary>
    private static Ok<object> GetConfiguration(
        IConnectionBuilderService connectionBuilder)
    {
        if (!connectionBuilder.IsConfigured)
        {
            return TypedResults.Ok<object>(new
            {
                configured = false,
                message = "MCP server not initialized. Call POST /mcp/initialize to configure connection parameters."
            });
        }

        var config = connectionBuilder.GetServerConfiguration();
        return TypedResults.Ok<object>(new
        {
            configured = true,
            host = config.Host,
            port = config.Port,
            username = config.Username
        });
    }

    /// <summary>
    /// List all available MCP tools.
    /// </summary>
    private static IResult GetToolsAsync()
    {
        var tools = new McpToolsResponse
        {
            Tools =
            [
                new McpTool
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
                new McpTool
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
            ]
        };

        return Results.Ok(tools);
    }

    /// <summary>
    /// Call a specific MCP tool.
    /// </summary>
    private static async Task<IResult> CallToolAsync(
        [FromBody] McpToolCallRequest request,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        IQueryService queryService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Tool call: {ToolName}", request.Name);

            var response = request.Name switch
            {
                "scan_database_structure" => await ScanDatabaseStructureAsync(
                    request.Arguments, connectionBuilder, schemaService, cancellationToken),
                "query_database" => await QueryDatabaseAsync(
                    request.Arguments, connectionBuilder, queryService, cancellationToken),
                _ => new McpToolCallResponse
                {
                    IsError = true,
                    Content = [new McpContent { Text = $"Unknown tool: {request.Name}" }]
                }
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling tool: {ToolName}", request.Name);

            return Results.Ok(new McpToolCallResponse
            {
                IsError = true,
                Content = [new McpContent { Text = $"Error: {ex.Message}" }]
            });
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 endpoint for MCP protocol.
    /// </summary>
    private static async Task<IResult> JsonRpcAsync(
        [FromBody] JsonRpcRequest request,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        IQueryService queryService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = request.Method switch
            {
                "tools/list" => GetToolsList(),
                "tools/call" => await HandleToolCallAsync(
                    request.Params, connectionBuilder, schemaService, queryService, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown method: {request.Method}")
            };

            return Results.Ok(new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON-RPC error: {Method}", request.Method);

            return Results.Ok(new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = -32603,
                    Message = "Internal error",
                    Data = ex.Message
                }
            });
        }
    }

    private static async Task<McpToolCallResponse> ScanDatabaseStructureAsync(
        Dictionary<string, object?> arguments,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        CancellationToken cancellationToken)
    {
        if (!connectionBuilder.IsConfigured)
        {
            return new McpToolCallResponse
            {
                IsError = true,
                Content = [new McpContent
                {
                    Text = "MCP server not initialized. Please call POST /mcp/initialize first to configure connection parameters."
                }]
            };
        }

        var database = GetRequiredArgument<string>(arguments, "database");
        var connectionString = connectionBuilder.BuildConnectionString(database);

        var schema = await schemaService.ScanDatabaseSchemaAsync(connectionString, cancellationToken);

        var schemaJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new McpToolCallResponse
        {
            IsError = false,
            Content = [new McpContent { Text = schemaJson }]
        };
    }

    private static async Task<McpToolCallResponse> QueryDatabaseAsync(
        Dictionary<string, object?> arguments,
        IConnectionBuilderService connectionBuilder,
        IQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (!connectionBuilder.IsConfigured)
        {
            return new McpToolCallResponse
            {
                IsError = true,
                Content = [new McpContent
                {
                    Text = "MCP server not initialized. Please call POST /mcp/initialize first to configure connection parameters."
                }]
            };
        }

        var database = GetRequiredArgument<string>(arguments, "database");
        var query = GetRequiredArgument<string>(arguments, "query");

        var connectionString = connectionBuilder.BuildConnectionString(database);

        // Validate query safety first
        if (!queryService.ValidateQuerySafety(query))
        {
            return new McpToolCallResponse
            {
                IsError = true,
                Content = [new McpContent
                {
                    Text = "Query validation failed: Only SELECT queries are allowed. Data modifications (INSERT/UPDATE/DELETE) and schema modifications (CREATE/ALTER/DROP) are blocked."
                }]
            };
        }

        var result = await queryService.ExecuteQueryAsync(connectionString, query, cancellationToken);

        var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new McpToolCallResponse
        {
            IsError = false,
            Content = [new McpContent { Text = resultJson }]
        };
    }

    private static T GetRequiredArgument<T>(Dictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value == null)
        {
            throw new ArgumentException($"Required argument '{key}' is missing");
        }

        if (value is JsonElement jsonElement)
        {
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText())
                ?? throw new ArgumentException($"Cannot deserialize argument '{key}'");
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

    private static object GetToolsList()
    {
        var tools = new McpToolsResponse
        {
            Tools =
            [
                new McpTool
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
                new McpTool
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
            ]
        };

        return tools;
    }

    private static async Task<object> HandleToolCallAsync(
        Dictionary<string, object?>? parameters,
        IConnectionBuilderService connectionBuilder,
        IDatabaseSchemaService schemaService,
        IQueryService queryService,
        CancellationToken cancellationToken)
    {
        if (parameters == null)
        {
            throw new ArgumentException("Missing parameters");
        }

        if (!parameters.TryGetValue("name", out var nameObj) || nameObj == null)
        {
            throw new ArgumentException("Missing tool name");
        }

        if (!parameters.TryGetValue("arguments", out var argsObj) || argsObj == null)
        {
            throw new ArgumentException("Missing tool arguments");
        }

        var name = nameObj.ToString() ?? throw new ArgumentException("Tool name cannot be null");

        var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(
            argsObj.ToString() ?? "{}") ?? [];

        var request = new McpToolCallRequest
        {
            Name = name,
            Arguments = arguments
        };

        var response = request.Name switch
        {
            "scan_database_structure" => await ScanDatabaseStructureAsync(
                request.Arguments, connectionBuilder, schemaService, cancellationToken),
            "query_database" => await QueryDatabaseAsync(
                request.Arguments, connectionBuilder, queryService, cancellationToken),
            _ => new McpToolCallResponse
            {
                IsError = true,
                Content = [new McpContent { Text = $"Unknown tool: {request.Name}" }]
            }
        };

        return response;
    }
}
