using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PostgresMcp.Models;
using PostgresMcp.Services;

namespace PostgresMcp.Controllers;

/// <summary>
/// MCP (Model Context Protocol) controller for PostgreSQL database operations.
/// </summary>
[ApiController]
[Route("mcp")]
public class McpController(
    ILogger<McpController> logger,
    IDatabaseSchemaService schemaService,
    IQueryService queryService,
    IOptions<PostgresOptions> postgresOptions) : ControllerBase
{
    private readonly PostgresOptions _postgresOptions = postgresOptions.Value;

    /// <summary>
    /// List all available MCP tools.
    /// </summary>
    [HttpGet("tools")]
    public IActionResult GetTools()
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
                            connectionString = new
                            {
                                type = "string",
                                description = "PostgreSQL connection string (e.g., 'Host=localhost;Database=mydb;Username=user;Password=pass')"
                            }
                        },
                        required = new[] { "connectionString" }
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
                            connectionString = new
                            {
                                type = "string",
                                description = "PostgreSQL connection string"
                            },
                            query = new
                            {
                                type = "string",
                                description = "SQL SELECT query to execute (must be read-only)"
                            }
                        },
                        required = new[] { "connectionString", "query" }
                    }
                }
            ]
        };

        return Ok(tools);
    }

    /// <summary>
    /// Call a specific MCP tool.
    /// </summary>
    [HttpPost("tools/call")]
    public async Task<IActionResult> CallTool([FromBody] McpToolCallRequest request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Tool call: {ToolName}", request.Name);

            var response = request.Name switch
            {
                "scan_database_structure" => await ScanDatabaseStructureAsync(request.Arguments, cancellationToken),
                "query_database" => await QueryDatabaseAsync(request.Arguments, cancellationToken),
                _ => new McpToolCallResponse
                {
                    IsError = true,
                    Content = [new McpContent { Text = $"Unknown tool: {request.Name}" }]
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling tool: {ToolName}", request.Name);

            return Ok(new McpToolCallResponse
            {
                IsError = true,
                Content = [new McpContent { Text = $"Error: {ex.Message}" }]
            });
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 endpoint for MCP protocol.
    /// </summary>
    [HttpPost("jsonrpc")]
    public async Task<IActionResult> JsonRpc([FromBody] JsonRpcRequest request, CancellationToken cancellationToken)
    {
        try
        {
            object? result = request.Method switch
            {
                "tools/list" => GetToolsList(),
                "tools/call" => await HandleToolCall(request.Params, cancellationToken),
                _ => throw new InvalidOperationException($"Unknown method: {request.Method}")
            };

            return Ok(new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "JSON-RPC error: {Method}", request.Method);

            return Ok(new JsonRpcResponse
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

    private async Task<McpToolCallResponse> ScanDatabaseStructureAsync(
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString(arguments);

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

    private async Task<McpToolCallResponse> QueryDatabaseAsync(
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var connectionString = GetConnectionString(arguments);

        if (!arguments.TryGetValue("query", out var queryObj) || queryObj == null)
        {
            throw new ArgumentException("Missing required argument: query");
        }

        var query = queryObj.ToString() ?? throw new ArgumentException("Query cannot be null");

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

    private string GetConnectionString(Dictionary<string, object?> arguments)
    {
        if (arguments.TryGetValue("connectionString", out var connStrObj) && connStrObj != null)
        {
            return connStrObj.ToString() ?? throw new ArgumentException("Connection string cannot be null");
        }

        // Use default connection string if not provided
        if (!string.IsNullOrEmpty(_postgresOptions.DefaultConnectionString))
        {
            return _postgresOptions.DefaultConnectionString;
        }

        throw new ArgumentException("No connection string provided and no default configured");
    }

    private object GetToolsList()
    {
        var tools = new McpToolsResponse
        {
            Tools = GetTools().Value as McpToolsResponse
                ?? throw new InvalidOperationException("Failed to get tools")
        };

        return tools;
    }

    private async Task<object> HandleToolCall(Dictionary<string, object?>? parameters, CancellationToken cancellationToken)
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

        var response = await CallTool(request, cancellationToken);

        return ((ObjectResult)response).Value ?? throw new InvalidOperationException("Failed to call tool");
    }
}
