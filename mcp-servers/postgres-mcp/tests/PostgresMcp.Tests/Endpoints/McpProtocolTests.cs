using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PostgresMcp.Endpoints;
using PostgresMcp.Models;
using PostgresMcp.Services;
using Xunit;

namespace PostgresMcp.Tests.Endpoints;

public class McpProtocolTests
{
    private readonly ILogger<Program> _logger;
    private readonly IConnectionBuilderService _connectionBuilder;
    private readonly IDatabaseSchemaService _schemaService;
    private readonly IQueryService _queryService;

    public McpProtocolTests()
    {
        _logger = Substitute.For<ILogger<Program>>();
        _connectionBuilder = Substitute.For<IConnectionBuilderService>();
        _schemaService = Substitute.For<IDatabaseSchemaService>();
        _queryService = Substitute.For<IQueryService>();
    }

    [Fact]
    public async Task HandleInitialize_ReturnsCorrectProtocolVersionAndDescription()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            Method = "initialize",
            Params = new InitializeParams { ProtocolVersion = "2024-11-05" }
        };

        // Act
        var result = await McpProtocolEndpoints.HandleInitializeAsync(request, _connectionBuilder, _logger);

        // Assert
        var initResult = Assert.IsType<InitializeResult>(result);
        Assert.Equal("2025-11-25", initResult.ProtocolVersion);
        Assert.NotNull(initResult.ServerInfo.Description);
        Assert.Contains("PostgreSQL", initResult.ServerInfo.Description);
    }

    [Fact]
    public void HandleToolsList_ToolsHaveOptionalIconProperty()
    {
        // Act
        var result = McpProtocolEndpoints.HandleToolsList();

        // Assert
        var listResult = Assert.IsType<ListToolsResult>(result);
        Assert.NotEmpty(listResult.Tools);
        foreach (var tool in listResult.Tools)
        {
            // Verify Icon property exists (it's null by default in current implementation)
            Assert.Null(tool.Icon);
        }
    }

    [Fact]
    public async Task HandleToolsCall_MissingParams_ReturnsToolError_NotException()
    {
        // Arrange
        var request = new JsonRpcRequest
        {
            Method = "tools/call",
            Params = new CallToolParams { Name = "scan_database_structure", Arguments = null }
            // Missing required 'database' argument inside Arguments dictionary, or Arguments is null
        };
        _connectionBuilder.IsConfigured.Returns(true);

        // Act
        var result = await McpProtocolEndpoints.HandleToolsCallAsync(request, _connectionBuilder, _schemaService, _queryService, _logger, CancellationToken.None);

        // Assert
        var toolResult = Assert.IsType<CallToolResult>(result);
        Assert.True(toolResult.IsError);
        Assert.NotEmpty(toolResult.Content);
        Assert.Contains("argument", toolResult.Content[0].Text?.ToLower());
    }
}
