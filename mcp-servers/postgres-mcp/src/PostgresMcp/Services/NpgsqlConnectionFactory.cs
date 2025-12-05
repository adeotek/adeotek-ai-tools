using System.Data.Common;
using Npgsql;

namespace PostgresMcp.Services;

/// <summary>
/// Implementation of IDbConnectionFactory using Npgsql.
/// </summary>
public class NpgsqlConnectionFactory : IDbConnectionFactory
{
    /// <inheritdoc />
    public DbConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }
}
