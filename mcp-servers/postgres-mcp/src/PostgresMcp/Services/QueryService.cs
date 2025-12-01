using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;
using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for executing read-only database queries with comprehensive safety validation.
/// </summary>
public class QueryService(
    ILogger<QueryService> logger,
    IOptions<PostgresOptions> postgresOptions,
    IOptions<SecurityOptions> securityOptions,
    IOptions<LoggingOptions> loggingOptions)
    : IQueryService
{
    private readonly PostgresOptions _postgresOptions = postgresOptions.Value;
    private readonly SecurityOptions _securityOptions = securityOptions.Value;
    private readonly LoggingOptions _loggingOptions = loggingOptions.Value;

    /// <inheritdoc/>
    public async Task<QueryResult> ExecuteQueryAsync(
        string connectionString,
        string sql,
        CancellationToken cancellationToken = default)
    {
        // Validate query safety first
        if (!ValidateQuerySafety(sql))
        {
            throw new InvalidOperationException("Query failed safety validation. Only SELECT queries are allowed.");
        }

        if (_loggingOptions.LogQueries)
        {
            logger.LogInformation("Executing query: {Sql}", sql);
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new QueryResult();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, connection)
        {
            CommandTimeout = _securityOptions.MaxQueryExecutionSeconds
        };

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        // Get column names
        for (int i = 0; i < reader.FieldCount; i++)
        {
            result.Columns.Add(reader.GetName(i));
        }

        // Read rows
        int rowCount = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (rowCount >= _securityOptions.MaxRowsPerQuery)
            {
                result.IsTruncated = true;
                break;
            }

            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[result.Columns[i]] = value;
            }

            result.Rows.Add(row);
            rowCount++;
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        if (_loggingOptions.LogResults)
        {
            logger.LogInformation("Query returned {RowCount} rows in {Ms}ms",
                result.RowCount, result.ExecutionTimeMs);
        }

        return result;
    }

    /// <inheritdoc/>
    public bool ValidateQuerySafety(string sql)
    {
        try
        {
            var normalizedQuery = sql.ToUpperInvariant().Trim();

            // Remove comments and extra whitespace
            normalizedQuery = RemoveComments(normalizedQuery);
            normalizedQuery = Regex.Replace(normalizedQuery, @"\s+", " ");

            // Must be a SELECT or WITH query
            if (!normalizedQuery.StartsWith("SELECT") && !normalizedQuery.StartsWith("WITH"))
            {
                logger.LogWarning("Query rejected: Must start with SELECT or WITH");
                return false;
            }

            // CRITICAL: Block all data modification keywords
            string[] dataModificationKeywords = [
                "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "UPSERT"
            ];

            if (dataModificationKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                logger.LogWarning("Query rejected: Contains data modification keyword");
                return false;
            }

            // CRITICAL: Block all schema modification keywords
            string[] schemaModificationKeywords = [
                "CREATE", "ALTER", "DROP", "RENAME", "GRANT", "REVOKE",
                "TRUNCATE", "COMMENT ON"
            ];

            if (schemaModificationKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                logger.LogWarning("Query rejected: Contains schema modification keyword");
                return false;
            }

            // Block dangerous functions that can modify data or schema
            string[] dangerousFunctions = [
                "pg_read_file", "pg_write_file", "pg_ls_dir", "COPY",
                "pg_execute", "pg_read_binary_file", "pg_stat_file",
                "pg_terminate_backend", "pg_cancel_backend",
                "pg_reload_conf", "pg_rotate_logfile",
                "pg_create_restore_point", "pg_start_backup", "pg_stop_backup",
                "pg_switch_wal", "pg_create_physical_replication_slot",
                "pg_drop_replication_slot", "pg_replication_origin_create",
                "pg_replication_origin_drop"
            ];

            if (dangerousFunctions.Any(func => normalizedQuery.Contains(func.ToUpperInvariant())))
            {
                logger.LogWarning("Query rejected: Contains dangerous function");
                return false;
            }

            // Block procedural code execution
            if (normalizedQuery.Contains("$$") ||
                Regex.IsMatch(normalizedQuery, @"\bDO\s+\$"))
            {
                logger.LogWarning("Query rejected: Contains procedural code");
                return false;
            }

            // Block transaction control (not needed for read-only)
            string[] transactionKeywords = [
                "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "START TRANSACTION"
            ];

            if (transactionKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                logger.LogWarning("Query rejected: Contains transaction control");
                return false;
            }

            // Block LOCK statements
            if (Regex.IsMatch(normalizedQuery, @"\bLOCK\s+TABLE"))
            {
                logger.LogWarning("Query rejected: Contains LOCK statement");
                return false;
            }

            // Block VACUUM, ANALYZE, REINDEX
            string[] maintenanceKeywords = ["VACUUM", "ANALYZE", "REINDEX", "CLUSTER"];
            if (maintenanceKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                logger.LogWarning("Query rejected: Contains maintenance command");
                return false;
            }

            // Block LISTEN/NOTIFY/UNLISTEN
            string[] messagingKeywords = ["LISTEN", "NOTIFY", "UNLISTEN"];
            if (messagingKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                logger.LogWarning("Query rejected: Contains messaging command");
                return false;
            }

            // Block SET commands (configuration changes)
            if (Regex.IsMatch(normalizedQuery, @"\bSET\s+"))
            {
                logger.LogWarning("Query rejected: Contains SET command");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating query safety");
            return false;
        }
    }

    private string RemoveComments(string sql)
    {
        // Remove single-line comments (-- comment)
        sql = Regex.Replace(sql, @"--[^\r\n]*", "");

        // Remove multi-line comments (/* comment */)
        sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);

        return sql;
    }
}
