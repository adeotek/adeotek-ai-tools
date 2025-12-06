using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PostgresMcp.Models;
using PostgresMcp.Services;
using Xunit;

namespace PostgresMcp.Tests.Services;

public class ConnectionBuilderServiceTests
{
    private readonly ILogger<ConnectionBuilderService> _logger;
    private readonly PostgresOptions _options;
    private readonly IOptions<PostgresOptions> _postgresOptions;

    public ConnectionBuilderServiceTests()
    {
        _logger = Substitute.For<ILogger<ConnectionBuilderService>>();
        _options = new PostgresOptions();
        _postgresOptions = Options.Create(_options);
    }

    [Fact]
    public void IsConfigured_WhenConnectionStringProvided_ReturnsTrue()
    {
        // Arrange
        _options.ConnectionString = "Host=localhost;Database=test";
        var service = new ConnectionBuilderService(_logger, _postgresOptions);

        // Act & Assert
        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_WhenLegacyConfigProvided_ReturnsTrue()
    {
        // Arrange
        var service = new ConnectionBuilderService(_logger, _postgresOptions);
        service.ConfigureServer(new ServerConnectionOptions
        {
            Host = "localhost",
            Port = 5432,
            Username = "user",
            Password = "pass"
        });

        // Act & Assert
        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_WhenUnconfigured_ReturnsFalse()
    {
        // Arrange
        var service = new ConnectionBuilderService(_logger, _postgresOptions);

        // Act & Assert
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void BuildConnectionString_WithConnectionStringConfig_SwapsDatabase()
    {
        // Arrange
        _options.ConnectionString = "Host=myhost;Port=5433;Database=original_db;Username=user;Password=pass";
        var service = new ConnectionBuilderService(_logger, _postgresOptions);

        // Act
        var result = service.BuildConnectionString("target_db");

        // Assert
        Assert.Contains("Database=target_db", result);
        Assert.Contains("Host=myhost", result);
        Assert.Contains("Port=5433", result);
        // Should not contain the original database
        Assert.DoesNotContain("Database=original_db", result);
    }

    [Fact]
    public void BuildConnectionString_WithLegacyConfig_BuildsCorrectString()
    {
        // Arrange
        var service = new ConnectionBuilderService(_logger, _postgresOptions);
        service.ConfigureServer(new ServerConnectionOptions
        {
            Host = "legacyhost",
            Port = 9999,
            Username = "legacyuser",
            Password = "legacypass"
        });

        // Act
        var result = service.BuildConnectionString("target_db");

        // Assert
        Assert.Contains("Host=legacyhost", result);
        Assert.Contains("Port=9999", result);
        Assert.Contains("Database=target_db", result);
        Assert.Contains("Username=legacyuser", result);
        Assert.Contains("Password=legacypass", result);
    }

    [Fact]
    public void BuildConnectionString_ThrowsWhenNotConfigured()
    {
        // Arrange
        var service = new ConnectionBuilderService(_logger, _postgresOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.BuildConnectionString("db"));
    }

    [Fact]
    public void GetServerConfiguration_MasksPassword()
    {
        // Arrange
        _options.ConnectionString = "Host=host;Password=secret";
        var service = new ConnectionBuilderService(_logger, _postgresOptions);

        // Act
        var config = service.GetServerConfiguration();

        // Assert
        Assert.Equal("***", config.Password);
        Assert.Equal("host", config.Host);
    }
}
