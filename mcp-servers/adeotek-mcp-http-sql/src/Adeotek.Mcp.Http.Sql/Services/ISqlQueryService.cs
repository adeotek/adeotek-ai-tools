using Adeotek.Mcp.Http.Sql.Models;

namespace Adeotek.Mcp.Http.Sql.Services;

public interface ISqlQueryService
{
    Task<SqlQueryResult> ExecuteQueryAsync(string sql, string? database = null, CancellationToken cancellationToken = default);
}
