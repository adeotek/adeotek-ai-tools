using System.Text.Json;
using AdeotekSqlMcp.Models;
using AdeotekSqlMcp.Security;
using AdeotekSqlMcp.Services;
using AdeotekSqlMcp.Utilities;
using Serilog.Events;

namespace AdeotekSqlMcp;

/// <summary>
/// Main application entry point
/// Implements MCP protocol 2025-11-25 with stdio transport
/// </summary>
public class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static async Task<int> Main(string[] args)
    {
        // Setup logging
        var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL")?.ToUpperInvariant() switch
        {
            "TRACE" => LogEventLevel.Verbose,
            "DEBUG" => LogEventLevel.Debug,
            "INFO" => LogEventLevel.Information,
            "WARN" => LogEventLevel.Warning,
            "ERROR" => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };

        var logger = LoggerFactory.CreateLogger(logLevel);

        try
        {
            logger.Information("Adeotek SQL MCP Server starting...");
            logger.Information("MCP Protocol: 2025-11-25 (stdio transport)");
            logger.Information("Supported databases: Microsoft SQL Server, PostgreSQL");

            // Create services
            var validator = new QueryValidator();
            var toolsService = new McpToolsService(validator, logger);
            var promptsService = new McpPromptsService();

            // Start MCP server
            await RunServerAsync(toolsService, promptsService, logger);

            return 0;
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Fatal error occurred");
            return 1;
        }
        finally
        {
            logger.Information("Adeotek SQL MCP Server stopped");
            await Task.Delay(100); // Allow final logs to flush
        }
    }

    private static async Task RunServerAsync(
        McpToolsService toolsService,
        McpPromptsService promptsService,
        Serilog.Core.Logger logger)
    {
        logger.Information("MCP server ready, listening on stdio");

        using var reader = new StreamReader(Console.OpenStandardInput());
        await using var writer = new StreamWriter(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, JsonOptions);
                if (request == null)
                {
                    logger.Warning("Received invalid JSON-RPC request");
                    continue;
                }

                logger.Debug("Received request: {Method}", request.Method);

                var response = await HandleRequestAsync(request, toolsService, promptsService, logger);

                var responseJson = JsonSerializer.Serialize(response, JsonOptions);
                await writer.WriteLineAsync(responseJson);

                logger.Debug("Sent response for request ID: {Id}", request.Id);
            }
            catch (JsonException ex)
            {
                logger.Error(ex, "Failed to parse JSON-RPC request");

                var errorResponse = new JsonRpcResponse
                {
                    Id = null,
                    Error = new JsonRpcError
                    {
                        Code = -32700,
                        Message = "Parse error"
                    }
                };

                var errorJson = JsonSerializer.Serialize(errorResponse, JsonOptions);
                await writer.WriteLineAsync(errorJson);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Unexpected error handling request");
            }
        }
    }

    private static async Task<JsonRpcResponse> HandleRequestAsync(
        JsonRpcRequest request,
        McpToolsService toolsService,
        McpPromptsService promptsService,
        Serilog.Core.Logger logger)
    {
        try
        {
            object? result = request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "initialized" => HandleInitialized(),
                "tools/list" => HandleToolsList(toolsService),
                "tools/call" => await HandleToolsCallAsync(request, toolsService),
                "prompts/list" => HandlePromptsList(promptsService),
                "prompts/get" => HandlePromptsGet(request, promptsService),
                _ => throw new McpException($"Method not found: {request.Method}", -32601)
            };

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        catch (McpException ex)
        {
            logger.Error(ex, "MCP error handling request");

            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = ex.ErrorCode,
                    Message = ex.Message
                }
            };
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Unexpected error handling request");

            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = -32603,
                    Message = "Internal error",
                    Data = ex.Message
                }
            };
        }
    }

    private static object HandleInitialize(JsonRpcRequest request)
    {
        return new
        {
            protocolVersion = "2025-11-25",
            serverInfo = new
            {
                name = "adeotek-sql-net-mcp",
                version = "1.0.0"
            },
            capabilities = new
            {
                tools = new { },
                prompts = new { }
            }
        };
    }

    private static object HandleInitialized()
    {
        return new { };
    }

    private static object HandleToolsList(McpToolsService toolsService)
    {
        var tools = toolsService.GetTools();
        return new { tools };
    }

    private static async Task<object> HandleToolsCallAsync(JsonRpcRequest request, McpToolsService toolsService)
    {
        if (request.Params == null)
        {
            throw new McpException("Missing params", -32602);
        }

        if (!request.Params.TryGetValue("name", out var nameObj) || nameObj is not string toolName)
        {
            throw new McpException("Missing tool name", -32602);
        }

        var arguments = request.Params.TryGetValue("arguments", out var argsObj) && argsObj is JsonElement argsElement
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(argsElement.GetRawText(), JsonOptions)
            : null;

        var result = await toolsService.ExecuteToolAsync(toolName, arguments);
        return result;
    }

    private static object HandlePromptsList(McpPromptsService promptsService)
    {
        var prompts = promptsService.GetPrompts();
        return new { prompts };
    }

    private static object HandlePromptsGet(JsonRpcRequest request, McpPromptsService promptsService)
    {
        if (request.Params == null)
        {
            throw new McpException("Missing params", -32602);
        }

        if (!request.Params.TryGetValue("name", out var nameObj) || nameObj is not string promptName)
        {
            throw new McpException("Missing prompt name", -32602);
        }

        var arguments = request.Params.TryGetValue("arguments", out var argsObj) && argsObj is JsonElement argsElement
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(argsElement.GetRawText(), JsonOptions)
            : null;

        var prompt = promptsService.GetPrompt(promptName, arguments);

        return new
        {
            description = $"Prompt: {promptName}",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new
                    {
                        type = "text",
                        text = prompt
                    }
                }
            }
        };
    }
}
