using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Npgsql;
using PostgresMcp.Models;
using System.Text;

namespace PostgresMcp.Services;

/// <summary>
/// Service for scanning and analyzing PostgreSQL database schema.
/// </summary>
public class DatabaseSchemaService(
    ILogger<DatabaseSchemaService> logger,
    IOptions<SecurityOptions> securityOptions,
    Kernel? kernel = null)
    : IDatabaseSchemaService
{
    private readonly SecurityOptions _securityOptions = securityOptions.Value;

    /// <inheritdoc/>
    public async Task<DatabaseSchema> ScanDatabaseSchemaAsync(
        string connectionString,
        string? schemaFilter = null,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Scanning database schema with filter: {SchemaFilter}", schemaFilter ?? "none");

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
        logger.LogInformation("Getting table info for {Schema}.{Table}", schemaName, tableName);

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
        logger.LogInformation("Answering schema question: {Question}", question);

        // Get the schema
        var schema = await ScanDatabaseSchemaAsync(connectionString, null, cancellationToken);

        if (kernel == null)
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

        var response = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return response.ToString();
    }

    private async Task<List<TableInfo>> GetTablesAsync(
        NpgsqlConnection connection,
        string? schemaFilter,
        CancellationToken cancellationToken)
    {
        List<TableInfo> tables = [];
        await using var cmd = new NpgsqlCommand(DbQueries.GetTablesSql, connection);
        cmd.Parameters.AddWithValue("schemaFilter", (object?)schemaFilter ?? DBNull.Value);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var schemaName = reader.GetString(0);
            var tableName = reader.GetString(1);

            // Apply security filters
            if (IsSchemaBlocked(schemaName)) continue;

            var table = new TableInfo
            {
                SchemaName = schemaName,
                TableName = tableName,
                Columns = [],
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

    private static async Task<List<ColumnInfo>> GetColumnsAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        List<ColumnInfo> columns = [];
        await using var cmd = new NpgsqlCommand(DbQueries.GetColumnsSql, connection);
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

    private static async Task<PrimaryKeyInfo?> GetPrimaryKeyAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(DbQueries.GetPrimaryKeySql, connection);
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

    private static async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(
        NpgsqlConnection connection,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken)
    {
        List<ForeignKeyInfo> foreignKeys = [];
        await using var cmd = new NpgsqlCommand(DbQueries.GetForeignKeysSql, connection);
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
        List<IndexInfo> indexes = [];
        await using var cmd = new NpgsqlCommand(DbQueries.GetIndexesSql, connection);
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
        List<ViewInfo> views = [];
        await using var cmd = new NpgsqlCommand(DbQueries.GetViewsSql, connection);
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

    private static async Task<List<Relationship>> GetRelationshipsAsync(
        NpgsqlConnection connection,
        string? schemaFilter,
        CancellationToken cancellationToken)
    {
        List<Relationship> relationships = [];
        await using var cmd = new NpgsqlCommand(DbQueries.GetRelationshipsSql, connection);
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
        {
            return true;
        }

        return _securityOptions.AllowedSchemas.Count != 0 &&
               !_securityOptions.AllowedSchemas.Contains(schemaName);
    }

    private static string FormatSchemaAsText(DatabaseSchema schema)
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
                List<string> parts = [col.ColumnName, col.DataType];
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
