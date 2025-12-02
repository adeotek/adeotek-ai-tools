using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PostgresNaturalLanguageMcp.Controllers;
using PostgresNaturalLanguageMcp.Models;
using PostgresNaturalLanguageMcp.Services;

namespace PostgresNaturalLanguageMcp.Tests.Controllers;

public class McpControllerTests
{
    private readonly ILogger<McpController> _logger = Substitute.For<ILogger<McpController>>();
    private readonly IConnectionBuilderService _connectionBuilder = Substitute.For<IConnectionBuilderService>();
    private readonly IDatabaseSchemaService _schemaService = Substitute.For<IDatabaseSchemaService>();
    private readonly IQueryService _queryService = Substitute.For<IQueryService>();
    private readonly ISqlGenerationService _sqlGenerationService = Substitute.For<ISqlGenerationService>();

    private readonly McpController _controller;

    public McpControllerTests()
    {
        // Configure the connection builder to be initialized by default for most tests
        _connectionBuilder.IsConfigured.Returns(true);
        _connectionBuilder.BuildConnectionString(Arg.Any<string>())
            .Returns(callInfo => $"Host=localhost;Database={callInfo.Arg<string>()};Username=test;Password=test");

        _controller = new McpController(
            _logger,
            _connectionBuilder,
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
            Arguments = [] // Missing database
        };

        // Act
        var result = await _controller.CallTool(request, CancellationToken.None);

        // Assert
        Assert.True(result is StatusCodeResult or ObjectResult);
    }

    [Fact]
    public void Initialize_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var options = new ServerConnectionOptions
        {
            Host = "localhost",
            Port = 5432,
            Username = "postgres",
            Password = "password"
        };

        // Act
        var result = _controller.Initialize(options);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
    }

    [Fact]
    public void GetConfiguration_WhenConfigured_ReturnsConfiguration()
    {
        // Arrange
        var serverConfig = new ServerConnectionOptions
        {
            Host = "localhost",
            Port = 5432,
            Username = "postgres",
            Password = "***"
        };
        _connectionBuilder.GetServerConfiguration().Returns(serverConfig);

        // Act
        var result = _controller.GetConfiguration();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public void GetConfiguration_WhenNotConfigured_ReturnsNotConfigured()
    {
        // Arrange
        _connectionBuilder.IsConfigured.Returns(false);

        // Act
        var result = _controller.GetConfiguration();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CallTool_WhenNotInitialized_ReturnsError()
    {
        // Arrange
        _connectionBuilder.IsConfigured.Returns(false);
        var request = new McpToolCallRequest
        {
            Name = "scan_database_structure",
            Arguments = new Dictionary<string, object?> { ["database"] = "testdb" }
        };

        // Act
        var result = await _controller.CallTool(request, CancellationToken.None);

        // Assert
        var badResult = result as BadRequestObjectResult;
        Assert.NotNull(badResult);
        var response = badResult.Value as McpToolCallResponse;
        Assert.NotNull(response);
        Assert.False(response.Success);
        Assert.Contains("not initialized", response.Error ?? "");
    }
}
