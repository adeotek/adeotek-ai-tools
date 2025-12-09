using System.Text.Json.Serialization;

namespace AdeotekSqlMcp.Models;

/// <summary>
/// Database connection configuration
/// </summary>
public sealed record DatabaseConfig
{
    public required string Type { get; init; } // "mssql" or "postgres"
    public required string Host { get; init; }
    public int Port { get; init; }
    public required string Database { get; init; }
    public required string User { get; init; }
    public required string Password { get; init; }
    public bool UseSsl { get; init; }
    public int ConnectionTimeout { get; init; } = 30;
    public int CommandTimeout { get; init; } = 30;
}

/// <summary>
/// Database information
/// </summary>
public sealed record DatabaseInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("size")]
    public string? Size { get; init; }

    [JsonPropertyName("owner")]
    public string? Owner { get; init; }

    [JsonPropertyName("encoding")]
    public string? Encoding { get; init; }

    [JsonPropertyName("collation")]
    public string? Collation { get; init; }
}

/// <summary>
/// Table information
/// </summary>
public sealed record TableInfo
{
    [JsonPropertyName("schema")]
    public required string Schema { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; } // "table", "view", "materialized_view"

    [JsonPropertyName("rowCount")]
    public long? RowCount { get; init; }

    [JsonPropertyName("size")]
    public string? Size { get; init; }
}

/// <summary>
/// Column information
/// </summary>
public sealed record ColumnInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("dataType")]
    public required string DataType { get; init; }

    [JsonPropertyName("nullable")]
    public bool Nullable { get; init; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; init; }

    [JsonPropertyName("isPrimaryKey")]
    public bool IsPrimaryKey { get; init; }

    [JsonPropertyName("isForeignKey")]
    public bool IsForeignKey { get; init; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; init; }

    [JsonPropertyName("precision")]
    public int? Precision { get; init; }

    [JsonPropertyName("scale")]
    public int? Scale { get; init; }
}

/// <summary>
/// Index information
/// </summary>
public sealed record IndexInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("columns")]
    public required IReadOnlyList<string> Columns { get; init; }

    [JsonPropertyName("isUnique")]
    public bool IsUnique { get; init; }

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }
}

/// <summary>
/// Foreign key information
/// </summary>
public sealed record ForeignKeyInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("columns")]
    public required IReadOnlyList<string> Columns { get; init; }

    [JsonPropertyName("referencedSchema")]
    public required string ReferencedSchema { get; init; }

    [JsonPropertyName("referencedTable")]
    public required string ReferencedTable { get; init; }

    [JsonPropertyName("referencedColumns")]
    public required IReadOnlyList<string> ReferencedColumns { get; init; }

    [JsonPropertyName("onDelete")]
    public string? OnDelete { get; init; }

    [JsonPropertyName("onUpdate")]
    public string? OnUpdate { get; init; }
}

/// <summary>
/// Constraint information
/// </summary>
public sealed record ConstraintInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("definition")]
    public string? Definition { get; init; }
}

/// <summary>
/// Complete table schema information
/// </summary>
public sealed record TableSchema
{
    [JsonPropertyName("schema")]
    public required string Schema { get; init; }

    [JsonPropertyName("table")]
    public required string Table { get; init; }

    [JsonPropertyName("columns")]
    public required IReadOnlyList<ColumnInfo> Columns { get; init; }

    [JsonPropertyName("indexes")]
    public required IReadOnlyList<IndexInfo> Indexes { get; init; }

    [JsonPropertyName("foreignKeys")]
    public required IReadOnlyList<ForeignKeyInfo> ForeignKeys { get; init; }

    [JsonPropertyName("constraints")]
    public required IReadOnlyList<ConstraintInfo> Constraints { get; init; }
}

/// <summary>
/// Query result
/// </summary>
public sealed record QueryResult
{
    [JsonPropertyName("columns")]
    public required IReadOnlyList<string> Columns { get; init; }

    [JsonPropertyName("rows")]
    public required IReadOnlyList<Dictionary<string, object?>> Rows { get; init; }

    [JsonPropertyName("rowCount")]
    public int RowCount { get; init; }

    [JsonPropertyName("executionTimeMs")]
    public long ExecutionTimeMs { get; init; }
}
