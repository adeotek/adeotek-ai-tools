using System.Diagnostics;
using Npgsql;
using AdeotekSqlMcp.Models;
using AdeotekSqlMcp.Utilities;
using Serilog.Core;

namespace AdeotekSqlMcp.Database;

/// <summary>
/// PostgreSQL database implementation
/// </summary>
public sealed class PostgresDatabase : IDatabase
{
    private readonly DatabaseConfig _config;
    private readonly Logger _logger;
    private NpgsqlConnection? _connection;
    private bool _disposed;

    public string DatabaseType => "postgres";

    public PostgresDatabase(DatabaseConfig config, Logger logger)
    {
        if (config.Type != "postgres")
        {
            throw new ArgumentException($"Invalid database type: {config.Type}. Expected: postgres");
        }

        _config = config;
        _logger = logger;
    }

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var conn = await GetConnectionAsync(cancellationToken);
            await using var cmd = new NpgsqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);
            _logger.Information("PostgreSQL connection test successful");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "PostgreSQL connection test failed");
            throw new DatabaseConnectionException("Connection test failed", ex);
        }
    }

    public async Task<IReadOnlyList<DatabaseInfo>> ListDatabasesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var conn = await GetConnectionAsync(cancellationToken);

            var query = @"
                SELECT
                    datname as name,
                    pg_size_pretty(pg_database_size(datname)) as size,
                    pg_catalog.pg_get_userbyid(datdba) as owner,
                    pg_encoding_to_char(encoding) as encoding,
                    datcollate as collation
                FROM pg_database
                WHERE datistemplate = false
                ORDER BY datname";

            await using var cmd = new NpgsqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var databases = new List<DatabaseInfo>();
            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(new DatabaseInfo
                {
                    Name = reader.GetString(0),
                    Size = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Owner = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Encoding = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Collation = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            _logger.Information("Listed {Count} databases", databases.Count);
            return databases;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to list databases");
            throw new QueryExecutionException("Failed to list databases", ex);
        }
    }

    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(string database, string? schema = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var conn = await GetConnectionAsync(database, cancellationToken);
            schema ??= "public";

            var query = @"
                SELECT
                    schemaname as schema,
                    tablename as name,
                    'table' as type,
                    (SELECT reltuples::bigint FROM pg_class WHERE oid = (quote_ident(schemaname) || '.' || quote_ident(tablename))::regclass) as row_count,
                    pg_size_pretty(pg_total_relation_size(quote_ident(schemaname) || '.' || quote_ident(tablename))) as size
                FROM pg_tables
                WHERE schemaname = @schema
                UNION ALL
                SELECT
                    schemaname as schema,
                    viewname as name,
                    'view' as type,
                    NULL as row_count,
                    NULL as size
                FROM pg_views
                WHERE schemaname = @schema
                ORDER BY schema, name";

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("schema", schema);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var tables = new List<TableInfo>();
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(new TableInfo
                {
                    Schema = reader.GetString(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2),
                    RowCount = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                    Size = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            _logger.Information("Listed {Count} tables in schema {Schema}", tables.Count, schema);
            return tables;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to list tables");
            throw new QueryExecutionException("Failed to list tables", ex);
        }
    }

    public async Task<TableSchema> DescribeTableAsync(string database, string schema, string table, CancellationToken cancellationToken = default)
    {
        try
        {
            var conn = await GetConnectionAsync(database, cancellationToken);

            // Get columns
            var columns = await GetColumnsAsync(conn, schema, table, cancellationToken);

            // Get indexes
            var indexes = await GetIndexesAsync(conn, schema, table, cancellationToken);

            // Get foreign keys
            var foreignKeys = await GetForeignKeysAsync(conn, schema, table, cancellationToken);

            // Get constraints
            var constraints = await GetConstraintsAsync(conn, schema, table, cancellationToken);

            _logger.Information("Described table {Schema}.{Table}", schema, table);

            return new TableSchema
            {
                Schema = schema,
                Table = table,
                Columns = columns,
                Indexes = indexes,
                ForeignKeys = foreignKeys,
                Constraints = constraints
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to describe table {Schema}.{Table}", schema, table);
            throw new QueryExecutionException($"Failed to describe table {schema}.{table}", ex);
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(string database, string query, int maxRows = 1000, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var conn = await GetConnectionAsync(database, cancellationToken);

            await using var cmd = new NpgsqlCommand(query, conn);
            cmd.CommandTimeout = _config.CommandTimeout;

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToList();

            var rows = new List<Dictionary<string, object?>>();
            var rowCount = 0;

            while (await reader.ReadAsync(cancellationToken) && rowCount < maxRows)
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
                rowCount++;
            }

            stopwatch.Stop();

            _logger.Information("Executed query, returned {RowCount} rows in {Ms}ms", rowCount, stopwatch.ElapsedMilliseconds);

            return new QueryResult
            {
                Columns = columns,
                Rows = rows,
                RowCount = rowCount,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Query execution failed after {Ms}ms", stopwatch.ElapsedMilliseconds);
            throw new QueryExecutionException("Query execution failed", ex);
        }
    }

    public async Task<string> GetQueryPlanAsync(string database, string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var conn = await GetConnectionAsync(database, cancellationToken);

            var explainQuery = $"EXPLAIN (FORMAT JSON, ANALYZE FALSE) {query}";

            await using var cmd = new NpgsqlCommand(explainQuery, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var plan = string.Empty;
            if (await reader.ReadAsync(cancellationToken))
            {
                plan = reader.GetString(0);
            }

            _logger.Information("Retrieved query plan");
            return plan;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get query plan");
            throw new QueryExecutionException("Failed to get query plan", ex);
        }
    }

    private async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await GetConnectionAsync(_config.Database, cancellationToken);
    }

    private async Task<NpgsqlConnection> GetConnectionAsync(string database, CancellationToken cancellationToken = default)
    {
        if (_connection?.Database == database && _connection.State == System.Data.ConnectionState.Open)
        {
            return _connection;
        }

        var sslMode = _config.UseSsl ? "Require" : "Prefer";
        var connStr = $"Host={_config.Host};Port={_config.Port};Database={database};Username={_config.User};Password={_config.Password};SSL Mode={sslMode};Timeout={_config.ConnectionTimeout};Command Timeout={_config.CommandTimeout}";

        _connection = new NpgsqlConnection(connStr);
        await _connection.OpenAsync(cancellationToken);

        return _connection;
    }

    private static async Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(NpgsqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                column_name,
                data_type,
                is_nullable,
                column_default,
                character_maximum_length,
                numeric_precision,
                numeric_scale
            FROM information_schema.columns
            WHERE table_schema = @schema AND table_name = @table
            ORDER BY ordinal_position";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var columns = new List<ColumnInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                Nullable = reader.GetString(2) == "YES",
                DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                MaxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Precision = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Scale = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                IsPrimaryKey = false,
                IsForeignKey = false
            });
        }

        return columns;
    }

    private static async Task<IReadOnlyList<IndexInfo>> GetIndexesAsync(NpgsqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                i.relname as index_name,
                array_agg(a.attname ORDER BY a.attnum) as column_names,
                ix.indisunique as is_unique,
                ix.indisprimary as is_primary
            FROM pg_class t
            JOIN pg_index ix ON t.oid = ix.indrelid
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = @schema AND t.relname = @table
            GROUP BY i.relname, ix.indisunique, ix.indisprimary";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var indexes = new List<IndexInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var columnArray = (string[])reader.GetValue(1);
            indexes.Add(new IndexInfo
            {
                Name = reader.GetString(0),
                Columns = columnArray.ToList(),
                IsUnique = reader.GetBoolean(2),
                IsPrimary = reader.GetBoolean(3)
            });
        }

        return indexes;
    }

    private static async Task<IReadOnlyList<ForeignKeyInfo>> GetForeignKeysAsync(NpgsqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                con.conname as constraint_name,
                array_agg(att.attname ORDER BY u.ord) as columns,
                nsp_ref.nspname as referenced_schema,
                cls_ref.relname as referenced_table,
                array_agg(att_ref.attname ORDER BY u.ord) as referenced_columns,
                con.confupdtype,
                con.confdeltype
            FROM pg_constraint con
            JOIN pg_class cls ON con.conrelid = cls.oid
            JOIN pg_namespace nsp ON cls.relnamespace = nsp.oid
            JOIN pg_class cls_ref ON con.confrelid = cls_ref.oid
            JOIN pg_namespace nsp_ref ON cls_ref.relnamespace = nsp_ref.oid
            JOIN LATERAL unnest(con.conkey) WITH ORDINALITY AS u(attnum, ord) ON true
            JOIN pg_attribute att ON att.attrelid = con.conrelid AND att.attnum = u.attnum
            JOIN LATERAL unnest(con.confkey) WITH ORDINALITY AS u_ref(attnum, ord) ON u.ord = u_ref.ord
            JOIN pg_attribute att_ref ON att_ref.attrelid = con.confrelid AND att_ref.attnum = u_ref.attnum
            WHERE nsp.nspname = @schema AND cls.relname = @table AND con.contype = 'f'
            GROUP BY con.conname, nsp_ref.nspname, cls_ref.relname, con.confupdtype, con.confdeltype";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var foreignKeys = new List<ForeignKeyInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var columns = (string[])reader.GetValue(1);
            var refColumns = (string[])reader.GetValue(4);

            foreignKeys.Add(new ForeignKeyInfo
            {
                Name = reader.GetString(0),
                Columns = columns.ToList(),
                ReferencedSchema = reader.GetString(2),
                ReferencedTable = reader.GetString(3),
                ReferencedColumns = refColumns.ToList(),
                OnUpdate = ConvertActionCode(reader.GetString(5)),
                OnDelete = ConvertActionCode(reader.GetString(6))
            });
        }

        return foreignKeys;
    }

    private static async Task<IReadOnlyList<ConstraintInfo>> GetConstraintsAsync(NpgsqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                con.conname as name,
                CASE con.contype
                    WHEN 'c' THEN 'CHECK'
                    WHEN 'u' THEN 'UNIQUE'
                    WHEN 'p' THEN 'PRIMARY KEY'
                    WHEN 'f' THEN 'FOREIGN KEY'
                    ELSE con.contype::text
                END as type,
                pg_get_constraintdef(con.oid) as definition
            FROM pg_constraint con
            JOIN pg_class cls ON con.conrelid = cls.oid
            JOIN pg_namespace nsp ON cls.relnamespace = nsp.oid
            WHERE nsp.nspname = @schema AND cls.relname = @table";

        await using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("schema", schema);
        cmd.Parameters.AddWithValue("table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var constraints = new List<ConstraintInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            constraints.Add(new ConstraintInfo
            {
                Name = reader.GetString(0),
                Type = reader.GetString(1),
                Definition = reader.GetString(2)
            });
        }

        return constraints;
    }

    private static string ConvertActionCode(string code)
    {
        return code switch
        {
            "a" => "NO ACTION",
            "r" => "RESTRICT",
            "c" => "CASCADE",
            "n" => "SET NULL",
            "d" => "SET DEFAULT",
            _ => "UNKNOWN"
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _connection?.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _disposed = true;
    }
}
