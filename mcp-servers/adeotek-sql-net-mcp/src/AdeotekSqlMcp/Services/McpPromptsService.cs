using AdeotekSqlMcp.Models;
using AdeotekSqlMcp.Utilities;

namespace AdeotekSqlMcp.Services;

/// <summary>
/// Service for MCP prompts implementation
/// </summary>
public sealed class McpPromptsService
{
    /// <summary>
    /// Gets all available MCP prompts
    /// </summary>
    public IReadOnlyList<McpPrompt> GetPrompts()
    {
        return new[]
        {
            new McpPrompt
            {
                Name = "analyze-schema",
                Description = "Analyze database schema and provide insights about structure, relationships, and potential issues",
                Arguments = new[]
                {
                    new McpPromptArgument
                    {
                        Name = "database",
                        Description = "Database to analyze",
                        Required = true
                    },
                    new McpPromptArgument
                    {
                        Name = "focus",
                        Description = "Specific area to focus on (tables, relationships, indexes, etc.)",
                        Required = false
                    }
                }
            },
            new McpPrompt
            {
                Name = "query-assistant",
                Description = "Help construct SQL queries based on natural language requirements",
                Arguments = new[]
                {
                    new McpPromptArgument
                    {
                        Name = "database",
                        Description = "Target database",
                        Required = true
                    },
                    new McpPromptArgument
                    {
                        Name = "requirement",
                        Description = "Natural language description of what to query",
                        Required = true
                    }
                }
            },
            new McpPrompt
            {
                Name = "performance-review",
                Description = "Review query performance and suggest optimizations",
                Arguments = new[]
                {
                    new McpPromptArgument
                    {
                        Name = "database",
                        Description = "Database name",
                        Required = true
                    },
                    new McpPromptArgument
                    {
                        Name = "query",
                        Description = "SQL query to analyze",
                        Required = true
                    }
                }
            }
        };
    }

    /// <summary>
    /// Gets a prompt with arguments substituted
    /// </summary>
    public string GetPrompt(string promptName, Dictionary<string, string>? arguments)
    {
        arguments ??= new Dictionary<string, string>();

        return promptName switch
        {
            "analyze-schema" => GetAnalyzeSchemaPrompt(arguments),
            "query-assistant" => GetQueryAssistantPrompt(arguments),
            "performance-review" => GetPerformanceReviewPrompt(arguments),
            _ => throw new PromptNotFoundException(promptName)
        };
    }

    private static string GetAnalyzeSchemaPrompt(Dictionary<string, string> arguments)
    {
        var database = GetRequiredArgument(arguments, "database");
        var focus = GetOptionalArgument(arguments, "focus") ?? "comprehensive analysis";

        return $@"Please analyze the '{database}' database schema with focus on: {focus}

Use the following tools to gather information:
1. Use sql_list_tables to get all tables in the database
2. Use sql_describe_table for detailed schema information on key tables
3. Analyze the structure and provide insights

Your analysis should include:
1. **Schema Overview**: List of tables, views, and their purposes
2. **Data Integrity**: Analysis of primary keys, foreign keys, and constraints
3. **Relationships**: How tables relate to each other (one-to-one, one-to-many, many-to-many)
4. **Indexing Strategy**: Review of indexes and recommendations
5. **Naming Conventions**: Assessment of naming consistency
6. **Normalization**: Database normalization level and potential issues
7. **Recommendations**: Specific suggestions for improvements

Please be thorough and provide actionable insights.";
    }

    private static string GetQueryAssistantPrompt(Dictionary<string, string> arguments)
    {
        var database = GetRequiredArgument(arguments, "database");
        var requirement = GetRequiredArgument(arguments, "requirement");

        return $@"Help me construct a SQL query for the '{database}' database.

Requirement: {requirement}

Steps to follow:
1. Use sql_list_tables to understand available tables
2. Use sql_describe_table to understand table structures and relationships
3. Construct an appropriate SELECT query that:
   - Retrieves the requested data
   - Uses proper JOINs if multiple tables are needed
   - Includes appropriate WHERE clauses
   - Has proper column selection (avoid SELECT *)
   - Includes LIMIT clause for safety
   - Follows read-only best practices

Please provide:
1. **Query**: The complete SQL query
2. **Explanation**: Step-by-step explanation of what the query does
3. **Assumptions**: Any assumptions made about the data or requirements
4. **Optimizations**: Suggestions for better performance
5. **Test**: Example of how to test the query

Remember: This is a read-only server, so only SELECT queries are allowed.";
    }

    private static string GetPerformanceReviewPrompt(Dictionary<string, string> arguments)
    {
        var database = GetRequiredArgument(arguments, "database");
        var query = GetRequiredArgument(arguments, "query");

        return $@"Please review the performance of this SQL query on the '{database}' database:

Query:
```sql
{query}
```

Steps to analyze:
1. Use sql_get_query_plan to get the execution plan
2. Use sql_describe_table to understand the tables involved
3. Analyze the query structure

Your performance review should include:
1. **Execution Plan Analysis**: Interpretation of the query plan
   - Sequential scans vs index scans
   - Join methods used
   - Estimated vs actual rows
   - Bottlenecks identified

2. **Performance Bottlenecks**:
   - Missing indexes
   - Inefficient JOINs
   - Large table scans
   - Expensive operations

3. **Index Recommendations**:
   - Suggested indexes to create
   - Columns to include in indexes
   - Expected performance improvement

4. **Query Optimization**:
   - Alternative query structures
   - Better JOIN orders
   - Filtering improvements
   - Subquery optimization

5. **Estimated Impact**:
   - Expected performance improvement
   - Trade-offs to consider
   - When to apply optimizations

Please provide specific, actionable recommendations with SQL statements where applicable.";
    }

    private static string GetRequiredArgument(Dictionary<string, string> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required argument: {key}");
        }
        return value;
    }

    private static string? GetOptionalArgument(Dictionary<string, string> arguments, string key)
    {
        return arguments.TryGetValue(key, out var value) ? value : null;
    }
}
