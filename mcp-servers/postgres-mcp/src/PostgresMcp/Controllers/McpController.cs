using Microsoft.AspNetCore.Mvc;
using PostgresMcp.Models;
using PostgresMcp.Services;
using System.Text.Json;

namespace PostgresMcp.Controllers;

/// <summary>
/// MCP (Model Context Protocol) controller exposing PostgreSQL database tools.
/// Implements the MCP specification for tool discovery and execution.
/// </summary>
[ApiController]
[Route("mcp")]
public class McpController(
    ILogger<McpController> logger,
    IDatabaseSchemaService schemaService,
    IQueryService queryService,
    ISqlGenerationService sqlGenerationService) : ControllerBase
{
    private readonly ILogger<McpController> _logger = logger;
    private readonly IDatabaseSchemaService _schemaService = schemaService;
    private readonly IQueryService _queryService = queryService;
    private readonly ISqlGenerationService _sqlGenerationService = sqlGenerationService;

    /// <summary>
    /// Lists all available MCP tools.
    /// Endpoint: GET /mcp/tools
    /// </summary>
    [HttpGet("tools")]
    [ProducesResponseType(typeof(McpListToolsResponse), StatusCodes.Status200OK)]
    public IActionResult ListTools()
    {
        List<McpTool> tools =
        [
            new McpTool
            {
                Name = "scan_database_structure",
                Description = "Analyze and describe PostgreSQL database schema including tables, columns, relationships, constraints, and indexes. Answers natural language questions about the schema.",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        connectionString = new
                        {
                            type = "string",
                            description = "PostgreSQL connection string (e.g., 'Host=localhost;Database=mydb;Username=user;Password=pass')"
                        },
                        schemaFilter = new
                        {
                            type = "string",
                            description = "Optional schema filter (e.g., 'public'). If not specified, all accessible schemas are scanned."
                        },
                        question = new
                        {
                            type = "string",
                            description = "Optional natural language question about the schema (e.g., 'What tables have foreign keys to the users table?')"
                        }
                    },
                    required = ["connectionString"]
                }
            },
            new McpTool
            {
                Name = "query_database_data",
                Description = "Query and analyze data from PostgreSQL tables with automatic relationship detection. Converts natural language queries to SQL, follows foreign key relationships, and returns structured data with context.",
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
                            description = "Natural language query describing what data to retrieve (e.g., 'Show me all users who made orders in the last 30 days with their order totals')"
                        }
                    },
                    required = ["connectionString", "query"]
                }
            },
            new McpTool
            {
                Name = "advanced_sql_query",
                Description = "Generate and execute optimized SQL queries from natural language descriptions. Uses AI to convert requests to SQL, validates safety, optimizes performance, and returns both the query and results with explanations.",
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
                        naturalLanguageQuery = new
                        {
                            type = "string",
                            description = "Detailed natural language description of the desired query (e.g., 'Calculate the average order value by customer segment for Q4 2024, showing only segments with more than 100 orders')"
                        }
                    },
                    required = ["connectionString", "naturalLanguageQuery"]
                }
            }
        ];

        return Ok(new McpListToolsResponse { Tools = tools });
    }

    /// <summary>
    /// Executes an MCP tool.
    /// Endpoint: POST /mcp/tools/call
    /// </summary>
    [HttpPost("tools/call")]
    [ProducesResponseType(typeof(McpToolCallResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(McpToolCallResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(McpToolCallResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CallTool(
        [FromBody] McpToolCallRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Calling MCP tool: {ToolName}", request.Name);

            var result = request.Name switch
            {
                "scan_database_structure" => await ExecuteScanDatabaseStructure(request.Arguments, cancellationToken),
                "query_database_data" => await ExecuteQueryDatabaseData(request.Arguments, cancellationToken),
                "advanced_sql_query" => await ExecuteAdvancedSqlQuery(request.Arguments, cancellationToken),
                _ => new McpToolCallResponse
                {
                    Success = false,
                    Error = $"Unknown tool: {request.Name}"
                }
            };

            return result.Success ? Ok(result) : BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", request.Name);

            return StatusCode(500, new McpToolCallResponse
            {
                Success = false,
                Error = $"Internal error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// JSON-RPC 2.0 endpoint for MCP protocol compliance.
    /// Endpoint: POST /mcp/jsonrpc
    /// </summary>
    [HttpPost("jsonrpc")]
    [ProducesResponseType(typeof(JsonRpcResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> JsonRpc(
        [FromBody] JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("JSON-RPC call: {Method}", request.Method);

            object? result = request.Method switch
            {
                "tools/list" => await Task.FromResult(GetToolsList()),
                "tools/call" => await ExecuteToolFromJsonRpc(request.Params, cancellationToken),
                _ => null
            };

            if (result == null)
            {
                return Ok(new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = -32601,
                        Message = $"Method not found: {request.Method}"
                    }
                });
            }

            return Ok(new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON-RPC error");

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

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    private async Task<McpToolCallResponse> ExecuteScanDatabaseStructure(
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var connectionString = GetRequiredArgument<string>(arguments, "connectionString");
        var schemaFilter = GetOptionalArgument<string>(arguments, "schemaFilter");
        var question = GetOptionalArgument<string>(arguments, "question");

        if (!string.IsNullOrEmpty(question))
        {
            // Answer a specific question about the schema
            var answer = await _schemaService.AnswerSchemaQuestionAsync(
                connectionString,
                question,
                cancellationToken);

            return new McpToolCallResponse
            {
                Success = true,
                Data = new { answer, question },
                Metadata = new Dictionary<string, object>
                {
                    ["executedAt"] = DateTime.UtcNow,
                    ["hasAiResponse"] = true
                }
            };
        }

        // Scan the entire schema
        var schema = await _schemaService.ScanDatabaseSchemaAsync(
            connectionString,
            schemaFilter,
            cancellationToken);

        return new McpToolCallResponse
        {
            Success = true,
            Data = schema,
            Metadata = new Dictionary<string, object>
            {
                ["executedAt"] = DateTime.UtcNow,
                ["tableCount"] = schema.TableCount,
                ["serverVersion"] = schema.ServerVersion ?? "unknown"
            }
        };
    }

    private async Task<McpToolCallResponse> ExecuteQueryDatabaseData(
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var connectionString = GetRequiredArgument<string>(arguments, "connectionString");
        var query = GetRequiredArgument<string>(arguments, "query");

        var result = await _queryService.QueryDataAsync(
            connectionString,
            query,
            cancellationToken);

        return new McpToolCallResponse
        {
            Success = true,
            Data = result,
            Metadata = new Dictionary<string, object>
            {
                ["executedAt"] = DateTime.UtcNow,
                ["rowCount"] = result.RowCount,
                ["executionTimeMs"] = result.ExecutionTimeMs
            }
        };
    }

    private async Task<McpToolCallResponse> ExecuteAdvancedSqlQuery(
        Dictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        var connectionString = GetRequiredArgument<string>(arguments, "connectionString");
        var naturalLanguageQuery = GetRequiredArgument<string>(arguments, "naturalLanguageQuery");

        var result = await _sqlGenerationService.GenerateAndExecuteQueryAsync(
            connectionString,
            naturalLanguageQuery,
            cancellationToken);

        return new McpToolCallResponse
        {
            Success = result.IsSafe,
            Data = result,
            Metadata = new Dictionary<string, object>
            {
                ["executedAt"] = DateTime.UtcNow,
                ["isSafe"] = result.IsSafe,
                ["confidence"] = result.ConfidenceScore ?? 0.0
            }
        };
    }

    private McpListToolsResponse GetToolsList()
    {
        var listToolsResult = ListTools() as OkObjectResult;
        return (listToolsResult?.Value as McpListToolsResponse)!;
    }

    private async Task<object> ExecuteToolFromJsonRpc(
        object? parameters,
        CancellationToken cancellationToken)
    {
        if (parameters == null)
        {
            throw new ArgumentException("Parameters are required for tool execution");
        }

        // Parse parameters as McpToolCallRequest
        var json = JsonSerializer.Serialize(parameters);
        var request = JsonSerializer.Deserialize<McpToolCallRequest>(json)
            ?? throw new ArgumentException("Invalid tool call parameters");

        var result = await CallTool(request, cancellationToken);

        if (result is OkObjectResult okResult)
        {
            return okResult.Value!;
        }
        else if (result is BadRequestObjectResult badResult)
        {
            throw new Exception(badResult.Value?.ToString() ?? "Bad request");
        }
        else
        {
            throw new Exception("Unknown error");
        }
    }

    private T GetRequiredArgument<T>(Dictionary<string, object?> arguments, string key)
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

        // Try to convert
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            throw new ArgumentException($"Cannot convert argument '{key}' to type {typeof(T).Name}");
        }
    }

    private T? GetOptionalArgument<T>(Dictionary<string, object?> arguments, string key)
    {
        try
        {
            if (!arguments.TryGetValue(key, out var value) || value == null)
            {
                return default;
            }

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Null)
                {
                    return default;
                }
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }

            if (value is T typedValue)
            {
                return typedValue;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
