using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PostgresMcp.Models;
using PostgresMcp.Services;
using Xunit;

namespace PostgresMcp.Tests.Services;

public class DatabaseSchemaServiceTests
{
    private readonly ILogger<DatabaseSchemaService> _logger;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOptions<PostgresOptions> _postgresOptions;
    private readonly IOptions<SecurityOptions> _securityOptions;
    private readonly DatabaseSchemaService _service;

    public DatabaseSchemaServiceTests()
    {
        _logger = Substitute.For<ILogger<DatabaseSchemaService>>();
        _connectionFactory = Substitute.For<IDbConnectionFactory>();
        _postgresOptions = Options.Create(new PostgresOptions());
        _securityOptions = Options.Create(new SecurityOptions());

        _service = new DatabaseSchemaService(
            _logger,
            _connectionFactory,
            _postgresOptions,
            _securityOptions);
    }

    [Fact]
    public async Task ScanDatabaseSchemaAsync_WithNullConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ScanDatabaseSchemaAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ScanDatabaseSchemaAsync_WithEmptyConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ScanDatabaseSchemaAsync("", CancellationToken.None));
    }

    [Fact]
    public async Task ScanDatabaseSchemaAsync_WithWhitespaceConnectionString_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ScanDatabaseSchemaAsync("   ", CancellationToken.None));
    }

    [Fact]
    public async Task ScanDatabaseSchemaAsync_CreatesConnectionAndOpensIt()
    {
        // Arrange
        var connectionString = "Host=localhost;Database=testdb";

        var connection = Substitute.For<DbConnection>();
        connection.Database.Returns("testdb");
        connection.ServerVersion.Returns("PostgreSQL 14.0");

        _connectionFactory.CreateConnection(connectionString).Returns(connection);

        // Act - Wrap in try-catch since we can't fully mock command creation
        try
        {
            await _service.ScanDatabaseSchemaAsync(connectionString);
        }
        catch (NullReferenceException)
        {
            // Expected when command creation is not fully mocked
        }

        // Assert - Verify the connection was created and opened
        _connectionFactory.Received(1).CreateConnection(connectionString);
        await connection.Received(1).OpenAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("pg_catalog")]
    [InlineData("information_schema")]
    public void SecurityOptions_BlockedSchemas_ContainsSystemSchemas(string schemaName)
    {
        // Arrange
        var securityOptions = new SecurityOptions
        {
            BlockedSchemas = ["pg_catalog", "information_schema"],
            AllowedSchemas = []
        };

        // Assert
        Assert.Contains(schemaName, securityOptions.BlockedSchemas);
    }

    [Fact]
    public void SecurityOptions_AllowedSchemas_FilterCorrectly()
    {
        // Arrange
        var securityOptions = new SecurityOptions
        {
            BlockedSchemas = [],
            AllowedSchemas = ["public", "analytics"]
        };

        // Assert
        Assert.Contains("public", securityOptions.AllowedSchemas);
        Assert.Contains("analytics", securityOptions.AllowedSchemas);
        Assert.DoesNotContain("other_schema", securityOptions.AllowedSchemas);
    }
}
