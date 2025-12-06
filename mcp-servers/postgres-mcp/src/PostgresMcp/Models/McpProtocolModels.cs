using System.Text.Json.Serialization;

namespace PostgresMcp.Models;

#region Initialize Protocol

/// <summary>
/// Initialize request parameters.
/// </summary>
public class InitializeParams
{
    /// <summary>
    /// Protocol version supported by client.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2025-11-25";

    /// <summary>
    /// Client capabilities.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public ClientCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Client information.
    /// </summary>
    [JsonPropertyName("clientInfo")]
    public Implementation ClientInfo { get; set; } = new();
}

/// <summary>
/// Client capabilities.
/// </summary>
public class ClientCapabilities
{
    /// <summary>
    /// Experimental capabilities.
    /// </summary>
    [JsonPropertyName("experimental")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Experimental { get; set; }

    /// <summary>
    /// Sampling capability.
    /// </summary>
    [JsonPropertyName("sampling")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Sampling { get; set; }
}

/// <summary>
/// Initialize response result.
/// </summary>
public class InitializeResult
{
    /// <summary>
    /// Protocol version supported by server.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2025-11-25";

    /// <summary>
    /// Server capabilities.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public ServerCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Server information.
    /// </summary>
    [JsonPropertyName("serverInfo")]
    public Implementation ServerInfo { get; set; } = new();
}

/// <summary>
/// Server capabilities.
/// </summary>
public class ServerCapabilities
{
    /// <summary>
    /// Experimental capabilities.
    /// </summary>
    [JsonPropertyName("experimental")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Experimental { get; set; }

    /// <summary>
    /// Logging capability.
    /// </summary>
    [JsonPropertyName("logging")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Logging { get; set; }

    /// <summary>
    /// Prompts capability.
    /// </summary>
    [JsonPropertyName("prompts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PromptsCapability? Prompts { get; set; }

    /// <summary>
    /// Resources capability.
    /// </summary>
    [JsonPropertyName("resources")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ResourcesCapability? Resources { get; set; }

    /// <summary>
    /// Tools capability.
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolsCapability? Tools { get; set; }
}

/// <summary>
/// Prompts capability.
/// </summary>
public class PromptsCapability
{
    /// <summary>
    /// Whether prompts support list changed notifications.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// Resources capability.
/// </summary>
public class ResourcesCapability
{
    /// <summary>
    /// Whether resources support subscribe.
    /// </summary>
    [JsonPropertyName("subscribe")]
    public bool Subscribe { get; set; }

    /// <summary>
    /// Whether resources support list changed notifications.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// Tools capability.
/// </summary>
public class ToolsCapability
{
    /// <summary>
    /// Whether tools support list changed notifications.
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; }
}

/// <summary>
/// Implementation information (client or server).
/// </summary>
public class Implementation
{
    /// <summary>
    /// Implementation name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Implementation version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Implementation description.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}

#endregion

#region Tools Protocol

/// <summary>
/// Tool definition.
/// </summary>
public class Tool
{
    /// <summary>
    /// Tool name (e.g., "scan_database_structure").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tool description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Input schema (JSON Schema format).
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { };

    /// <summary>
    /// Tool icon.
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Icon { get; set; }
}

/// <summary>
/// Tools list result.
/// </summary>
public class ListToolsResult
{
    /// <summary>
    /// List of available tools.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<Tool> Tools { get; set; } = [];
}

/// <summary>
/// Call tool request parameters.
/// </summary>
public class CallToolParams
{
    /// <summary>
    /// Name of the tool to call.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tool arguments.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object?>? Arguments { get; set; }
}

/// <summary>
/// Call tool result.
/// </summary>
public class CallToolResult
{
    /// <summary>
    /// Content returned by the tool.
    /// </summary>
    [JsonPropertyName("content")]
    public List<Content> Content { get; set; } = [];

    /// <summary>
    /// Whether the tool call resulted in an error.
    /// </summary>
    [JsonPropertyName("isError")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsError { get; set; }
}

/// <summary>
/// Content item (text, image, resource).
/// </summary>
public class Content
{
    /// <summary>
    /// Content type (e.g., "text", "image", "resource").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    /// <summary>
    /// Text content (for type="text").
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// MIME type (for type="image" or type="resource").
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Data (for type="image" or type="resource").
    /// </summary>
    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Data { get; set; }
}

#endregion

#region Resources Protocol

/// <summary>
/// Resource definition.
/// </summary>
public class Resource
{
    /// <summary>
    /// Resource URI (e.g., "postgres://localhost/mydb/schema").
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// Resource name (human-readable).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Resource description.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// MIME type.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Resource icon.
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Icon { get; set; }
}

/// <summary>
/// List resources request parameters.
/// </summary>
public class ListResourcesParams
{
    /// <summary>
    /// Pagination cursor (optional).
    /// </summary>
    [JsonPropertyName("cursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cursor { get; set; }
}

/// <summary>
/// List resources result.
/// </summary>
public class ListResourcesResult
{
    /// <summary>
    /// List of resources.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<Resource> Resources { get; set; } = [];

    /// <summary>
    /// Next page cursor (if more resources available).
    /// </summary>
    [JsonPropertyName("nextCursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextCursor { get; set; }
}

/// <summary>
/// Read resource request parameters.
/// </summary>
public class ReadResourceParams
{
    /// <summary>
    /// Resource URI to read.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// Read resource result.
/// </summary>
public class ReadResourceResult
{
    /// <summary>
    /// Resource contents.
    /// </summary>
    [JsonPropertyName("contents")]
    public List<ResourceContents> Contents { get; set; } = [];
}

/// <summary>
/// Resource contents.
/// </summary>
public class ResourceContents
{
    /// <summary>
    /// Resource URI.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// MIME type.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "application/json";

    /// <summary>
    /// Text content (if text-based).
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// Blob content (if binary).
    /// </summary>
    [JsonPropertyName("blob")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Blob { get; set; }
}

/// <summary>
/// Subscribe to resource request parameters.
/// </summary>
public class SubscribeResourceParams
{
    /// <summary>
    /// Resource URI to subscribe to.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// Unsubscribe from resource request parameters.
/// </summary>
public class UnsubscribeResourceParams
{
    /// <summary>
    /// Resource URI to unsubscribe from.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

#endregion

#region Prompts Protocol

/// <summary>
/// Prompt template definition.
/// </summary>
public class Prompt
{
    /// <summary>
    /// Prompt name (e.g., "query_template").
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Prompt description.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Prompt icon.
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Icon { get; set; }

    /// <summary>
    /// Arguments the prompt accepts.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PromptArgument>? Arguments { get; set; }
}

/// <summary>
/// Prompt argument definition.
/// </summary>
public class PromptArgument
{
    /// <summary>
    /// Argument name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Argument description.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether the argument is required.
    /// </summary>
    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

/// <summary>
/// List prompts result.
/// </summary>
public class ListPromptsResult
{
    /// <summary>
    /// List of available prompts.
    /// </summary>
    [JsonPropertyName("prompts")]
    public List<Prompt> Prompts { get; set; } = [];
}

/// <summary>
/// Get prompt request parameters.
/// </summary>
public class GetPromptParams
{
    /// <summary>
    /// Prompt name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Arguments for the prompt.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Arguments { get; set; }
}

/// <summary>
/// Get prompt result.
/// </summary>
public class GetPromptResult
{
    /// <summary>
    /// Prompt description.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Prompt messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<PromptMessage> Messages { get; set; } = [];
}

/// <summary>
/// Prompt message.
/// </summary>
public class PromptMessage
{
    /// <summary>
    /// Message role (e.g., "user", "assistant").
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("content")]
    public Content Content { get; set; } = new();
}

#endregion

#region Notifications

/// <summary>
/// Notification types.
/// </summary>
public static class NotificationTypes
{
    public const string Initialized = "notifications/initialized";
    public const string Progress = "notifications/progress";
    public const string ResourcesListChanged = "notifications/resources/list_changed";
    public const string ResourcesUpdated = "notifications/resources/updated";
    public const string ToolsListChanged = "notifications/tools/list_changed";
    public const string PromptsListChanged = "notifications/prompts/list_changed";
}

/// <summary>
/// Progress notification parameters.
/// </summary>
public class ProgressNotification
{
    /// <summary>
    /// Progress token from the request.
    /// </summary>
    [JsonPropertyName("progressToken")]
    public string ProgressToken { get; set; } = string.Empty;

    /// <summary>
    /// Progress value (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("progress")]
    public double Progress { get; set; }

    /// <summary>
    /// Total work units (optional).
    /// </summary>
    [JsonPropertyName("total")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Total { get; set; }
}

/// <summary>
/// Resource updated notification parameters.
/// </summary>
public class ResourceUpdatedNotification
{
    /// <summary>
    /// URI of the updated resource.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}

#endregion
