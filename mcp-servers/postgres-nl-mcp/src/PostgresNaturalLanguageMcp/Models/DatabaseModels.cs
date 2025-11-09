namespace PostgresNaturalLanguageMcp.Models;

/// <summary>
/// Represents a comprehensive database schema structure.
/// </summary>
public record DatabaseSchema
{
    /// <summary>
    /// List of tables in the database.
    /// </summary>
    public required List<TableInfo> Tables { get; init; }

    /// <summary>
    /// List of views in the database.
    /// </summary>
    public List<ViewInfo>? Views { get; init; }

    /// <summary>
    /// List of relationships between tables.
    /// </summary>
    public List<Relationship>? Relationships { get; init; }

    /// <summary>
    /// Total number of tables.
    /// </summary>
    public int TableCount => Tables.Count;

    /// <summary>
    /// Database server version.
    /// </summary>
    public string? ServerVersion { get; init; }
}

/// <summary>
/// Detailed information about a database table.
/// </summary>
public record TableInfo
{
    /// <summary>
    /// Schema name (e.g., "public").
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// Table name.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Full qualified name (schema.table).
    /// </summary>
    public string FullName => $"{SchemaName}.{TableName}";

    /// <summary>
    /// List of columns in the table.
    /// </summary>
    public required List<ColumnInfo> Columns { get; init; }

    /// <summary>
    /// Primary key constraint information.
    /// </summary>
    public PrimaryKeyInfo? PrimaryKey { get; set; }

    /// <summary>
    /// Foreign key constraints.
    /// </summary>
    public List<ForeignKeyInfo>? ForeignKeys { get; set; }

    /// <summary>
    /// Indexes on the table.
    /// </summary>
    public List<IndexInfo>? Indexes { get; set; }

    /// <summary>
    /// Estimated row count.
    /// </summary>
    public long? RowCount { get; init; }

    /// <summary>
    /// Table size in bytes.
    /// </summary>
    public long? SizeInBytes { get; init; }

    /// <summary>
    /// Table comment/description.
    /// </summary>
    public string? Comment { get; init; }
}

/// <summary>
/// Information about a database column.
/// </summary>
public record ColumnInfo
{
    /// <summary>
    /// Column name.
    /// </summary>
    public required string ColumnName { get; init; }

    /// <summary>
    /// Data type (e.g., "integer", "varchar", "timestamp").
    /// </summary>
    public required string DataType { get; init; }

    /// <summary>
    /// Whether the column allows NULL values.
    /// </summary>
    public required bool IsNullable { get; init; }

    /// <summary>
    /// Default value expression.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Maximum length for character types.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Numeric precision.
    /// </summary>
    public int? NumericPrecision { get; init; }

    /// <summary>
    /// Numeric scale.
    /// </summary>
    public int? NumericScale { get; init; }

    /// <summary>
    /// Ordinal position in the table.
    /// </summary>
    public required int OrdinalPosition { get; init; }

    /// <summary>
    /// Whether this is an identity/auto-increment column.
    /// </summary>
    public bool IsIdentity { get; init; }

    /// <summary>
    /// Column comment/description.
    /// </summary>
    public string? Comment { get; init; }
}

/// <summary>
/// Information about a primary key constraint.
/// </summary>
public record PrimaryKeyInfo
{
    /// <summary>
    /// Constraint name.
    /// </summary>
    public required string ConstraintName { get; init; }

    /// <summary>
    /// List of column names that form the primary key.
    /// </summary>
    public required List<string> Columns { get; init; }
}

/// <summary>
/// Information about a foreign key constraint.
/// </summary>
public record ForeignKeyInfo
{
    /// <summary>
    /// Constraint name.
    /// </summary>
    public required string ConstraintName { get; init; }

    /// <summary>
    /// Column name in this table.
    /// </summary>
    public required string ColumnName { get; init; }

    /// <summary>
    /// Referenced schema name.
    /// </summary>
    public required string ReferencedSchema { get; init; }

    /// <summary>
    /// Referenced table name.
    /// </summary>
    public required string ReferencedTable { get; init; }

    /// <summary>
    /// Referenced column name.
    /// </summary>
    public required string ReferencedColumn { get; init; }

    /// <summary>
    /// ON DELETE action (CASCADE, SET NULL, etc.).
    /// </summary>
    public string? OnDelete { get; init; }

    /// <summary>
    /// ON UPDATE action.
    /// </summary>
    public string? OnUpdate { get; init; }
}

/// <summary>
/// Information about an index.
/// </summary>
public record IndexInfo
{
    /// <summary>
    /// Index name.
    /// </summary>
    public required string IndexName { get; init; }

    /// <summary>
    /// List of column names in the index.
    /// </summary>
    public required List<string> Columns { get; init; }

    /// <summary>
    /// Whether this is a unique index.
    /// </summary>
    public required bool IsUnique { get; init; }

    /// <summary>
    /// Index type (btree, hash, gin, gist, etc.).
    /// </summary>
    public string? IndexType { get; init; }

    /// <summary>
    /// Whether this is a primary key index.
    /// </summary>
    public bool IsPrimary { get; init; }
}

/// <summary>
/// Information about a database view.
/// </summary>
public record ViewInfo
{
    /// <summary>
    /// Schema name.
    /// </summary>
    public required string SchemaName { get; init; }

    /// <summary>
    /// View name.
    /// </summary>
    public required string ViewName { get; init; }

    /// <summary>
    /// View definition (SQL).
    /// </summary>
    public string? Definition { get; init; }

    /// <summary>
    /// List of columns in the view.
    /// </summary>
    public List<ColumnInfo>? Columns { get; init; }
}

/// <summary>
/// Represents a relationship between two tables.
/// </summary>
public record Relationship
{
    /// <summary>
    /// Source table (schema.table).
    /// </summary>
    public required string SourceTable { get; init; }

    /// <summary>
    /// Target table (schema.table).
    /// </summary>
    public required string TargetTable { get; init; }

    /// <summary>
    /// Foreign key constraint name.
    /// </summary>
    public required string ConstraintName { get; init; }

    /// <summary>
    /// Relationship type (one-to-many, many-to-one, etc.).
    /// </summary>
    public string? RelationType { get; init; }
}

/// <summary>
/// Result of a database query.
/// </summary>
public record QueryResult
{
    /// <summary>
    /// Column names.
    /// </summary>
    public required List<string> Columns { get; init; }

    /// <summary>
    /// Rows of data.
    /// </summary>
    public required List<Dictionary<string, object?>> Rows { get; init; }

    /// <summary>
    /// Number of rows returned.
    /// </summary>
    public int RowCount => Rows.Count;

    /// <summary>
    /// Execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Related tables that were involved in the query.
    /// </summary>
    public List<string>? RelatedTables { get; init; }
}

/// <summary>
/// Result of an AI-generated SQL query.
/// </summary>
public record SqlGenerationResult
{
    /// <summary>
    /// The generated SQL query.
    /// </summary>
    public required string SqlQuery { get; init; }

    /// <summary>
    /// Explanation of what the query does.
    /// </summary>
    public string? Explanation { get; init; }

    /// <summary>
    /// The executed query results.
    /// </summary>
    public QueryResult? Results { get; init; }

    /// <summary>
    /// Confidence score (0-1) of the query generation.
    /// </summary>
    public double? ConfidenceScore { get; init; }

    /// <summary>
    /// Whether the query was validated as safe.
    /// </summary>
    public bool IsSafe { get; init; }

    /// <summary>
    /// Warnings or suggestions about the query.
    /// </summary>
    public List<string>? Warnings { get; init; }
}
