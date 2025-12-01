namespace PostgresMcp.Models;

/// <summary>
/// Represents a complete database schema.
/// </summary>
public class DatabaseSchema
{
    /// <summary>
    /// PostgreSQL server version.
    /// </summary>
    public string ServerVersion { get; set; } = string.Empty;

    /// <summary>
    /// Database name.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// List of tables in the database.
    /// </summary>
    public List<TableInfo> Tables { get; set; } = [];

    /// <summary>
    /// Total number of tables.
    /// </summary>
    public int TableCount => Tables.Count;

    /// <summary>
    /// List of relationships between tables.
    /// </summary>
    public List<TableRelationship>? Relationships { get; set; }
}

/// <summary>
/// Represents information about a database table.
/// </summary>
public class TableInfo
{
    /// <summary>
    /// Schema name (e.g., "public").
    /// </summary>
    public string SchemaName { get; set; } = string.Empty;

    /// <summary>
    /// Table name.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Full table name including schema (schema.table).
    /// </summary>
    public string FullName => $"{SchemaName}.{TableName}";

    /// <summary>
    /// Table comment/description.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Estimated row count.
    /// </summary>
    public long? RowCount { get; set; }

    /// <summary>
    /// List of columns in the table.
    /// </summary>
    public List<ColumnInfo> Columns { get; set; } = [];

    /// <summary>
    /// Primary key information.
    /// </summary>
    public PrimaryKeyInfo? PrimaryKey { get; set; }

    /// <summary>
    /// List of foreign keys.
    /// </summary>
    public List<ForeignKeyInfo>? ForeignKeys { get; set; }

    /// <summary>
    /// List of indexes.
    /// </summary>
    public List<IndexInfo>? Indexes { get; set; }
}

/// <summary>
/// Represents a table column.
/// </summary>
public class ColumnInfo
{
    /// <summary>
    /// Column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Data type (e.g., "integer", "varchar(255)").
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the column allows NULL values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Default value expression.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Whether this is an identity/auto-increment column.
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Column comment/description.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Ordinal position in the table.
    /// </summary>
    public int OrdinalPosition { get; set; }
}

/// <summary>
/// Represents a primary key.
/// </summary>
public class PrimaryKeyInfo
{
    /// <summary>
    /// Primary key constraint name.
    /// </summary>
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>
    /// List of columns in the primary key.
    /// </summary>
    public List<string> Columns { get; set; } = [];
}

/// <summary>
/// Represents a foreign key relationship.
/// </summary>
public class ForeignKeyInfo
{
    /// <summary>
    /// Foreign key constraint name.
    /// </summary>
    public string ConstraintName { get; set; } = string.Empty;

    /// <summary>
    /// Column name in the current table.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Referenced schema name.
    /// </summary>
    public string ReferencedSchema { get; set; } = string.Empty;

    /// <summary>
    /// Referenced table name.
    /// </summary>
    public string ReferencedTable { get; set; } = string.Empty;

    /// <summary>
    /// Referenced column name.
    /// </summary>
    public string ReferencedColumn { get; set; } = string.Empty;
}

/// <summary>
/// Represents a table index.
/// </summary>
public class IndexInfo
{
    /// <summary>
    /// Index name.
    /// </summary>
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a unique index.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Whether this is a primary key index.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// List of columns in the index.
    /// </summary>
    public List<string> Columns { get; set; } = [];
}

/// <summary>
/// Represents a relationship between two tables.
/// </summary>
public class TableRelationship
{
    /// <summary>
    /// Source table (schema.table).
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// Target table (schema.table).
    /// </summary>
    public string TargetTable { get; set; } = string.Empty;

    /// <summary>
    /// Relationship type (e.g., "one-to-many", "many-to-one").
    /// </summary>
    public string RelationType { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key constraint name.
    /// </summary>
    public string ConstraintName { get; set; } = string.Empty;
}

/// <summary>
/// Represents the result of a database query.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// List of column names.
    /// </summary>
    public List<string> Columns { get; set; } = [];

    /// <summary>
    /// Query result rows (each row is a dictionary of column name to value).
    /// </summary>
    public List<Dictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// Number of rows returned.
    /// </summary>
    public int RowCount => Rows.Count;

    /// <summary>
    /// Query execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Whether the results were truncated due to max rows limit.
    /// </summary>
    public bool IsTruncated { get; set; }
}
