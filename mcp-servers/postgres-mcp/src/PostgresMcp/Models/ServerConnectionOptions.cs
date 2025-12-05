using System.Text.Json.Serialization;

namespace PostgresMcp.Models;

/// <summary>
/// Server connection options for PostgreSQL.
/// </summary>
public class ServerConnectionOptions
{
    /// <summary>
    /// PostgreSQL server host (default: localhost).
    /// </summary>
    [JsonPropertyName("host")]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// PostgreSQL server port (default: 5432).
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Database username.
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Database password.
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Use SSL for connection (default: true).
    /// </summary>
    [JsonPropertyName("useSsl")]
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Whether the configuration has been initialized.
    /// </summary>
    public bool IsConfigured { get; set; }
}
