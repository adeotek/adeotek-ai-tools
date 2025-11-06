using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using PostgresMcp.Models;
using System.Text.RegularExpressions;

namespace PostgresMcp.Services;

/// <summary>
/// Service for generating SQL queries from natural language using AI.
/// </summary>
public class SqlGenerationService(
    ILogger<SqlGenerationService> logger,
    IDatabaseSchemaService schemaService,
    IQueryService queryService,
    IOptions<SecurityOptions> securityOptions,
    IOptions<AiOptions> aiOptions,
    Kernel? kernel = null) : ISqlGenerationService
{
    private readonly ILogger<SqlGenerationService> _logger = logger;
    private readonly IDatabaseSchemaService _schemaService = schemaService;
    private readonly IQueryService _queryService = queryService;
    private readonly SecurityOptions _securityOptions = securityOptions.Value;
    private readonly AiOptions _aiOptions = aiOptions.Value;
    private readonly Kernel? _kernel = kernel;

    /// <inheritdoc/>
    public async Task<SqlGenerationResult> GenerateAndExecuteQueryAsync(
        string connectionString,
        string naturalLanguageQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating SQL from natural language: {Query}", naturalLanguageQuery);

        if (_kernel == null || !_aiOptions.Enabled)
        {
            throw new InvalidOperationException(
                "AI features are not configured or disabled. Cannot generate SQL from natural language.");
        }

        // Get database schema
        var schema = await _schemaService.ScanDatabaseSchemaAsync(
            connectionString,
            null,
            cancellationToken);

        // Generate SQL query
        var (sqlQuery, explanation, confidence) = await GenerateSqlWithExplanationAsync(
            schema,
            naturalLanguageQuery,
            cancellationToken);

        _logger.LogInformation("Generated SQL: {Sql}", sqlQuery);

        // Validate safety
        var isSafe = ValidateSqlSafety(sqlQuery);
        if (!isSafe)
        {
            return new SqlGenerationResult
            {
                SqlQuery = sqlQuery,
                Explanation = explanation,
                IsSafe = false,
                ConfidenceScore = confidence,
                Warnings = ["Query failed safety validation"]
            };
        }

        // Optimize query
        var optimizedSql = await OptimizeQueryAsync(sqlQuery, cancellationToken);
        List<string> warnings = [];
        if (optimizedSql != sqlQuery)
        {
            warnings.Add("Query was optimized for better performance");
            sqlQuery = optimizedSql;
        }

        // Execute query
        QueryResult? results = null;
        try
        {
            results = await _queryService.ExecuteQueryAsync(
                connectionString,
                sqlQuery,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing generated SQL");
            warnings.Add($"Execution error: {ex.Message}");
        }

        return new SqlGenerationResult
        {
            SqlQuery = sqlQuery,
            Explanation = explanation,
            Results = results,
            IsSafe = true,
            ConfidenceScore = confidence,
            Warnings = warnings.Any() ? warnings : null
        };
    }

    /// <inheritdoc/>
    public bool ValidateSqlSafety(string sqlQuery)
    {
        try
        {
            var normalizedQuery = sqlQuery.ToUpperInvariant().Trim();

            // Must be a SELECT or WITH query
            if (!normalizedQuery.StartsWith("SELECT") && !normalizedQuery.StartsWith("WITH"))
            {
                return false;
            }

            // Check for data modification keywords
            string[] dataModificationKeywords = ["INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE"];
            if (dataModificationKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                return false;
            }

            // Check for schema modification keywords
            string[] schemaModificationKeywords = ["CREATE", "ALTER", "DROP", "RENAME"];
            if (schemaModificationKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                return false;
            }

            // Check for dangerous functions
            string[] dangerousFunctions =
            [
                "pg_read_file", "pg_write_file", "pg_ls_dir", "COPY",
                "pg_execute", "pg_read_binary_file", "pg_stat_file"
            ];
            if (dangerousFunctions.Any(func => normalizedQuery.Contains(func.ToUpperInvariant())))
            {
                return false;
            }

            // Check for command execution attempts
            if (normalizedQuery.Contains("$$") || normalizedQuery.Contains("DO "))
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SQL safety");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string> OptimizeQueryAsync(
        string sqlQuery,
        CancellationToken cancellationToken = default)
    {
        if (_kernel == null || !_aiOptions.Enabled)
        {
            return sqlQuery;
        }

        try
        {
            var prompt = $"""
                You are a PostgreSQL query optimization expert. Analyze and optimize the following SQL query.

                Original Query:
                {sqlQuery}

                Optimization Guidelines:
                1. Ensure proper use of indexes
                2. Avoid SELECT *
                3. Use appropriate JOIN types
                4. Add WHERE clauses early in the execution
                5. Use LIMIT when appropriate
                6. Avoid subqueries when JOINs are more efficient
                7. Keep the query functionality exactly the same

                Return ONLY the optimized SQL query, no explanations or markdown formatting.
                If the query is already optimal, return it unchanged.
                """;

            var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
            var optimizedSql = response.ToString().Trim();

            // Clean up markdown if present
            optimizedSql = Regex.Replace(optimizedSql, @"```sql\s*|\s*```", "", RegexOptions.IgnoreCase);
            optimizedSql = optimizedSql.Trim();

            // Validate the optimized query is still safe
            if (!ValidateSqlSafety(optimizedSql))
            {
                _logger.LogWarning("Optimized query failed safety validation, returning original");
                return sqlQuery;
            }

            return optimizedSql;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing query, returning original");
            return sqlQuery;
        }
    }

    private async Task<(string SqlQuery, string Explanation, double Confidence)> GenerateSqlWithExplanationAsync(
        DatabaseSchema schema,
        string naturalLanguageQuery,
        CancellationToken cancellationToken)
    {
        if (_kernel == null)
        {
            throw new InvalidOperationException("AI kernel is not configured");
        }

        var schemaContext = FormatSchemaForAi(schema);

        var prompt = $"""
            You are a PostgreSQL expert. Generate a SQL query from the natural language description.

            Database Schema:
            {schemaContext}

            Requirements:
            1. Generate ONLY SELECT queries (no data or schema modifications)
            2. Use proper JOIN syntax for related tables
            3. Include appropriate WHERE, GROUP BY, HAVING, ORDER BY as needed
            4. Use meaningful column aliases
            5. Follow PostgreSQL best practices
            6. Consider foreign key relationships

            Natural Language Request: {naturalLanguageQuery}

            Respond in the following format:
            SQL:
            <the SQL query>

            EXPLANATION:
            <brief explanation of what the query does and why>

            CONFIDENCE:
            <confidence score from 0.0 to 1.0>
            """;

        var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var responseText = response.ToString();

        // Parse the response
        var sqlMatch = Regex.Match(responseText, @"SQL:\s*(.+?)(?=EXPLANATION:|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var explanationMatch = Regex.Match(responseText, @"EXPLANATION:\s*(.+?)(?=CONFIDENCE:|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var confidenceMatch = Regex.Match(responseText, @"CONFIDENCE:\s*([\d.]+)", RegexOptions.IgnoreCase);

        var sqlQuery = sqlMatch.Success ? sqlMatch.Groups[1].Value.Trim() : responseText;
        var explanation = explanationMatch.Success ? explanationMatch.Groups[1].Value.Trim() : "No explanation provided";
        var confidence = confidenceMatch.Success && double.TryParse(confidenceMatch.Groups[1].Value, out var conf)
            ? conf
            : 0.7;

        // Clean up SQL (remove markdown code blocks)
        sqlQuery = Regex.Replace(sqlQuery, @"```sql\s*|\s*```", "", RegexOptions.IgnoreCase);
        sqlQuery = sqlQuery.Trim();

        return (sqlQuery, explanation, confidence);
    }

    private string FormatSchemaForAi(DatabaseSchema schema)
    {
        var schemaText = new System.Text.StringBuilder();
        schemaText.AppendLine($"Database: PostgreSQL {schema.ServerVersion}");
        schemaText.AppendLine($"Tables: {schema.TableCount}");
        schemaText.AppendLine();

        foreach (var table in schema.Tables)
        {
            schemaText.AppendLine($"Table: {table.FullName}");
            if (!string.IsNullOrEmpty(table.Comment))
            {
                schemaText.AppendLine($"  Description: {table.Comment}");
            }

            schemaText.AppendLine("  Columns:");
            foreach (var col in table.Columns)
            {
                List<string> attrs = [];
                if (!col.IsNullable) attrs.Add("NOT NULL");
                if (col.IsIdentity) attrs.Add("IDENTITY");
                if (col.DefaultValue != null) attrs.Add($"DEFAULT {col.DefaultValue}");

                var attrsStr = attrs.Count != 0 ? $" ({string.Join(", ", attrs)})" : "";
                schemaText.AppendLine($"    {col.ColumnName}: {col.DataType}{attrsStr}");
            }

            if (table.PrimaryKey != null)
            {
                schemaText.AppendLine($"  Primary Key: {string.Join(", ", table.PrimaryKey.Columns)}");
            }

            if (table.ForeignKeys?.Any() == true)
            {
                schemaText.AppendLine("  Foreign Keys:");
                foreach (var fk in table.ForeignKeys)
                {
                    schemaText.AppendLine($"    {fk.ColumnName} -> {fk.ReferencedSchema}.{fk.ReferencedTable}({fk.ReferencedColumn})");
                }
            }

            if (table.Indexes?.Any() == true)
            {
                var nonPrimaryIndexes = table.Indexes.Where(i => !i.IsPrimary).ToList();
                if (nonPrimaryIndexes.Any())
                {
                    schemaText.AppendLine("  Indexes:");
                    foreach (var idx in nonPrimaryIndexes)
                    {
                        var uniqueStr = idx.IsUnique ? "UNIQUE " : "";
                        schemaText.AppendLine($"    {uniqueStr}{idx.IndexName} on ({string.Join(", ", idx.Columns)})");
                    }
                }
            }

            schemaText.AppendLine();
        }

        if (schema.Relationships?.Any() == true)
        {
            schemaText.AppendLine("Relationships:");
            foreach (var rel in schema.Relationships)
            {
                schemaText.AppendLine($"  {rel.SourceTable} -> {rel.TargetTable} ({rel.RelationType})");
            }
        }

        return schemaText.ToString();
    }
}
