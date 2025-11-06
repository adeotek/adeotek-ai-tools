using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PostgresMcp.Controllers;
using PostgresMcp.Models;
using PostgresMcp.Services;

namespace PostgresMcp.Tests.Controllers;

public class McpControllerTests
{
    private readonly ILogger<McpController> _logger;
    private readonly IDatabaseSchemaService _schemaService;
    private readonly IQueryService _queryService;
    private readonly ISqlGenerationService _sqlGenerationService;
    private readonly McpController _controller;

    public McpControllerTests()
    {
        _logger = Substitute.For<ILogger<McpController>>();
        _schemaService = Substitute.For<IDatabaseSchemaService>();
        _queryService = Substitute.For<IQueryService>();
        _sqlGenerationService = Substitute.For<ISqlGenerationService>();

        _controller = new McpController(
            _logger,
            _schemaService,
            _queryService,
            _sqlGenerationService);
    }

    [Fact]
    public void ListTools_ReturnsThreeTools()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.IsType<McpListToolsResponse>(okResult.Value);

        var response = okResult.Value as McpListToolsResponse;
        Assert.NotNull(response);
        Assert.Equal(3, response.Tools.Count);
        Assert.Contains(response.Tools, t => t.Name == "scan_database_structure");
        Assert.Contains(response.Tools, t => t.Name == "query_database_data");
        Assert.Contains(response.Tools, t => t.Name == "advanced_sql_query");
    }

    [Fact]
    public void ListTools_ToolsShouldHaveDescriptions()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        var response = okResult.Value as McpListToolsResponse;
        Assert.NotNull(response);

        foreach (var tool in response.Tools)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            Assert.NotNull(tool.InputSchema);
        }
    }

    [Fact]
    public void ListTools_ScanDatabaseStructure_ShouldHaveCorrectSchema()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        var response = okResult.Value as McpListToolsResponse;
        Assert.NotNull(response);
        var tool = response.Tools.First(t => t.Name == "scan_database_structure");

        Assert.Equal("scan_database_structure", tool.Name);
        Assert.Contains("schema", tool.Description);
        Assert.NotNull(tool.InputSchema);
    }

    [Fact]
    public async Task CallTool_UnknownTool_ReturnsBadRequest()
    {
        // Arrange
        var request = new McpToolCallRequest
        {
            Name = "unknown_tool",
            Arguments = []
        };

        // Act
        var result = await _controller.CallTool(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public void Health_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.Health();

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task CallTool_MissingRequiredArguments_ShouldHandleGracefully()
    {
        // Arrange
        var request = new McpToolCallRequest
        {
            Name = "scan_database_structure",
            Arguments = [] // Missing connectionString
        };

        // Act
        var result = await _controller.CallTool(request, CancellationToken.None);

        // Assert
        Assert.True(result is StatusCodeResult or ObjectResult);
    }
}
