using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Provides MCP prompt templates for common database queries.
/// </summary>
public class PromptProvider : IPromptProvider
{
    private readonly ILogger<PromptProvider> _logger;

    public PromptProvider(ILogger<PromptProvider> logger)
    {
        _logger = logger;
    }

    public Task<ListPromptsResult> ListPromptsAsync()
    {
        var prompts = new List<Prompt>
        {
            new()
            {
                Name = "analyze_table",
                Description = "Generate a query to analyze a specific table's structure and data",
                Arguments =
                [
                    new PromptArgument
                    {
                        Name = "database",
                        Description = "Database name",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "table",
                        Description = "Table name (schema.table or just table)",
                        Required = true
                    }
                ]
            },
            new()
            {
                Name = "find_relationships",
                Description = "Generate a query to find relationships between tables",
                Arguments =
                [
                    new PromptArgument
                    {
                        Name = "database",
                        Description = "Database name",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "schema",
                        Description = "Schema name (default: public)",
                        Required = false
                    }
                ]
            },
            new()
            {
                Name = "recent_data",
                Description = "Generate a query to get recent data from a table",
                Arguments =
                [
                    new PromptArgument
                    {
                        Name = "database",
                        Description = "Database name",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "table",
                        Description = "Table name",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "limit",
                        Description = "Number of rows to return (default: 10)",
                        Required = false
                    }
                ]
            },
            new()
            {
                Name = "search_columns",
                Description = "Generate a query to search for columns containing specific text",
                Arguments =
                [
                    new PromptArgument
                    {
                        Name = "database",
                        Description = "Database name",
                        Required = true
                    },
                    new PromptArgument
                    {
                        Name = "search_text",
                        Description = "Text to search for in column names",
                        Required = true
                    }
                ]
            }
        };

        return Task.FromResult(new ListPromptsResult { Prompts = prompts });
    }

    public Task<GetPromptResult> GetPromptAsync(string name, Dictionary<string, string>? arguments = null)
    {
        arguments ??= new Dictionary<string, string>();

        var prompt = name switch
        {
            "analyze_table" => GenerateAnalyzeTablePrompt(arguments),
            "find_relationships" => GenerateFindRelationshipsPrompt(arguments),
            "recent_data" => GenerateRecentDataPrompt(arguments),
            "search_columns" => GenerateSearchColumnsPrompt(arguments),
            _ => throw new ArgumentException($"Unknown prompt: {name}")
        };

        return Task.FromResult(prompt);
    }

    private GetPromptResult GenerateAnalyzeTablePrompt(Dictionary<string, string> arguments)
    {
        var database = GetRequiredArgument(arguments, "database");
        var table = GetRequiredArgument(arguments, "table");

        var promptText = $@"You have access to a PostgreSQL database '{database}'.

Please analyze the table '{table}' by:
1. First, scan the database structure using the scan_database_structure tool to understand the table schema
2. Then, query a sample of data from {table} to understand its content
3. Provide insights about:
   - The table's purpose based on its columns
   - Data types and constraints
   - Relationships with other tables (foreign keys)
   - Sample data patterns
   - Any potential data quality issues

Use the available MCP tools to gather this information.";

        return new GetPromptResult
        {
            Description = $"Analyze table '{table}' in database '{database}'",
            Messages =
            [
                new PromptMessage
                {
                    Role = "user",
                    Content = new Content
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            ]
        };
    }

    private GetPromptResult GenerateFindRelationshipsPrompt(Dictionary<string, string> arguments)
    {
        var database = GetRequiredArgument(arguments, "database");
        var schema = arguments.GetValueOrDefault("schema", "public");

        var promptText = $@"You have access to a PostgreSQL database '{database}'.

Please find and visualize the relationships between tables in the '{schema}' schema:
1. Use the scan_database_structure tool to get all tables and their foreign keys
2. Identify all table relationships
3. Create a textual representation of the database schema showing:
   - Tables and their primary keys
   - Foreign key relationships
   - Relationship types (one-to-many, many-to-many, etc.)
4. Suggest any missing relationships or potential improvements";

        return new GetPromptResult
        {
            Description = $"Find relationships in schema '{schema}' of database '{database}'",
            Messages =
            [
                new PromptMessage
                {
                    Role = "user",
                    Content = new Content
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            ]
        };
    }

    private GetPromptResult GenerateRecentDataPrompt(Dictionary<string, string> arguments)
    {
        var database = GetRequiredArgument(arguments, "database");
        var table = GetRequiredArgument(arguments, "table");
        var limit = arguments.GetValueOrDefault("limit", "10");

        var promptText = $@"You have access to a PostgreSQL database '{database}'.

Please retrieve the most recent data from the '{table}' table:
1. First, use scan_database_structure to understand the table schema
2. Identify which column represents the creation/update timestamp
3. Query the {limit} most recent records ordered by that timestamp
4. Present the data in a readable format
5. Provide insights about the recent activity or patterns";

        return new GetPromptResult
        {
            Description = $"Get recent data from table '{table}' in database '{database}'",
            Messages =
            [
                new PromptMessage
                {
                    Role = "user",
                    Content = new Content
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            ]
        };
    }

    private GetPromptResult GenerateSearchColumnsPrompt(Dictionary<string, string> arguments)
    {
        var database = GetRequiredArgument(arguments, "database");
        var searchText = GetRequiredArgument(arguments, "search_text");

        var promptText = $@"You have access to a PostgreSQL database '{database}'.

Please search for columns containing the text '{searchText}':
1. Use scan_database_structure to get all tables and their columns
2. Find all columns where the column name contains '{searchText}' (case-insensitive)
3. For each matching column, show:
   - Table name
   - Column name
   - Data type
   - Whether it's nullable
   - Whether it's part of a primary or foreign key
4. Group results by table for better readability";

        return new GetPromptResult
        {
            Description = $"Search for columns containing '{searchText}' in database '{database}'",
            Messages =
            [
                new PromptMessage
                {
                    Role = "user",
                    Content = new Content
                    {
                        Type = "text",
                        Text = promptText
                    }
                }
            ]
        };
    }

    private static string GetRequiredArgument(Dictionary<string, string> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Required argument '{key}' is missing or empty");
        }

        return value;
    }
}
