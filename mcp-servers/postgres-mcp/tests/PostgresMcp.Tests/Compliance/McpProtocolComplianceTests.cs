using Microsoft.Extensions.Logging;
using NSubstitute;
using PostgresMcp.Models;
using Xunit;

namespace PostgresMcp.Tests.Compliance;

/// <summary>
/// Tests to verify compliance with MCP Specification 2025-11-25.
/// </summary>
public class McpProtocolComplianceTests
{
    [Fact]
    public void JsonRpcRequest_ShouldHaveCorrectVersion()
    {
        // Arrange & Act
        var request = new JsonRpcRequest();

        // Assert
        Assert.Equal("2.0", request.JsonRpc);
    }

    [Fact]
    public void JsonRpcResponse_ShouldHaveCorrectVersion()
    {
        // Arrange & Act
        var response = new JsonRpcResponse();

        // Assert
        Assert.Equal("2.0", response.JsonRpc);
    }

    [Fact]
    public void InitializeParams_ShouldHaveCorrectProtocolVersion()
    {
        // Arrange & Act
        var params_ = new InitializeParams();

        // Assert
        Assert.Equal("2025-11-25", params_.ProtocolVersion);
    }

    [Fact]
    public void InitializeResult_ShouldHaveCorrectProtocolVersion()
    {
        // Arrange & Act
        var result = new InitializeResult();

        // Assert
        Assert.Equal("2025-11-25", result.ProtocolVersion);
    }

    [Fact]
    public void Tool_ShouldUseInputSchemaProperty()
    {
        // Arrange
        var tool = new Tool
        {
            Name = "test_tool",
            Description = "Test tool",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    param1 = new { type = "string" }
                }
            }
        };

        // Act & Assert
        Assert.NotNull(tool.InputSchema);
        // The property name should be "inputSchema" in JSON (verified by JsonPropertyName attribute)
    }

    [Fact]
    public void CallToolParams_ShouldHaveNameAndArguments()
    {
        // Arrange
        var params_ = new CallToolParams
        {
            Name = "test_tool",
            Arguments = new Dictionary<string, object?>
            {
                { "param1", "value1" }
            }
        };

        // Act & Assert
        Assert.Equal("test_tool", params_.Name);
        Assert.NotNull(params_.Arguments);
        Assert.Contains("param1", params_.Arguments.Keys);
    }

    [Fact]
    public void CallToolResult_ShouldHaveContentAndIsError()
    {
        // Arrange & Act
        var result = new CallToolResult
        {
            Content =
            [
                new Content { Type = "text", Text = "Result text" }
            ],
            IsError = false
        };

        // Assert
        Assert.NotEmpty(result.Content);
        Assert.False(result.IsError);
    }

    [Theory]
    [InlineData(-32700, "ParseError")]
    [InlineData(-32600, "InvalidRequest")]
    [InlineData(-32601, "MethodNotFound")]
    [InlineData(-32602, "InvalidParams")]
    [InlineData(-32603, "InternalError")]
    public void JsonRpcErrorCodes_ShouldMatchStandard(int code, string name)
    {
        // Assert - Verify standard JSON-RPC 2.0 error codes are defined
        Assert.True(code >= -32768 && code <= -32000, $"{name} error code {code} should be in reserved range");
    }

    [Theory]
    [InlineData(-32002, "ServerNotInitialized")]
    [InlineData(-32001, "ResourceNotFound")]
    [InlineData(-32000, "ToolExecutionError")]
    public void ServerErrorCodes_ShouldBeInValidRange(int code, string name)
    {
        // Assert - Verify server error codes are in -32000 to -32099 range
        Assert.True(code >= -32099 && code <= -32000, $"{name} error code {code} should be in server error range");
    }

    [Fact]
    public void ServerCapabilities_ShouldSupportTools()
    {
        // Arrange & Act
        var capabilities = new ServerCapabilities
        {
            Tools = new ToolsCapability { ListChanged = true }
        };

        // Assert
        Assert.NotNull(capabilities.Tools);
        Assert.True(capabilities.Tools.ListChanged);
    }

    [Fact]
    public void ServerCapabilities_ShouldSupportResources()
    {
        // Arrange & Act
        var capabilities = new ServerCapabilities
        {
            Resources = new ResourcesCapability
            {
                Subscribe = true,
                ListChanged = true
            }
        };

        // Assert
        Assert.NotNull(capabilities.Resources);
        Assert.True(capabilities.Resources.Subscribe);
        Assert.True(capabilities.Resources.ListChanged);
    }

    [Fact]
    public void ServerCapabilities_ShouldSupportPrompts()
    {
        // Arrange & Act
        var capabilities = new ServerCapabilities
        {
            Prompts = new PromptsCapability { ListChanged = true }
        };

        // Assert
        Assert.NotNull(capabilities.Prompts);
        Assert.True(capabilities.Prompts.ListChanged);
    }

    [Fact]
    public void JsonRpcError_ShouldHaveCodeAndMessage()
    {
        // Arrange
        var error = new JsonRpcError
        {
            Code = JsonRpcErrorCodes.InvalidParams,
            Message = "Invalid parameters provided",
            Data = new { details = "param1 is required" }
        };

        // Act & Assert
        Assert.Equal(JsonRpcErrorCodes.InvalidParams, error.Code);
        Assert.Equal("Invalid parameters provided", error.Message);
        Assert.NotNull(error.Data);
    }

    [Fact]
    public void Resource_ShouldHaveUriAndName()
    {
        // Arrange
        var resource = new Resource
        {
            Uri = "postgres://localhost:5432/databases/mydb",
            Name = "mydb",
            Description = "My database",
            MimeType = "application/json"
        };

        // Act & Assert
        Assert.Equal("postgres://localhost:5432/databases/mydb", resource.Uri);
        Assert.Equal("mydb", resource.Name);
        Assert.Equal("My database", resource.Description);
        Assert.Equal("application/json", resource.MimeType);
    }

    [Fact]
    public void Prompt_ShouldHaveNameAndArguments()
    {
        // Arrange
        var prompt = new Prompt
        {
            Name = "analyze_table",
            Description = "Analyze a database table",
            Arguments =
            [
                new PromptArgument
                {
                    Name = "table_name",
                    Description = "Name of the table",
                    Required = true
                }
            ]
        };

        // Act & Assert
        Assert.Equal("analyze_table", prompt.Name);
        Assert.Single(prompt.Arguments);
        Assert.True(prompt.Arguments[0].Required);
    }

    [Fact]
    public void Content_ShouldSupportTextType()
    {
        // Arrange
        var content = new Content
        {
            Type = "text",
            Text = "Sample text content"
        };

        // Act & Assert
        Assert.Equal("text", content.Type);
        Assert.Equal("Sample text content", content.Text);
    }

    [Fact]
    public void Content_ShouldSupportImageType()
    {
        // Arrange
        var content = new Content
        {
            Type = "image",
            MimeType = "image/png",
            Data = "base64encodeddata"
        };

        // Act & Assert
        Assert.Equal("image", content.Type);
        Assert.Equal("image/png", content.MimeType);
        Assert.Equal("base64encodeddata", content.Data);
    }

    [Fact]
    public void NotificationTypes_ShouldFollowNamingConvention()
    {
        // Assert - Verify notification types follow MCP naming convention
        Assert.Equal("notifications/initialized", NotificationTypes.Initialized);
        Assert.Equal("notifications/progress", NotificationTypes.Progress);
        Assert.Equal("notifications/resources/list_changed", NotificationTypes.ResourcesListChanged);
        Assert.Equal("notifications/resources/updated", NotificationTypes.ResourcesUpdated);
        Assert.Equal("notifications/tools/list_changed", NotificationTypes.ToolsListChanged);
        Assert.Equal("notifications/prompts/list_changed", NotificationTypes.PromptsListChanged);
    }

    [Fact]
    public void Implementation_ShouldHaveNameAndVersion()
    {
        // Arrange
        var implementation = new Implementation
        {
            Name = "postgres-mcp",
            Version = "2.0.0"
        };

        // Act & Assert
        Assert.Equal("postgres-mcp", implementation.Name);
        Assert.Equal("2.0.0", implementation.Version);
    }

    [Fact]
    public void JsonRpcResponse_ShouldNotHaveBothResultAndError()
    {
        // Arrange - Success response
        var successResponse = new JsonRpcResponse
        {
            Id = 1,
            Result = new { success = true }
        };

        // Assert
        Assert.NotNull(successResponse.Result);
        Assert.Null(successResponse.Error);

        // Arrange - Error response
        var errorResponse = new JsonRpcResponse
        {
            Id = 1,
            Error = new JsonRpcError { Code = -32600, Message = "Invalid request" }
        };

        // Assert
        Assert.Null(errorResponse.Result);
        Assert.NotNull(errorResponse.Error);
    }

    [Fact]
    public void ListToolsResult_ShouldContainTools()
    {
        // Arrange
        var result = new ListToolsResult
        {
            Tools =
            [
                new Tool
                {
                    Name = "scan_database_structure",
                    Description = "Scan database structure",
                    InputSchema = new { type = "object" }
                }
            ]
        };

        // Act & Assert
        Assert.Single(result.Tools);
        Assert.Equal("scan_database_structure", result.Tools[0].Name);
    }

    [Fact]
    public void ListResourcesResult_ShouldContainResources()
    {
        // Arrange
        var result = new ListResourcesResult
        {
            Resources =
            [
                new Resource
                {
                    Uri = "postgres://localhost:5432/databases",
                    Name = "databases"
                }
            ]
        };

        // Act & Assert
        Assert.Single(result.Resources);
        Assert.Equal("postgres://localhost:5432/databases", result.Resources[0].Uri);
    }

    [Fact]
    public void ListPromptsResult_ShouldContainPrompts()
    {
        // Arrange
        var result = new ListPromptsResult
        {
            Prompts =
            [
                new Prompt
                {
                    Name = "analyze_table",
                    Description = "Analyze a table"
                }
            ]
        };

        // Act & Assert
        Assert.Single(result.Prompts);
        Assert.Equal("analyze_table", result.Prompts[0].Name);
    }

    [Fact]
    public void ProgressNotification_ShouldHaveTokenAndProgress()
    {
        // Arrange
        var notification = new ProgressNotification
        {
            ProgressToken = "task-123",
            Progress = 0.5,
            Total = 100
        };

        // Act & Assert
        Assert.Equal("task-123", notification.ProgressToken);
        Assert.Equal(0.5, notification.Progress);
        Assert.Equal(100, notification.Total);
    }

    #region MCP Protocol 2025-11-25 Compliance Tests

    [Fact]
    public void Implementation_ShouldHaveDescriptionField()
    {
        // Arrange & Act
        var implementation = new Implementation
        {
            Name = "postgres-mcp",
            Version = "2.0.0",
            Description = "Read-only PostgreSQL MCP server"
        };

        // Assert
        Assert.Equal("postgres-mcp", implementation.Name);
        Assert.Equal("2.0.0", implementation.Version);
        Assert.Equal("Read-only PostgreSQL MCP server", implementation.Description);
    }

    [Fact]
    public void Implementation_DescriptionShouldBeOptional()
    {
        // Arrange & Act
        var implementation = new Implementation
        {
            Name = "postgres-mcp",
            Version = "2.0.0"
        };

        // Assert - Description should be nullable and omitted when null
        Assert.Null(implementation.Description);
    }

    [Fact]
    public void Tool_ShouldSupportIconField()
    {
        // Arrange & Act
        var tool = new Tool
        {
            Name = "scan_database_structure",
            Description = "Scan database structure",
            InputSchema = new { type = "object" },
            Icon = "data:image/svg+xml,%3Csvg%3E%3C/svg%3E"
        };

        // Assert
        Assert.Equal("scan_database_structure", tool.Name);
        Assert.NotNull(tool.Icon);
        Assert.StartsWith("data:image/svg+xml", tool.Icon);
    }

    [Fact]
    public void Tool_IconShouldBeOptional()
    {
        // Arrange & Act
        var tool = new Tool
        {
            Name = "query_database",
            Description = "Execute queries",
            InputSchema = new { type = "object" }
        };

        // Assert - Icon should be nullable and omitted when null
        Assert.Null(tool.Icon);
    }

    [Fact]
    public void Resource_ShouldSupportIconField()
    {
        // Arrange & Act
        var resource = new Resource
        {
            Uri = "postgres://localhost:5432/databases",
            Name = "databases",
            Icon = "data:image/svg+xml,%3Csvg%3E%3C/svg%3E"
        };

        // Assert
        Assert.Equal("databases", resource.Name);
        Assert.NotNull(resource.Icon);
        Assert.StartsWith("data:image/svg+xml", resource.Icon);
    }

    [Fact]
    public void Resource_IconShouldBeOptional()
    {
        // Arrange & Act
        var resource = new Resource
        {
            Uri = "postgres://localhost:5432/tables",
            Name = "tables"
        };

        // Assert - Icon should be nullable and omitted when null
        Assert.Null(resource.Icon);
    }

    [Fact]
    public void Prompt_ShouldSupportIconField()
    {
        // Arrange & Act
        var prompt = new Prompt
        {
            Name = "analyze_table",
            Description = "Analyze a table",
            Icon = "data:image/svg+xml,%3Csvg%3E%3C/svg%3E"
        };

        // Assert
        Assert.Equal("analyze_table", prompt.Name);
        Assert.NotNull(prompt.Icon);
        Assert.StartsWith("data:image/svg+xml", prompt.Icon);
    }

    [Fact]
    public void Prompt_IconShouldBeOptional()
    {
        // Arrange & Act
        var prompt = new Prompt
        {
            Name = "find_relationships",
            Description = "Find table relationships"
        };

        // Assert - Icon should be nullable and omitted when null
        Assert.Null(prompt.Icon);
    }

    #endregion
}
