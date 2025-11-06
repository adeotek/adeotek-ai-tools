using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PostgresMcp.Controllers;
using PostgresMcp.Models;
using PostgresMcp.Services;

namespace PostgresMcp.Tests.Controllers;

public class McpControllerTests
{
    private readonly Mock<ILogger<McpController>> _loggerMock;
    private readonly Mock<IDatabaseSchemaService> _schemaServiceMock;
    private readonly Mock<IQueryService> _queryServiceMock;
    private readonly Mock<ISqlGenerationService> _sqlGenerationServiceMock;
    private readonly McpController _controller;

    public McpControllerTests()
    {
        _loggerMock = new Mock<ILogger<McpController>>();
        _schemaServiceMock = new Mock<IDatabaseSchemaService>();
        _queryServiceMock = new Mock<IQueryService>();
        _sqlGenerationServiceMock = new Mock<ISqlGenerationService>();

        _controller = new McpController(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _queryServiceMock.Object,
            _sqlGenerationServiceMock.Object);
    }

    [Fact]
    public void ListTools_ReturnsThreeTools()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeOfType<McpListToolsResponse>();

        var response = okResult.Value as McpListToolsResponse;
        response!.Tools.Should().HaveCount(3);
        response.Tools.Should().Contain(t => t.Name == "scan_database_structure");
        response.Tools.Should().Contain(t => t.Name == "query_database_data");
        response.Tools.Should().Contain(t => t.Name == "advanced_sql_query");
    }

    [Fact]
    public void ListTools_ToolsShouldHaveDescriptions()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as McpListToolsResponse;

        foreach (var tool in response!.Tools)
        {
            tool.Name.Should().NotBeNullOrEmpty();
            tool.Description.Should().NotBeNullOrEmpty();
            tool.InputSchema.Should().NotBeNull();
        }
    }

    [Fact]
    public void ListTools_ScanDatabaseStructure_ShouldHaveCorrectSchema()
    {
        // Act
        var result = _controller.ListTools();

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as McpListToolsResponse;
        var tool = response!.Tools.First(t => t.Name == "scan_database_structure");

        tool.Name.Should().Be("scan_database_structure");
        tool.Description.Should().Contain("schema");
        tool.InputSchema.Should().NotBeNull();
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
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Health_ReturnsHealthyStatus()
    {
        // Act
        var result = _controller.Health();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
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
        result.Should().Match<IActionResult>(r => r is StatusCodeResult or ObjectResult);
    }
}
