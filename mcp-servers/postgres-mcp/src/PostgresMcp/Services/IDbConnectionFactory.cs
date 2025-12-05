using System.Data.Common;
using Npgsql;

namespace PostgresMcp.Services;

/// <summary>
/// Factory for creating database connections.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new database connection.
    /// </summary>
    /// <param name="connectionString">The connection string to use.</param>
    /// <returns>A new DbConnection.</returns>
    DbConnection CreateConnection(string connectionString);
}
