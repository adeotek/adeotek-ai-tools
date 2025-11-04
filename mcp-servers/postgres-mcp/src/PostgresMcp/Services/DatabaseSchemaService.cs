using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Npgsql;
using PostgresMcp.Models;
using System.Data;
using System.Text;

namespace PostgresMcp.Services;

/// <summary>
/// Service for scanning and analyzing PostgreSQL database schema.
/// </summary>
public class DatabaseSchemaService : IDatabaseSchemaService
{
    private readonly ILogger<DatabaseSchemaService> _logger;
    private readonly SecurityOptions _securityOptions;
    private readonly Kernel? _kernel;

    public DatabaseSchemaService(
        ILogger<DatabaseSchemaService> logger,
        IOptions<SecurityOptions> securityOptions,
        Kernel? kernel = null)
    {
        _logger = logger;
        _securityOptions = securityOptions.Value;
        _kernel = kernel;
    }

    /// <inheritdoc/>
    public async Task<DatabaseSchema> ScanDatabaseSchemaAsync(
        string connectionString,
        string? schemaFilter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scanning database schema with filter: {SchemaFilter}", schemaFilter ?? "none");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = await GetTablesAsync(connection, schemaFilter, cancellationToken);
        var views = await GetViewsAsync(connection, schemaFilter, cancellationToken);
        var relationships = await GetRelationshipsAsync(connection, schemaFilter, cancellationToken);
        var serverVersion = connection.ServerVersion;

        return new DatabaseSchema
        {
            Tables = tables,
            Views = views,
            Relationships = relationships,
            ServerVersion = serverVersion
        };
    }

    /// <inheritdoc/>
    public async Task<TableInfo?> GetTableInfoAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting table info for {Schema}.{Table}", schemaName, tableName);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var tables = await GetTablesAsync(connection, schemaName, cancellationToken);
        return tables.FirstOrDefault(t =>
            t.SchemaName == schemaName &&
            t.TableName == tableName);
    }

    /// <inheritdoc/>
    public async Task<string> AnswerSchemaQuestionAsync(
        string connectionString,
        string question,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Answering schema question: {Question}", question);

        // Get the schema
        var schema = await ScanDatabaseSchemaAsync(connectionString, null, cancellationToken);

        if (_kernel == null)
        {
            // Without AI, provide a structured text response
            return FormatSchemaAsText(schema);
        }

        // Use AI to answer the question based on the schema
        var schemaContext = FormatSchemaForAi(schema);
        var prompt = $"""
            You are a database expert. Answer the following question about the database schema.

            Database Schema:
            {schemaContext}

            Question: {question}

            Provide a clear, concise answer focused on the relevant parts of the schema.
            """;

        var response = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return response.ToString();
    }

    private async Task<List<TableInfo>> GetTablesAsync(
        NpgsqlConnection connection,
        string? schemaFilter,
        CancellationToken cancellationToken)
    {
        var tables = new List<TableInfo>();

        var sql = """
            SELECT
                t.table_schema,
                t.table_name,
                pg_stat.n_live_tup as row_count,
                pg_total_relation_size(quote_ident(t.table_schema) || '.' || quote_ident(t.table_name)) as size_bytes,
                obj_description((quote_ident(t.table_schema) || '.' || quote_ident(t.table_name))::regclass, 'pg_class') as table_comment
            FROM information_schema.tables t
            LEFT JOIN pg_stat_user_tables pg_stat
                ON t.table_schema = pg_stat.schemaname
                AND t.table_name = pg_stat.relname
            WHERE t.table_type = 'BASE TABLE'
                AND t.table_schema NOT IN ('pg_catalog', 'information_schema')
                AND (@schemaFilter IS NULL OR t.table_schema = @schemaFilter)
            ORDER BY t.table_schema, t.table_name
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemaFilter", (object?)schemaFilter ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);

            // Apply security filters
            if (IsSchemaBlocked(schemaName))
                continue;

            var table = new TableInfo
            {
                SchemaName = schemaName,
                TableName = tableName,
                Columns = new List<ColumnInfo>(),
                RowCount = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                SizeInBytes = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                Comment = reader.IsDBNull(4) ? null : reader.GetString(4)
            };

            tables.Add(table);
        }

        // Get detailed information for each table
        foreach (var table in tables)
        {
            table.Columns.AddRange(await GetColumnsAsync(connection, table.SchemaName, table.TableName, cancellationToken));
            table.PrimaryKey = await GetPrimaryKeyAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            table.ForeignKeys = await GetForeignKeysAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            table.Indexes = await GetIndexesAsync(connection, table.SchemaName, table.TableName, cancellationToken);
        }

        return tables;
    }

    private async Task<List<ColumnInfo>> GetColumnsAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var columns = new List<ColumnInfo>();

        var sql = """
            SELECT
                c.column_name,
                c.data_type,
                c.is_nullable,
                c.column_default,
                c.character_maximum_length,
                c.numeric_precision,
                c.numeric_scale,
                c.ordinal_position,
                c.is_identity,
                col_description((quote_ident(c.table_schema) || '.' || quote_ident(c.table_name))::regclass, c.ordinal_position) as column_comment
            FROM information_schema.columns c
            WHERE c.table_schema = @schemaName
                AND c.table_name = @tableName
            ORDER BY c.ordinal_position
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemaName", schemaName);
        cmd.Parameters.AddWithValue("tableName", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES",
                DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                MaxLength = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                NumericPrecision = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                NumericScale = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                OrdinalPosition = reader.GetInt32(7),
                IsIdentity = reader.GetString(8) == "YES",
                Comment = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return columns;
    }

    private async Task<PrimaryKeyInfo?> GetPrimaryKeyAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var sql = """
            SELECT
                tc.constraint_name,
                string_agg(kcu.column_name, ',' ORDER BY kcu.ordinal_position) as columns
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            WHERE tc.constraint_type = 'PRIMARY KEY'
                AND tc.table_schema = @schemaName
                AND tc.table_name = @tableName
            GROUP BY tc.constraint_name
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemaName", schemaName);
        cmd.Parameters.AddWithValue("tableName", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new PrimaryKeyInfo
            {
                ConstraintName = reader.GetString(0),
                Columns = reader.GetString(1).Split(',').ToList()
            };
        }

        return null;
    }

    private async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var foreignKeys = new List<ForeignKeyInfo>();

        var sql = """
            SELECT
                tc.constraint_name,
                kcu.column_name,
                ccu.table_schema AS referenced_schema,
                ccu.table_name AS referenced_table,
                ccu.column_name AS referenced_column,
                rc.delete_rule,
                rc.update_rule
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            JOIN information_schema.referential_constraints rc
                ON rc.constraint_name = tc.constraint_name
                AND rc.constraint_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
                AND tc.table_schema = @schemaName
                AND tc.table_name = @tableName
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemaName", schemaName);
        cmd.Parameters.AddWithValue("tableName", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                ConstraintName = reader.GetString(0),
                ColumnName = reader.GetString(1),
                ReferencedSchema = reader.GetString(2),
                ReferencedTable = reader.GetString(3),
                ReferencedColumn = reader.GetString(4),
                OnDelete = reader.GetString(5),
                OnUpdate = reader.GetString(6)
            });
        }

        return foreignKeys;
    }

    private async Task<List<IndexInfo>> GetIndexesAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var indexes = new List<IndexInfo>();

        var sql = """
            SELECT
                i.relname AS index_name,
                array_agg(a.attname ORDER BY x.ordinality) AS columns,
                ix.indisunique AS is_unique,
                am.amname AS index_type,
                ix.indisprimary AS is_primary
            FROM pg_index ix
            JOIN pg_class t ON t.oid = ix.indrelid
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            JOIN pg_am am ON i.relam = am.oid
            CROSS JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS x(attnum, ordinality)
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.attnum
            WHERE n.nspname = @schemaName
                AND t.relname = @tableName
            GROUP BY i.relname, ix.indisunique, am.amname, ix.indisprimary
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemaName", schemaName);
        cmd.Parameters.AddWithValue("tableName", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            indexes.Add(new IndexInfo
            {
                IndexName = reader.GetString(0),
                Columns = ((string[])reader.GetValue(1)).ToList(),
                IsUnique = reader.GetBoolean(2),
                IndexType = reader.GetString(3),
                IsPrimary = reader.GetBoolean(4)
            });
        }

        return indexes;
    }

    private async Task<List<ViewInfo>> GetViewsAsync(
        NpgsqlConnection connection,
        string? schemaFilter,
        CancellationToken cancellationToken)
    {
        var views = new List<ViewInfo>();

        var sql = """
            SELECT
                table_schema,
                table_name,
                view_definition
            FROM information_schema.views
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                AND (@schemaFilter IS NULL OR table_schema = @schemaFilter)
            ORDER BY table_schema, table_name
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemaFilter", (object?)schemaFilter ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var schemaName = reader.GetString(0);

            if (IsSchemaBlocked(schemaName))
                continue;

            views.Add(new ViewInfo
            {
                SchemaName = schemaName,
                ViewName = reader.GetString(1),
                Definition = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        return views;
    }

    private async Task<List<Relationship>> GetRelationshipsAsync(
        NpgsqlConnection connection,
        string? schemaFilter,
        CancellationToken cancellationToken)
    {
        var relationships = new List<Relationship>();

        var sql = """
            SELECT
                tc.table_schema || '.' || tc.table_name AS source_table,
                ccu.table_schema || '.' || ccu.table_name AS target_table,
                tc.constraint_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.constraint_column_usage ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
                AND tc.table_schema NOT IN ('pg_catalog', 'information_schema')
                AND (@schemaFilter IS NULL OR tc.table_schema = @schemaFilter)
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("schemaFilter", (object?)schemaFilter ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            relationships.Add(new Relationship
            {
                SourceTable = reader.GetString(0),
                TargetTable = reader.GetString(1),
                ConstraintName = reader.GetString(2),
                RelationType = "many-to-one"
            });
        }

        return relationships;
    }

    private bool IsSchemaBlocked(string schemaName)
    {
        if (_securityOptions.BlockedSchemas.Contains(schemaName))
            return true;

        if (_securityOptions.AllowedSchemas.Any() &&
            !_securityOptions.AllowedSchemas.Contains(schemaName))
            return true;

        return false;
    }

    private string FormatSchemaAsText(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Database Schema (PostgreSQL {schema.ServerVersion})");
        sb.AppendLine($"Total Tables: {schema.TableCount}");
        sb.AppendLine();

        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"Table: {table.FullName}");
            if (!string.IsNullOrEmpty(table.Comment))
                sb.AppendLine($"  Description: {table.Comment}");

            sb.AppendLine($"  Columns:");
            foreach (var col in table.Columns)
            {
                var nullable = col.IsNullable ? "NULL" : "NOT NULL";
                sb.AppendLine($"    - {col.ColumnName}: {col.DataType} {nullable}");
            }

            if (table.PrimaryKey != null)
                sb.AppendLine($"  Primary Key: {string.Join(", ", table.PrimaryKey.Columns)}");

            if (table.ForeignKeys?.Any() == true)
            {
                sb.AppendLine($"  Foreign Keys:");
                foreach (var fk in table.ForeignKeys)
                {
                    sb.AppendLine($"    - {fk.ColumnName} → {fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatSchemaForAi(DatabaseSchema schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"PostgreSQL {schema.ServerVersion} with {schema.TableCount} tables:");

        foreach (var table in schema.Tables)
        {
            sb.AppendLine($"\nTable: {table.FullName}");
            if (!string.IsNullOrEmpty(table.Comment))
                sb.AppendLine($"Description: {table.Comment}");

            sb.AppendLine("Columns:");
            foreach (var col in table.Columns)
            {
                var parts = new List<string> { col.ColumnName, col.DataType };
                if (!col.IsNullable) parts.Add("NOT NULL");
                if (col.IsIdentity) parts.Add("IDENTITY");
                if (!string.IsNullOrEmpty(col.DefaultValue)) parts.Add($"DEFAULT {col.DefaultValue}");
                sb.AppendLine($"  - {string.Join(" ", parts)}");
            }

            if (table.PrimaryKey != null)
                sb.AppendLine($"PK: {string.Join(", ", table.PrimaryKey.Columns)}");

            if (table.ForeignKeys?.Any() == true)
            {
                foreach (var fk in table.ForeignKeys)
                    sb.AppendLine($"FK: {fk.ColumnName} → {fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}");
            }
        }

        return sb.ToString();
    }
}
