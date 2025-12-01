using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for scanning and analyzing PostgreSQL database schemas.
/// </summary>
public interface IDatabaseSchemaService
{
    /// <summary>
    /// Scans the database schema and returns comprehensive structure information.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Complete database schema information.</returns>
    Task<DatabaseSchema> ScanDatabaseSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default);
}
