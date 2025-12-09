using AdeotekSqlMcp.Database;
using AdeotekSqlMcp.Models;
using AdeotekSqlMcp.Security;
using AdeotekSqlMcp.Utilities;
using Serilog.Core;

namespace AdeotekSqlMcp.Services;

/// <summary>
/// Service for MCP tools implementation
/// </summary>
public sealed class McpToolsService
{
    private readonly QueryValidator _validator;
    private readonly Logger _logger;

    public McpToolsService(QueryValidator validator, Logger logger)
    {
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available MCP tools
    /// </summary>
    public IReadOnlyList<McpTool> GetTools()
    {
        return new[]
        {
            new McpTool
            {
                Name = "sql_list_databases",
                Description = "List all databases available on the configured SQL server (Microsoft SQL Server or PostgreSQL)",
                InputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["connectionString"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Connection string in format: type=mssql;host=localhost;port=1433;user=sa;password=pass;database=master"
                        }
                    },
                    ["required"] = new[] { "connectionString" }
                }
            },
            new McpTool
            {
                Name = "sql_list_tables",
                Description = "List all tables in a specified database with metadata (schema, name, row count, size, type)",
                InputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["connectionString"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Connection string"
                        },
                        ["database"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Database name"
                        },
                        ["schema"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Schema filter (optional, defaults to 'public' for PostgreSQL or 'dbo' for SQL Server)"
                        }
                    },
                    ["required"] = new[] { "connectionString", "database" }
                }
            },
            new McpTool
            {
                Name = "sql_describe_table",
                Description = "Get detailed schema information for a specific table including columns, indexes, foreign keys, and constraints",
                InputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["connectionString"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Connection string"
                        },
                        ["database"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Database name"
                        },
                        ["schema"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Schema name"
                        },
                        ["table"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Table name"
                        }
                    },
                    ["required"] = new[] { "connectionString", "database", "schema", "table" }
                }
            },
            new McpTool
            {
                Name = "sql_query",
                Description = "Execute a read-only SELECT query with comprehensive security validation and automatic row limiting",
                InputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["connectionString"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Connection string"
                        },
                        ["database"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Database name"
                        },
                        ["query"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "SQL SELECT query"
                        },
                        ["maxRows"] = new Dictionary<string, object>
                        {
                            ["type"] = "number",
                            ["description"] = "Maximum rows to return (default: 1000, max: 10000)",
                            ["default"] = 1000
                        }
                    },
                    ["required"] = new[] { "connectionString", "database", "query" }
                }
            },
            new McpTool
            {
                Name = "sql_get_query_plan",
                Description = "Get the execution plan for a query without executing it (for performance analysis)",
                InputSchema = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["connectionString"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Connection string"
                        },
                        ["database"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "Database name"
                        },
                        ["query"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "SQL SELECT query to analyze"
                        }
                    },
                    ["required"] = new[] { "connectionString", "database", "query" }
                }
            }
        };
    }

    /// <summary>
    /// Executes an MCP tool
    /// </summary>
    public async Task<McpToolCallResponse> ExecuteToolAsync(string toolName, Dictionary<string, object>? arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Information("Executing tool: {ToolName}", toolName);

            if (arguments == null)
            {
                throw new ArgumentException("Arguments cannot be null");
            }

            var result = toolName switch
            {
                "sql_list_databases" => await ListDatabasesAsync(arguments, cancellationToken),
                "sql_list_tables" => await ListTablesAsync(arguments, cancellationToken),
                "sql_describe_table" => await DescribeTableAsync(arguments, cancellationToken),
                "sql_query" => await ExecuteQueryAsync(arguments, cancellationToken),
                "sql_get_query_plan" => await GetQueryPlanAsync(arguments, cancellationToken),
                _ => throw new ToolNotFoundException(toolName)
            };

            _logger.Information("Tool {ToolName} executed successfully", toolName);

            return new McpToolCallResponse
            {
                Success = true,
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Tool {ToolName} execution failed", toolName);

            return new McpToolCallResponse
            {
                Success = false,
                Error = ex.Message,
                Metadata = new Dictionary<string, object>
                {
                    ["errorType"] = ex.GetType().Name,
                    ["errorCode"] = ex is McpException mcpEx ? mcpEx.ErrorCode : -32000
                }
            };
        }
    }

    private async Task<object> ListDatabasesAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var connStr = GetRequiredArgument<string>(arguments, "connectionString");

        await using var db = DatabaseFactory.Create(connStr, _logger);
        await db.TestConnectionAsync(cancellationToken);

        var databases = await db.ListDatabasesAsync(cancellationToken);

        return new
        {
            databases = databases,
            count = databases.Count,
            databaseType = db.DatabaseType
        };
    }

    private async Task<object> ListTablesAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var connStr = GetRequiredArgument<string>(arguments, "connectionString");
        var database = GetRequiredArgument<string>(arguments, "database");
        var schema = GetOptionalArgument<string>(arguments, "schema");

        await using var db = DatabaseFactory.Create(connStr, _logger);
        await db.TestConnectionAsync(cancellationToken);

        var tables = await db.ListTablesAsync(database, schema, cancellationToken);

        return new
        {
            database = database,
            schema = schema,
            tables = tables,
            count = tables.Count
        };
    }

    private async Task<object> DescribeTableAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var connStr = GetRequiredArgument<string>(arguments, "connectionString");
        var database = GetRequiredArgument<string>(arguments, "database");
        var schema = GetRequiredArgument<string>(arguments, "schema");
        var table = GetRequiredArgument<string>(arguments, "table");

        // Sanitize identifiers
        schema = _validator.SanitizeIdentifier(schema);
        table = _validator.SanitizeIdentifier(table);

        await using var db = DatabaseFactory.Create(connStr, _logger);
        await db.TestConnectionAsync(cancellationToken);

        var tableSchema = await db.DescribeTableAsync(database, schema, table, cancellationToken);

        return tableSchema;
    }

    private async Task<object> ExecuteQueryAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var connStr = GetRequiredArgument<string>(arguments, "connectionString");
        var database = GetRequiredArgument<string>(arguments, "database");
        var query = GetRequiredArgument<string>(arguments, "query");
        var maxRows = GetOptionalArgument<int>(arguments, "maxRows") ?? 1000;

        // Validate query
        _validator.ValidateOrThrow(query);

        // Enforce row limit
        maxRows = Math.Min(maxRows, 10000);
        query = _validator.EnforceLimit(query, maxRows);

        await using var db = DatabaseFactory.Create(connStr, _logger);
        await db.TestConnectionAsync(cancellationToken);

        var result = await db.ExecuteQueryAsync(database, query, maxRows, cancellationToken);

        return result;
    }

    private async Task<object> GetQueryPlanAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        var connStr = GetRequiredArgument<string>(arguments, "connectionString");
        var database = GetRequiredArgument<string>(arguments, "database");
        var query = GetRequiredArgument<string>(arguments, "query");

        // Validate query
        _validator.ValidateOrThrow(query);

        await using var db = DatabaseFactory.Create(connStr, _logger);
        await db.TestConnectionAsync(cancellationToken);

        var plan = await db.GetQueryPlanAsync(database, query, cancellationToken);

        return new
        {
            query = query,
            plan = plan,
            database = database,
            databaseType = db.DatabaseType
        };
    }

    private static T GetRequiredArgument<T>(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
        {
            throw new ArgumentException($"Missing required argument: {key}");
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
            throw new ArgumentException($"Argument '{key}' must be of type {typeof(T).Name}");
        }
    }

    private static T? GetOptionalArgument<T>(Dictionary<string, object> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value))
        {
            return default;
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
            return default;
        }
    }
}
