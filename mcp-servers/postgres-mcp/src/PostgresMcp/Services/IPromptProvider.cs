using PostgresMcp.Models;

namespace PostgresMcp.Services;

/// <summary>
/// Service for providing MCP prompt templates.
/// </summary>
public interface IPromptProvider
{
    /// <summary>
    /// List all available prompt templates.
    /// </summary>
    /// <returns>List of prompts</returns>
    Task<ListPromptsResult> ListPromptsAsync();

    /// <summary>
    /// Get a specific prompt template with arguments substituted.
    /// </summary>
    /// <param name="name">Prompt name</param>
    /// <param name="arguments">Arguments to substitute in the template</param>
    /// <returns>Prompt with substituted arguments</returns>
    Task<GetPromptResult> GetPromptAsync(string name, Dictionary<string, string>? arguments = null);
}
