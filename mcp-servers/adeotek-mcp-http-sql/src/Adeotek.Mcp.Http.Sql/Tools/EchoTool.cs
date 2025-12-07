using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Adeotek.Mcp.Http.Sql.Tools;

[McpServerToolType]
public class EchoTool
{
    [McpServerTool, Description("Echoes the input back to the client.")]
    public static string Echo(string message)
    {
        return "hello " + message;
    }
}
