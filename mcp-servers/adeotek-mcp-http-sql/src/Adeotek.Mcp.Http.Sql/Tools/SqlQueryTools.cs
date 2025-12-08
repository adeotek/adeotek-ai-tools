using System.ComponentModel;
using Adeotek.Mcp.Http.Sql.Services;
using ModelContextProtocol.Server;

namespace Adeotek.Mcp.Http.Sql.Tools;

[McpServerToolType]
public class SqlQueryTools
{
    [McpServerTool, Description("Returns the list of databases present on the server.")]
    public static async Task<IEnumerable<string>> GetDatabasesAsync(
        ISqlQueryService sqlQueryService,
        [Description("Optional search term for filternig databases by name.")] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        // return await sqlQueryService.ExecuteQueryAsync(searchTerm, cancellationToken);
        throw new NotImplementedException();
    }

    // [McpServerTool, Description("Echoes the input back to the client.")]
    // public static string ScanDatabaseSchemaAsync(string message)
    // {
    //     return "hello " + message;
    // }
}
