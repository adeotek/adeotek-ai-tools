using System.Diagnostics;
using Microsoft.Data.SqlClient;
using AdeotekSqlMcp.Models;
using AdeotekSqlMcp.Utilities;
using Serilog.Core;

namespace AdeotekSqlMcp.Database;

/// <summary>
/// SQL Server database implementation
/// </summary>
public sealed class SqlServerDatabase : IDatabase
{
    private readonly DatabaseConfig _config;
    private readonly Logger _logger;
    private SqlConnection? _connection;
    private bool _disposed;

    public string DatabaseType => "mssql";

    public SqlServerDatabase(DatabaseConfig config, Logger logger)
    {
        if (config.Type != "mssql")
        {
            throw new ArgumentException($"Invalid database type: {config.Type}. Expected: mssql");
        }

        _config = config;
        _logger = logger;
    }

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var conn = await GetConnectionAsync(cancellationToken);
            await using var cmd = new SqlCommand("SELECT 1", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);
            _logger.Information("SQL Server connection test successful");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SQL Server connection test failed");
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
                    name,
                    CAST(size * 8.0 / 1024 AS VARCHAR) + ' MB' as size,
                    SUSER_SNAME(owner_sid) as owner,
                    collation_name
                FROM sys.databases
                WHERE database_id > 4
                ORDER BY name";

            await using var cmd = new SqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var databases = new List<DatabaseInfo>();
            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(new DatabaseInfo
                {
                    Name = reader.GetString(0),
                    Size = reader.IsDBNull(1) ? null : reader.GetString(1),
                    Owner = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Collation = reader.IsDBNull(3) ? null : reader.GetString(3)
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
            schema ??= "dbo";

            var query = @"
                SELECT
                    s.name as [schema],
                    t.name as [name],
                    t.type_desc as [type],
                    SUM(p.rows) as row_count,
                    CAST(SUM(a.total_pages) * 8.0 / 1024 AS VARCHAR) + ' MB' as size
                FROM sys.tables t
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                LEFT JOIN sys.partitions p ON t.object_id = p.object_id
                LEFT JOIN sys.allocation_units a ON p.partition_id = a.container_id
                WHERE s.name = @schema
                GROUP BY s.name, t.name, t.type_desc
                UNION ALL
                SELECT
                    s.name as [schema],
                    v.name as [name],
                    'VIEW' as [type],
                    NULL as row_count,
                    NULL as size
                FROM sys.views v
                INNER JOIN sys.schemas s ON v.schema_id = s.schema_id
                WHERE s.name = @schema
                ORDER BY [schema], [name]";

            await using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@schema", schema);
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

            await using var cmd = new SqlCommand(query, conn);
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

            await using var cmd1 = new SqlCommand("SET SHOWPLAN_XML ON", conn);
            await cmd1.ExecuteNonQueryAsync(cancellationToken);

            await using var cmd2 = new SqlCommand(query, conn);
            await using var reader = await cmd2.ExecuteReaderAsync(cancellationToken);

            var plan = string.Empty;
            if (await reader.ReadAsync(cancellationToken))
            {
                plan = reader.GetString(0);
            }

            await using var cmd3 = new SqlCommand("SET SHOWPLAN_XML OFF", conn);
            await cmd3.ExecuteNonQueryAsync(cancellationToken);

            _logger.Information("Retrieved query plan");
            return plan;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get query plan");
            throw new QueryExecutionException("Failed to get query plan", ex);
        }
    }

    private async Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await GetConnectionAsync(_config.Database, cancellationToken);
    }

    private async Task<SqlConnection> GetConnectionAsync(string database, CancellationToken cancellationToken = default)
    {
        if (_connection?.Database == database && _connection.State == System.Data.ConnectionState.Open)
        {
            return _connection;
        }

        var encrypt = _config.UseSsl ? "True" : "False";
        var connStr = $"Server={_config.Host},{_config.Port};Database={database};User Id={_config.User};Password={_config.Password};Encrypt={encrypt};TrustServerCertificate=True;Connection Timeout={_config.ConnectionTimeout};Command Timeout={_config.CommandTimeout}";

        _connection = new SqlConnection(connStr);
        await _connection.OpenAsync(cancellationToken);

        return _connection;
    }

    private static async Task<IReadOnlyList<ColumnInfo>> GetColumnsAsync(SqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                c.name,
                t.name as data_type,
                c.is_nullable,
                dc.definition as default_value,
                c.max_length,
                c.precision,
                c.scale,
                CAST(CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS BIT) as is_primary_key,
                CAST(CASE WHEN fk.parent_column_id IS NOT NULL THEN 1 ELSE 0 END AS BIT) as is_foreign_key
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            INNER JOIN sys.tables tb ON c.object_id = tb.object_id
            INNER JOIN sys.schemas s ON tb.schema_id = s.schema_id
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                WHERE i.is_primary_key = 1
            ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
            LEFT JOIN sys.foreign_key_columns fk ON c.object_id = fk.parent_object_id AND c.column_id = fk.parent_column_id
            LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
            WHERE s.name = @schema AND tb.name = @table
            ORDER BY c.column_id";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var columns = new List<ColumnInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                Name = reader.GetString(0),
                DataType = reader.GetString(1),
                Nullable = reader.GetBoolean(2),
                DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                MaxLength = reader.GetInt16(4) == -1 ? null : (int?)reader.GetInt16(4),
                Precision = reader.GetByte(5) == 0 ? null : (int?)reader.GetByte(5),
                Scale = reader.GetByte(6) == 0 ? null : (int?)reader.GetByte(6),
                IsPrimaryKey = reader.GetBoolean(7),
                IsForeignKey = reader.GetBoolean(8)
            });
        }

        return columns;
    }

    private static async Task<IReadOnlyList<IndexInfo>> GetIndexesAsync(SqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                i.name,
                STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY ic.key_ordinal) as columns,
                i.is_unique,
                i.is_primary_key
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            INNER JOIN sys.tables t ON i.object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = @schema AND t.name = @table
            GROUP BY i.name, i.is_unique, i.is_primary_key";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var indexes = new List<IndexInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var columns = reader.GetString(1).Split(',');
            indexes.Add(new IndexInfo
            {
                Name = reader.GetString(0),
                Columns = columns.ToList(),
                IsUnique = reader.GetBoolean(2),
                IsPrimary = reader.GetBoolean(3)
            });
        }

        return indexes;
    }

    private static async Task<IReadOnlyList<ForeignKeyInfo>> GetForeignKeysAsync(SqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                fk.name,
                STRING_AGG(c.name, ',') WITHIN GROUP (ORDER BY fkc.constraint_column_id) as columns,
                OBJECT_SCHEMA_NAME(fk.referenced_object_id) as referenced_schema,
                OBJECT_NAME(fk.referenced_object_id) as referenced_table,
                STRING_AGG(rc.name, ',') WITHIN GROUP (ORDER BY fkc.constraint_column_id) as referenced_columns,
                fk.update_referential_action_desc,
                fk.delete_referential_action_desc
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
            INNER JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
            INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = @schema AND t.name = @table
            GROUP BY fk.name, fk.referenced_object_id, fk.update_referential_action_desc, fk.delete_referential_action_desc";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var foreignKeys = new List<ForeignKeyInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var columns = reader.GetString(1).Split(',');
            var refColumns = reader.GetString(4).Split(',');

            foreignKeys.Add(new ForeignKeyInfo
            {
                Name = reader.GetString(0),
                Columns = columns.ToList(),
                ReferencedSchema = reader.GetString(2),
                ReferencedTable = reader.GetString(3),
                ReferencedColumns = refColumns.ToList(),
                OnUpdate = reader.GetString(5),
                OnDelete = reader.GetString(6)
            });
        }

        return foreignKeys;
    }

    private static async Task<IReadOnlyList<ConstraintInfo>> GetConstraintsAsync(SqlConnection conn, string schema, string table, CancellationToken cancellationToken)
    {
        var query = @"
            SELECT
                con.name,
                con.type_desc,
                CASE con.type
                    WHEN 'C' THEN (SELECT definition FROM sys.check_constraints WHERE object_id = con.object_id)
                    WHEN 'D' THEN (SELECT definition FROM sys.default_constraints WHERE object_id = con.object_id)
                    ELSE NULL
                END as definition
            FROM sys.objects con
            INNER JOIN sys.tables t ON con.parent_object_id = t.object_id
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE s.name = @schema AND t.name = @table
            AND con.type IN ('C', 'D', 'PK', 'UQ', 'F')";

        await using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", table);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var constraints = new List<ConstraintInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            constraints.Add(new ConstraintInfo
            {
                Name = reader.GetString(0),
                Type = reader.GetString(1),
                Definition = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        return constraints;
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
