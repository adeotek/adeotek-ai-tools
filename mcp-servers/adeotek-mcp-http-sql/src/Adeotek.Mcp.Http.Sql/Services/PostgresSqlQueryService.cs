using System.Diagnostics;
using System.Text.RegularExpressions;
using Adeotek.Mcp.Http.Sql.Models;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Adeotek.Mcp.Http.Sql.Services;

public partial class PostgresQueryService(
    IOptions<DatabaseServerOptions> databaseServerOptions,
    ILogger<PostgresQueryService> logger)
    : ISqlQueryService
{
    private readonly DatabaseServerOptions _databaseServerOptions = databaseServerOptions.Value;

    public async Task<SqlQueryResult> ExecuteQueryAsync(string sql, string? database = null, CancellationToken cancellationToken = default)
    {
        if (!ValidateQuerySafety(sql))
        {
            throw new InvalidOperationException("Query failed safety validation. Only SELECT queries are allowed.");
        }

        if (_databaseServerOptions.LogQueries)
        {
            LogExecutingQuery(sql);
        }

        var stopwatch = Stopwatch.StartNew();
        var result = new SqlQueryResult();

        // Create and open connection
        await using var connection = new NpgsqlConnection(BuildConnectionString());
        await connection.OpenAsync(cancellationToken);

        // Create and execute command
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = _databaseServerOptions.CommandTimeoutSeconds;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        // Get column names
        for (var i = 0; i < reader.FieldCount; i++)
        {
            result.Columns.Add(reader.GetName(i));
        }

        // Read rows
        var rowCount = 0;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (rowCount >= _databaseServerOptions.MaxRowsPerQuery)
            {
                result.IsTruncated = true;
                break;
            }

            var row = new Dictionary<string, object?>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[result.Columns[i]] = value;
            }

            result.Rows.Add(row);
            rowCount++;
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

        if (_databaseServerOptions.LogResults)
        {
            LogQueryResults(result.RowCount, result.ExecutionTimeMs);
        }

        return result;
    }

    private string BuildConnectionString(string? database = null)
    {
        if (string.IsNullOrEmpty(_databaseServerOptions.ConnectionString))
        {
            throw new InvalidOperationException("ConnectionString is not configured.");
        }

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(_databaseServerOptions.ConnectionString)
        {
            Database = string.IsNullOrEmpty(database) ? "postgres" : database,
            Timeout = _databaseServerOptions.ConnectionTimeoutSeconds,
            CommandTimeout = _databaseServerOptions.CommandTimeoutSeconds,
            MaxPoolSize = _databaseServerOptions.MaxPoolSize,
            MinPoolSize = _databaseServerOptions.MinPoolSize,
            // Configure SSL from server options (takes precedence)
            SslMode = _databaseServerOptions.UseSsl ? SslMode.Prefer : SslMode.Disable
        };

        return connectionStringBuilder.ToString();
    }

    private bool ValidateQuerySafety(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            LogQueryValidationFailed();
            return false;
        }

        try
        {
            var normalizedQuery = sql.ToUpperInvariant().Trim();

            // Remove comments and extra whitespace
            normalizedQuery = RemoveComments(normalizedQuery);
            normalizedQuery = WhiteSpaceRegex().Replace(normalizedQuery, " ");

            // Must be a SELECT or WITH query
            if (!normalizedQuery.StartsWith("SELECT") && !normalizedQuery.StartsWith("WITH"))
            {
                LogQueryRejectedMustBeSelect();
                return false;
            }

            // CRITICAL: Block all data modification keywords
            string[] dataModificationKeywords = [
                "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "UPSERT"
            ];

            if (dataModificationKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                LogQueryRejectedDataModification();
                return false;
            }

            // CRITICAL: Block all schema modification keywords
            string[] schemaModificationKeywords = [
                "CREATE", "ALTER", "DROP", "RENAME", "GRANT", "REVOKE",
                "TRUNCATE", "COMMENT ON"
            ];

            if (schemaModificationKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                LogQueryRejectedSchemaModification();
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
                LogQueryRejectedDangerousFunction();
                return false;
            }

            // Block procedural code execution
            if (normalizedQuery.Contains("$$") ||
                SqlDoRegex().IsMatch(normalizedQuery))
            {
                LogQueryRejectedProceduralCode();
                return false;
            }

            // Block transaction control (not needed for read-only)
            string[] transactionKeywords = [
                "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "START TRANSACTION"
            ];

            if (transactionKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                LogQueryRejectedTransactionControl();
                return false;
            }

            // Block LOCK statements
            if (Regex.IsMatch(normalizedQuery, @"\bLOCK\s+TABLE"))
            {
                LogQueryRejectedLockStatement();
                return false;
            }

            // Block VACUUM, ANALYZE, REINDEX
            string[] maintenanceKeywords = ["VACUUM", "ANALYZE", "REINDEX", "CLUSTER"];
            if (maintenanceKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                LogQueryRejectedMaintenanceCommand();
                return false;
            }

            // Block LISTEN/NOTIFY/UNLISTEN
            string[] messagingKeywords = ["LISTEN", "NOTIFY", "UNLISTEN"];
            if (messagingKeywords.Any(kw => Regex.IsMatch(normalizedQuery, $@"\b{kw}\b")))
            {
                LogQueryRejectedMessagingCommand();
                return false;
            }

            // Block SET commands (configuration changes)
            if (SqlSetRegex().IsMatch(normalizedQuery))
            {
                LogQueryRejectedSetCommand();
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            LogErrorValidatingQuery(ex);
            return false;
        }
    }

    private static string RemoveComments(string sql)
    {
        // Remove single-line comments (-- comment)
        sql = SingleLineCommentRegex().Replace(sql, "");
        // Remove multi-line comments (/* comment */)
        sql = MultiLineCommentRegex().Replace(sql, "");
        return sql;
    }

    [GeneratedRegex(@"--[^\r\n]*")]
    private static partial Regex SingleLineCommentRegex();
    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex MultiLineCommentRegex();
    [GeneratedRegex(@"\bSET\s+")]
    private static partial Regex SqlSetRegex();
    [GeneratedRegex(@"\bDO\s+\$")]
    private static partial Regex SqlDoRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpaceRegex();

    // High-performance logging using LoggerMessage source generator
    [LoggerMessage(Level = LogLevel.Information, Message = "Executing query: {Sql}")]
    private partial void LogExecutingQuery(string sql);

    [LoggerMessage(Level = LogLevel.Information, Message = "Query returned {RowCount} rows in {ExecutionTimeMs}ms")]
    private partial void LogQueryResults(int rowCount, long executionTimeMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query validation failed: Query is null or empty")]
    private partial void LogQueryValidationFailed();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Must start with SELECT or WITH")]
    private partial void LogQueryRejectedMustBeSelect();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains data modification keyword")]
    private partial void LogQueryRejectedDataModification();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains schema modification keyword")]
    private partial void LogQueryRejectedSchemaModification();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains dangerous function")]
    private partial void LogQueryRejectedDangerousFunction();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains procedural code")]
    private partial void LogQueryRejectedProceduralCode();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains transaction control")]
    private partial void LogQueryRejectedTransactionControl();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains LOCK statement")]
    private partial void LogQueryRejectedLockStatement();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains maintenance command")]
    private partial void LogQueryRejectedMaintenanceCommand();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains messaging command")]
    private partial void LogQueryRejectedMessagingCommand();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query rejected: Contains SET command")]
    private partial void LogQueryRejectedSetCommand();

    [LoggerMessage(Level = LogLevel.Error, Message = "Error validating query safety")]
    private partial void LogErrorValidatingQuery(Exception ex);
}
