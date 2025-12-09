using System.Text.RegularExpressions;
using AdeotekSqlMcp.Utilities;

namespace AdeotekSqlMcp.Security;

/// <summary>
/// Query validation result
/// </summary>
public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Multi-layer SQL query validation for read-only operations
/// </summary>
public sealed class QueryValidator
{
    private static readonly HashSet<string> BlockedKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Data modification
        "INSERT", "UPDATE", "DELETE", "TRUNCATE", "MERGE", "UPSERT", "REPLACE", "COPY",

        // Schema modification
        "CREATE", "ALTER", "DROP", "RENAME", "COMMENT",

        // Permissions
        "GRANT", "REVOKE",

        // Transaction control
        "BEGIN", "COMMIT", "ROLLBACK", "SAVEPOINT", "START TRANSACTION",

        // Locking
        "LOCK", "UNLOCK",

        // Maintenance
        "VACUUM", "ANALYZE", "REINDEX", "CLUSTER", "CHECKPOINT",

        // Configuration
        "SET", "RESET",

        // Messaging
        "LISTEN", "NOTIFY", "UNLISTEN",

        // Procedural
        "DO", "CALL", "EXECUTE", "EXEC", "DECLARE"
    };

    private static readonly HashSet<string> DangerousFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        // PostgreSQL dangerous functions
        "pg_read_file", "pg_read_binary_file", "pg_execute", "pg_terminate_backend",
        "pg_sleep", "pg_ls_dir", "pg_stat_file",

        // SQL Server dangerous functions
        "xp_cmdshell", "sp_executesql", "OPENROWSET", "OPENDATASOURCE", "OPENQUERY",
        "xp_regread", "xp_regwrite", "xp_fileexist"
    };

    private const int MaxQueryLength = 50000;
    private const int MaxRowLimit = 10000;

    /// <summary>
    /// Validates a SQL query for read-only operations
    /// </summary>
    public ValidationResult Validate(string query)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check for empty query
        if (string.IsNullOrWhiteSpace(query))
        {
            errors.Add("Query cannot be empty");
            return new ValidationResult { IsValid = false, Errors = errors, Warnings = warnings };
        }

        // Check query length
        if (query.Length > MaxQueryLength)
        {
            errors.Add($"Query exceeds maximum length of {MaxQueryLength} characters");
        }

        // Normalize query
        var normalizedQuery = query.Trim();
        var upperQuery = normalizedQuery.ToUpperInvariant();

        // Check starting keyword (must be SELECT, WITH, or EXPLAIN)
        if (!upperQuery.StartsWith("SELECT") &&
            !upperQuery.StartsWith("WITH") &&
            !upperQuery.StartsWith("EXPLAIN"))
        {
            errors.Add("Query must start with SELECT, WITH, or EXPLAIN");
        }

        // Check for blocked keywords
        foreach (var keyword in BlockedKeywords)
        {
            if (ContainsKeyword(upperQuery, keyword))
            {
                errors.Add($"Blocked keyword detected: {keyword}");
            }
        }

        // Check for dangerous functions
        foreach (var function in DangerousFunctions)
        {
            if (ContainsFunction(upperQuery, function))
            {
                errors.Add($"Dangerous function detected: {function}");
            }
        }

        // Check for multiple statements (semicolon)
        if (ContainsMultipleStatements(normalizedQuery))
        {
            errors.Add("Multiple statements are not allowed");
        }

        // Check for procedural code blocks
        if (upperQuery.Contains("$$") || upperQuery.Contains("$BODY$"))
        {
            errors.Add("Procedural code blocks are not allowed");
        }

        // Check for INTO OUTFILE
        if (Regex.IsMatch(upperQuery, @"\bINTO\s+OUTFILE\b"))
        {
            errors.Add("INTO OUTFILE is not allowed");
        }

        // Check for LOAD_FILE
        if (Regex.IsMatch(upperQuery, @"\bLOAD_FILE\b"))
        {
            errors.Add("LOAD_FILE is not allowed");
        }

        // Check for SQL injection patterns
        if (ContainsSqlInjectionPatterns(normalizedQuery))
        {
            errors.Add("Potential SQL injection pattern detected");
        }

        // Warnings
        if (!Regex.IsMatch(upperQuery, @"\bLIMIT\b") && !Regex.IsMatch(upperQuery, @"\bTOP\b"))
        {
            warnings.Add($"Query does not have a LIMIT clause. Results will be limited to {MaxRowLimit} rows");
        }

        if (upperQuery.Contains("SELECT *"))
        {
            warnings.Add("Using SELECT * may return more data than needed. Consider specifying columns explicitly");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Validates query and throws exception if invalid
    /// </summary>
    public void ValidateOrThrow(string query)
    {
        var result = Validate(query);
        if (!result.IsValid)
        {
            var errorMessage = string.Join("; ", result.Errors);
            throw new QueryValidationException($"Query validation failed: {errorMessage}");
        }
    }

    /// <summary>
    /// Enforces row limit on query
    /// </summary>
    public string EnforceLimit(string query, int maxRows = MaxRowLimit)
    {
        maxRows = Math.Min(maxRows, MaxRowLimit);

        var upperQuery = query.ToUpperInvariant();

        // If already has LIMIT, ensure it's not too high
        if (Regex.IsMatch(upperQuery, @"\bLIMIT\s+(\d+)"))
        {
            return Regex.Replace(
                query,
                @"LIMIT\s+(\d+)",
                match =>
                {
                    var limit = int.Parse(match.Groups[1].Value);
                    return limit > maxRows ? $"LIMIT {maxRows}" : match.Value;
                },
                RegexOptions.IgnoreCase
            );
        }

        // If has TOP, ensure it's not too high (SQL Server)
        if (Regex.IsMatch(upperQuery, @"\bTOP\s+(\d+)"))
        {
            return Regex.Replace(
                query,
                @"TOP\s+(\d+)",
                match =>
                {
                    var limit = int.Parse(match.Groups[1].Value);
                    return limit > maxRows ? $"TOP {maxRows}" : match.Value;
                },
                RegexOptions.IgnoreCase
            );
        }

        // Add LIMIT for PostgreSQL-style queries
        if (!upperQuery.Contains("TOP"))
        {
            return $"{query.TrimEnd(';')} LIMIT {maxRows}";
        }

        return query;
    }

    /// <summary>
    /// Sanitizes identifier (table, column, schema names)
    /// </summary>
    public string SanitizeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier cannot be empty", nameof(identifier));
        }

        // Allow only alphanumeric, underscore, and dot
        var sanitized = Regex.Replace(identifier, @"[^a-zA-Z0-9_.]", "");

        if (sanitized != identifier)
        {
            throw new QueryValidationException($"Invalid identifier: {identifier}");
        }

        return sanitized;
    }

    private static bool ContainsKeyword(string query, string keyword)
    {
        // Use word boundary to avoid false positives (e.g., "inserted" shouldn't match "INSERT")
        var pattern = $@"\b{Regex.Escape(keyword)}\b";
        return Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase);
    }

    private static bool ContainsFunction(string query, string function)
    {
        var pattern = $@"\b{Regex.Escape(function)}\s*\(";
        return Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase);
    }

    private static bool ContainsMultipleStatements(string query)
    {
        // Remove string literals to avoid false positives
        var withoutStrings = Regex.Replace(query, @"'[^']*'", "");

        // Check for semicolons (multiple statements)
        var semicolonCount = withoutStrings.Count(c => c == ';');

        // Allow one trailing semicolon
        if (semicolonCount > 1 || (semicolonCount == 1 && !withoutStrings.TrimEnd().EndsWith(";")))
        {
            return true;
        }

        return false;
    }

    private static bool ContainsSqlInjectionPatterns(string query)
    {
        // Common SQL injection patterns
        var patterns = new[]
        {
            @"'\s*OR\s+'1'\s*=\s*'1",
            @"'\s*OR\s+1\s*=\s*1",
            @"--\s*$",
            @"/\*.*\*/",
            @"\bUNION\s+ALL\s+SELECT\b",
            @"'\s*;\s*DROP\b"
        };

        return patterns.Any(pattern => Regex.IsMatch(query, pattern, RegexOptions.IgnoreCase));
    }
}
