using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Npgsql;
using PostgresMcp.Models;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PostgresMcp.Services;

/// <summary>
/// Service for executing database queries with relationship awareness.
/// </summary>
public class QueryService : IQueryService
{
    private readonly ILogger<QueryService> _logger;
    private readonly IDatabaseSchemaService _schemaService;
    private readonly SecurityOptions _securityOptions;
    private readonly Kernel? _kernel;

    public QueryService(
        ILogger<QueryService> logger,
        IDatabaseSchemaService schemaService,
        IOptions<SecurityOptions> securityOptions,
        Kernel? kernel = null)
    {
        _logger = logger;
        _schemaService = schemaService;
        _securityOptions = securityOptions.Value;
        _kernel = kernel;
    }

    /// <inheritdoc/>
    public async Task<QueryResult> QueryDataAsync(
        string connectionString,
        string naturalLanguageQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing natural language query: {Query}", naturalLanguageQuery);

        // Get schema context
        var schema = await _schemaService.ScanDatabaseSchemaAsync(
            connectionString,
            null,
            cancellationToken);

        // Generate SQL from natural language
        var sqlQuery = await GenerateSqlFromNaturalLanguageAsync(
            schema,
            naturalLanguageQuery,
            cancellationToken);

        _logger.LogInformation("Generated SQL: {Sql}", sqlQuery);

        // Execute the query
        return await ExecuteQueryAsync(connectionString, sqlQuery, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteQueryAsync(
        string connectionString,
        string sqlQuery,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing SQL query");

        // Validate query safety
        ValidateQuerySafety(sqlQuery);

        var stopwatch = Stopwatch.StartNew();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Apply query timeout
        await using var cmd = new NpgsqlCommand(sqlQuery, connection)
        {
            CommandTimeout = _securityOptions.MaxQueryExecutionSeconds
        };

        // Add row limit if not present
        var limitedQuery = EnsureRowLimit(sqlQuery, _securityOptions.MaxRowsPerQuery);
        cmd.CommandText = limitedQuery;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var columns = new List<string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        var rows = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[columns[i]] = value;
            }
            rows.Add(row);

            // Safety check: don't exceed max rows
            if (rows.Count >= _securityOptions.MaxRowsPerQuery)
                break;
        }

        stopwatch.Stop();

        var relatedTables = ExtractTableNames(sqlQuery);

        return new QueryResult
        {
            Columns = columns,
            Rows = rows,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            RelatedTables = relatedTables
        };
    }

    private async Task<string> GenerateSqlFromNaturalLanguageAsync(
        DatabaseSchema schema,
        string naturalLanguageQuery,
        CancellationToken cancellationToken)
    {
        if (_kernel == null)
        {
            throw new InvalidOperationException(
                "AI features are not configured. Cannot generate SQL from natural language.");
        }

        var schemaContext = FormatSchemaForSqlGeneration(schema);

        var prompt = $"""
            You are a PostgreSQL expert. Generate a SQL query based on the natural language request.

            Database Schema:
            {schemaContext}

            Rules:
            1. Generate only SELECT queries (no INSERT, UPDATE, DELETE, DROP, CREATE, ALTER)
            2. Use proper JOINs when querying related tables
            3. Include appropriate WHERE clauses for filtering
            4. Use column aliases for clarity
            5. Return ONLY the SQL query, no explanations or markdown
            6. Use proper PostgreSQL syntax
            7. Consider foreign key relationships when joining tables

            Natural Language Request: {naturalLanguageQuery}

            SQL Query:
            """;

        var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        var sql = response.ToString().Trim();

        // Clean up the response (remove markdown code blocks if present)
        sql = Regex.Replace(sql, @"```sql\s*|\s*```", "", RegexOptions.IgnoreCase);
        sql = sql.Trim();

        return sql;
    }

    private void ValidateQuerySafety(string sqlQuery)
    {
        var normalizedQuery = sqlQuery.ToUpperInvariant().Trim();

        // Check for data modification
        if (!_securityOptions.AllowDataModification)
        {
            var dataModificationKeywords = new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE" };
            foreach (var keyword in dataModificationKeywords)
            {
                if (Regex.IsMatch(normalizedQuery, $@"\b{keyword}\b"))
                {
                    throw new SecurityException(
                        $"Data modification queries are not allowed. Found: {keyword}");
                }
            }
        }

        // Check for schema modification
        if (!_securityOptions.AllowSchemaModification)
        {
            var schemaModificationKeywords = new[] { "CREATE", "ALTER", "DROP", "RENAME" };
            foreach (var keyword in schemaModificationKeywords)
            {
                if (Regex.IsMatch(normalizedQuery, $@"\b{keyword}\b"))
                {
                    throw new SecurityException(
                        $"Schema modification queries are not allowed. Found: {keyword}");
                }
            }
        }

        // Check for dangerous functions
        var dangerousFunctions = new[] { "pg_read_file", "pg_write_file", "pg_ls_dir", "COPY" };
        foreach (var func in dangerousFunctions)
        {
            if (normalizedQuery.Contains(func.ToUpperInvariant()))
            {
                throw new SecurityException(
                    $"Dangerous function or command not allowed: {func}");
            }
        }

        // Ensure it's a SELECT query
        if (!normalizedQuery.TrimStart().StartsWith("SELECT") &&
            !normalizedQuery.TrimStart().StartsWith("WITH"))
        {
            throw new SecurityException(
                "Only SELECT queries (and WITH clauses) are allowed.");
        }
    }

    private string EnsureRowLimit(string sqlQuery, int maxRows)
    {
        // Check if query already has a LIMIT clause
        if (Regex.IsMatch(sqlQuery, @"\bLIMIT\s+\d+", RegexOptions.IgnoreCase))
        {
            // Validate the existing limit doesn't exceed max
            var match = Regex.Match(sqlQuery, @"\bLIMIT\s+(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int existingLimit))
            {
                if (existingLimit <= maxRows)
                    return sqlQuery;

                // Replace with max limit
                return Regex.Replace(
                    sqlQuery,
                    @"\bLIMIT\s+\d+",
                    $"LIMIT {maxRows}",
                    RegexOptions.IgnoreCase);
            }
        }

        // Add LIMIT clause
        return $"{sqlQuery.TrimEnd(';')} LIMIT {maxRows}";
    }

    private List<string> ExtractTableNames(string sqlQuery)
    {
        var tables = new List<string>();

        // Match table names in FROM and JOIN clauses
        var patterns = new[]
        {
            @"\bFROM\s+([a-zA-Z_][a-zA-Z0-9_]*\.)?([a-zA-Z_][a-zA-Z0-9_]*)",
            @"\bJOIN\s+([a-zA-Z_][a-zA-Z0-9_]*\.)?([a-zA-Z_][a-zA-Z0-9_]*)"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(sqlQuery, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                var schema = match.Groups[1].Success
                    ? match.Groups[1].Value.TrimEnd('.')
                    : "public";
                var table = match.Groups[2].Value;
                var fullName = $"{schema}.{table}";

                if (!tables.Contains(fullName))
                    tables.Add(fullName);
            }
        }

        return tables;
    }

    private string FormatSchemaForSqlGeneration(DatabaseSchema schema)
    {
        var schemaText = new System.Text.StringBuilder();

        foreach (var table in schema.Tables)
        {
            schemaText.AppendLine($"Table: {table.FullName}");

            // Columns
            schemaText.AppendLine("Columns:");
            foreach (var col in table.Columns)
            {
                var nullable = col.IsNullable ? "NULL" : "NOT NULL";
                schemaText.AppendLine($"  {col.ColumnName} {col.DataType} {nullable}");
            }

            // Primary key
            if (table.PrimaryKey != null)
            {
                schemaText.AppendLine($"Primary Key: {string.Join(", ", table.PrimaryKey.Columns)}");
            }

            // Foreign keys (relationships)
            if (table.ForeignKeys?.Any() == true)
            {
                schemaText.AppendLine("Foreign Keys:");
                foreach (var fk in table.ForeignKeys)
                {
                    schemaText.AppendLine(
                        $"  {fk.ColumnName} -> {fk.ReferencedSchema}.{fk.ReferencedTable}({fk.ReferencedColumn})");
                }
            }

            schemaText.AppendLine();
        }

        return schemaText.ToString();
    }
}
