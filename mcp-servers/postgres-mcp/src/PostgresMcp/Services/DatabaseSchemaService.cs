using Microsoft.Extensions.Options;
using Npgsql;
using System.Data.Common;
using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for scanning PostgreSQL database schemas.
/// </summary>
public class DatabaseSchemaService(
    ILogger<DatabaseSchemaService> logger,
    IDbConnectionFactory connectionFactory,
    IOptions<PostgresOptions> postgresOptions,
    IOptions<SecurityOptions> securityOptions)
    : IDatabaseSchemaService
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    private readonly PostgresOptions _postgresOptions = postgresOptions.Value;
    private readonly SecurityOptions _securityOptions = securityOptions.Value;

    /// <inheritdoc/>
    public async Task<DatabaseSchema> ScanDatabaseSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Scanning database schema");

        await using var connection = _connectionFactory.CreateConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var schema = new DatabaseSchema
        {
            ServerVersion = connection.ServerVersion,
            DatabaseName = connection.Database
        };

        // Get all tables
        schema.Tables = await GetTablesAsync(connection, cancellationToken);

        // Get relationships
        schema.Relationships = BuildRelationships(schema.Tables);

        logger.LogInformation("Schema scan complete: {TableCount} tables found", schema.TableCount);

        return schema;
    }

    private async Task<List<TableInfo>> GetTablesAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var tables = new List<TableInfo>();

        const string tablesSql = """
            SELECT
                t.table_schema,
                t.table_name,
                obj_description((t.table_schema||'.'||t.table_name)::regclass, 'pg_class') as table_comment
            FROM information_schema.tables t
            WHERE t.table_type = 'BASE TABLE'
              AND t.table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY t.table_schema, t.table_name
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = tablesSql;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);
            var comment = reader.IsDBNull(2) ? null : reader.GetString(2);

            // Apply schema filters
            if (!IsSchemaAllowed(schemaName))
                continue;

            var table = new TableInfo
            {
                SchemaName = schemaName,
                TableName = tableName,
                Comment = comment
            };

            tables.Add(table);
        }

        // Get details for each table
        foreach (var table in tables)
        {
            table.Columns = await GetColumnsAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            table.PrimaryKey = await GetPrimaryKeyAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            table.ForeignKeys = await GetForeignKeysAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            table.Indexes = await GetIndexesAsync(connection, table.SchemaName, table.TableName, cancellationToken);
            table.RowCount = await GetRowCountAsync(connection, table.SchemaName, table.TableName, cancellationToken);
        }

        return tables;
    }

    private async Task<List<ColumnInfo>> GetColumnsAsync(
        DbConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var columns = new List<ColumnInfo>();

        const string columnsSql = """
            SELECT
                c.column_name,
                c.data_type,
                c.is_nullable,
                c.column_default,
                c.ordinal_position,
                CASE WHEN c.column_default LIKE 'nextval%' THEN true ELSE false END as is_identity,
                col_description((c.table_schema||'.'||c.table_name)::regclass, c.ordinal_position) as column_comment
            FROM information_schema.columns c
            WHERE c.table_schema = $1 AND c.table_name = $2
            ORDER BY c.ordinal_position
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = columnsSql;

        var p1 = cmd.CreateParameter();
        p1.ParameterName = "schemaName"; // Npgsql allows named parameters, but generic DbConnection acts differently. Standard SQL typically uses @ or $.
        // Npgsql supports $1 positional. Let's see if we can use AddWithValue with standard DbCommand.
        // DbCommand doesn't have AddWithValue.
        p1.Value = schemaName;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.Value = tableName;
        cmd.Parameters.Add(p2);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                IsNullable = reader.GetString(2) == "YES",
                DefaultValue = reader.IsDBNull(3) ? null : reader.GetString(3),
                OrdinalPosition = reader.GetInt32(4),
                IsIdentity = reader.GetBoolean(5),
                Comment = reader.IsDBNull(6) ? null : reader.GetString(6)
            });
        }

        return columns;
    }

    private async Task<PrimaryKeyInfo?> GetPrimaryKeyAsync(
        DbConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        const string pkSql = """
            SELECT
                tc.constraint_name,
                kcu.column_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            WHERE tc.constraint_type = 'PRIMARY KEY'
              AND tc.table_schema = $1
              AND tc.table_name = $2
            ORDER BY kcu.ordinal_position
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = pkSql;

        var p1 = cmd.CreateParameter();
        p1.Value = schemaName;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.Value = tableName;
        cmd.Parameters.Add(p2);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        PrimaryKeyInfo? pk = null;
        while (await reader.ReadAsync(cancellationToken))
        {
            if (pk == null)
            {
                pk = new PrimaryKeyInfo
                {
                    ConstraintName = reader.GetString(0),
                    Columns = []
                };
            }
            pk.Columns.Add(reader.GetString(1));
        }

        return pk;
    }

    private async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(
        DbConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var foreignKeys = new List<ForeignKeyInfo>();

        const string fkSql = """
            SELECT
                tc.constraint_name,
                kcu.column_name,
                ccu.table_schema AS foreign_table_schema,
                ccu.table_name AS foreign_table_name,
                ccu.column_name AS foreign_column_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND tc.table_schema = $1
              AND tc.table_name = $2
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = fkSql;

        var p1 = cmd.CreateParameter();
        p1.Value = schemaName;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.Value = tableName;
        cmd.Parameters.Add(p2);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                ConstraintName = reader.GetString(0),
                ColumnName = reader.GetString(1),
                ReferencedSchema = reader.GetString(2),
                ReferencedTable = reader.GetString(3),
                ReferencedColumn = reader.GetString(4)
            });
        }

        return foreignKeys;
    }

    private async Task<List<IndexInfo>> GetIndexesAsync(
        DbConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        var indexes = new List<IndexInfo>();

        const string indexSql = """
            SELECT
                i.indexname,
                i.indexdef,
                idx.indisunique,
                idx.indisprimary
            FROM pg_indexes i
            JOIN pg_class c ON c.relname = i.indexname
            JOIN pg_index idx ON idx.indexrelid = c.oid
            WHERE i.schemaname = $1 AND i.tablename = $2
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = indexSql;

        var p1 = cmd.CreateParameter();
        p1.Value = schemaName;
        cmd.Parameters.Add(p1);

        var p2 = cmd.CreateParameter();
        p2.Value = tableName;
        cmd.Parameters.Add(p2);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var indexName = reader.GetString(0);
            var indexDef = reader.GetString(1);
            var isUnique = reader.GetBoolean(2);
            var isPrimary = reader.GetBoolean(3);

            // Extract column names from index definition
            var columns = ExtractColumnsFromIndexDef(indexDef);

            indexes.Add(new IndexInfo
            {
                IndexName = indexName,
                IsUnique = isUnique,
                IsPrimary = isPrimary,
                Columns = columns
            });
        }

        return indexes;
    }

    private async Task<long?> GetRowCountAsync(
        DbConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        try
        {
            const string countSql = "SELECT reltuples::bigint FROM pg_class WHERE oid = $1::regclass";
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = countSql;

            var p1 = cmd.CreateParameter();
            p1.Value = $"{schemaName}.{tableName}";
            cmd.Parameters.Add(p1);

            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            return result as long?;
        }
        catch
        {
            return null;
        }
    }

    private List<TableRelationship> BuildRelationships(List<TableInfo> tables)
    {
        var relationships = new List<TableRelationship>();

        foreach (var table in tables)
        {
            if (table.ForeignKeys == null) continue;

            foreach (var fk in table.ForeignKeys)
            {
                relationships.Add(new TableRelationship
                {
                    SourceTable = table.FullName,
                    TargetTable = $"{fk.ReferencedSchema}.{fk.ReferencedTable}",
                    RelationType = "many-to-one",
                    ConstraintName = fk.ConstraintName
                });
            }
        }

        return relationships;
    }

    private bool IsSchemaAllowed(string schemaName)
    {
        // Check blocked schemas
        if (_securityOptions.BlockedSchemas.Contains(schemaName))
            return false;

        // Check allowed schemas (if specified)
        if (_securityOptions.AllowedSchemas.Count > 0 &&
            !_securityOptions.AllowedSchemas.Contains(schemaName))
            return false;

        return true;
    }

    private static List<string> ExtractColumnsFromIndexDef(string indexDef)
    {
        // Simple extraction of column names from index definition
        // Example: "CREATE INDEX idx_name ON schema.table USING btree (col1, col2)"
        var columns = new List<string>();

        var startIdx = indexDef.IndexOf('(');
        var endIdx = indexDef.LastIndexOf(')');

        if (startIdx >= 0 && endIdx > startIdx)
        {
            var columnsPart = indexDef.Substring(startIdx + 1, endIdx - startIdx - 1);
            columns.AddRange(columnsPart.Split(',').Select(c => c.Trim()));
        }

        return columns;
    }
}
