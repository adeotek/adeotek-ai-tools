using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using NSubstitute;
using PostgresNaturalLanguageMcp.Endpoints;
using PostgresNaturalLanguageMcp.Models;
using PostgresNaturalLanguageMcp.Services;
using System.Net;
using System.Net.Http.Json;

namespace PostgresNaturalLanguageMcp.Tests.Endpoints;

public class McpEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IConnectionBuilderService _connectionBuilder;
    private readonly IDatabaseSchemaService _schemaService;
    private readonly IQueryService _queryService;
    private readonly ISqlGenerationService _sqlGenerationService;

    public McpEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _connectionBuilder = Substitute.For<IConnectionBuilderService>();
        _schemaService = Substitute.For<IDatabaseSchemaService>();
        _queryService = Substitute.For<IQueryService>();
        _sqlGenerationService = Substitute.For<ISqlGenerationService>();

        // Configure default behavior
        _connectionBuilder.IsConfigured.Returns(true);
        _connectionBuilder.BuildConnectionString(Arg.Any<string>())
            .Returns(callInfo => $"Host=localhost;Database={callInfo.Arg<string>()};Username=test;Password=test");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace services with mocks
                services.AddSingleton(_connectionBuilder);
                services.AddScoped<IDatabaseSchemaService>(_ => _schemaService);
                services.AddScoped<IQueryService>(_ => _queryService);
                services.AddScoped<ISqlGenerationService>(_ => _sqlGenerationService);
                services.AddSingleton<Kernel>(_ => null!);
            });
        });
    }

    [Fact]
    public async Task GetTools_ReturnsThreeTools()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/mcp/tools");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<McpListToolsResponse>();

        Assert.NotNull(result);
        Assert.Equal(3, result.Tools.Count);
        Assert.Contains(result.Tools, t => t.Name == "scan_database_structure");
        Assert.Contains(result.Tools, t => t.Name == "query_database_data");
        Assert.Contains(result.Tools, t => t.Name == "advanced_sql_query");
    }

    [Fact]
    public async Task GetTools_AllToolsHaveDescriptions()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/mcp/tools");
        var result = await response.Content.ReadFromJsonAsync<McpListToolsResponse>();

        // Assert
        Assert.NotNull(result);
        foreach (var tool in result.Tools)
        {
            Assert.False(string.IsNullOrEmpty(tool.Name));
            Assert.False(string.IsNullOrEmpty(tool.Description));
            Assert.NotNull(tool.InputSchema);
        }
    }

    [Fact]
    public async Task PostInitialize_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        var options = new ServerConnectionOptions
        {
            Host = "localhost",
            Port = 5432,
            Username = "postgres",
            Password = "password"
        };

        // Act
        var response = await client.PostAsJsonAsync("/mcp/initialize", options);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("success", result);
    }

    [Fact]
    public async Task PostInitialize_WithMissingUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var options = new ServerConnectionOptions
        {
            Host = "localhost",
            Port = 5432,
            Username = "",
            Password = "password"
        };

        // Act
        var response = await client.PostAsJsonAsync("/mcp/initialize", options);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetConfiguration_WhenConfigured_ReturnsConfiguration()
    {
        // Arrange
        var client = _factory.CreateClient();
        var serverConfig = new ServerConnectionOptions
        {
            Host = "localhost",
            Port = 5432,
            Username = "postgres",
            Password = "***"
        };
        _connectionBuilder.GetServerConfiguration().Returns(serverConfig);

        // Act
        var response = await client.GetAsync("/mcp/configuration");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("configured", result);
    }

    [Fact]
    public async Task GetConfiguration_WhenNotConfigured_ReturnsNotConfiguredMessage()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var notConfiguredBuilder = Substitute.For<IConnectionBuilderService>();
                notConfiguredBuilder.IsConfigured.Returns(false);
                services.AddSingleton(notConfiguredBuilder);
            });
        });
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/mcp/configuration");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("not initialized", result);
    }

    [Fact]
    public async Task PostCallTool_WithUnknownTool_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new McpToolCallRequest
        {
            Name = "unknown_tool",
            Arguments = new Dictionary<string, object?>()
        };

        // Act
        var response = await client.PostAsJsonAsync("/mcp/tools/call", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostCallTool_WhenNotInitialized_ReturnsBadRequest()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                var notConfiguredBuilder = Substitute.For<IConnectionBuilderService>();
                notConfiguredBuilder.IsConfigured.Returns(false);
                services.AddSingleton(notConfiguredBuilder);
            });
        });
        var client = factory.CreateClient();
        var request = new McpToolCallRequest
        {
            Name = "scan_database_structure",
            Arguments = new Dictionary<string, object?> { ["database"] = "testdb" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/mcp/tools/call", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<McpToolCallResponse>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Contains("not initialized", result.Error ?? "");
    }

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/mcp/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", result);
    }

    [Fact]
    public async Task PostCallTool_WithMissingDatabase_HandlesGracefully()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new McpToolCallRequest
        {
            Name = "scan_database_structure",
            Arguments = new Dictionary<string, object?>() // Missing database
        };

        // Act
        var response = await client.PostAsJsonAsync("/mcp/tools/call", request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.InternalServerError);
    }
}
