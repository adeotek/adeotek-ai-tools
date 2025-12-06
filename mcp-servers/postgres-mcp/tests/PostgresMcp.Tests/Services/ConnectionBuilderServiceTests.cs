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

    public ConnectionBuilderServiceTests()
    {
        _logger = Substitute.For<ILogger<ConnectionBuilderService>>();
    }

    [Fact]
    public void Constructor_WithoutConnectionString_ServiceNotConfigured()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions());

        // Act
        var service = new ConnectionBuilderService(_logger, options);

        // Assert
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void Constructor_WithValidConnectionString_ServiceConfigured()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass"
        });

        // Act
        var service = new ConnectionBuilderService(_logger, options);

        // Assert
        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void Constructor_WithInvalidConnectionString_ServiceNotConfigured()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "invalid connection string"
        });

        // Act
        var service = new ConnectionBuilderService(_logger, options);

        // Assert
        // Service should gracefully handle invalid connection string and remain unconfigured
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void BuildConnectionString_WhenNotConfigured_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, options);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.BuildConnectionString("testdb"));
        Assert.Contains("not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildConnectionString_WithConfiguredService_ReturnsValidConnectionString()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass",
            ConnectionTimeoutSeconds = 30,
            CommandTimeoutSeconds = 60,
            MaxPoolSize = 100,
            MinPoolSize = 0
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var connectionString = service.BuildConnectionString("mydatabase");

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("Database=mydatabase", connectionString);
        Assert.Contains("Host=localhost", connectionString);
        Assert.Contains("Port=5432", connectionString);
        Assert.Contains("Username=testuser", connectionString);
    }

    [Fact]
    public void BuildConnectionString_WithDifferentDatabases_ReturnsDifferentConnectionStrings()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass"
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var connString1 = service.BuildConnectionString("database1");
        var connString2 = service.BuildConnectionString("database2");

        // Assert
        Assert.Contains("Database=database1", connString1);
        Assert.Contains("Database=database2", connString2);
        Assert.NotEqual(connString1, connString2);
    }

    [Fact]
    public void ConfigureServer_WithValidOptions_ConfiguresService()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, options);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "testhost",
            Port = 5433,
            Username = "testuser",
            Password = "testpass"
        };

        // Act
        service.ConfigureServer(serverOptions);

        // Assert
        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void ConfigureServer_OverridesConnectionStringConfiguration()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=originalhost;Port=5432;Username=original;Password=pass"
        });
        var service = new ConnectionBuilderService(_logger, options);
        
        var newServerOptions = new ServerConnectionOptions
        {
            Host = "newhost",
            Port = 5433,
            Username = "newuser",
            Password = "newpass"
        };

        // Act
        service.ConfigureServer(newServerOptions);
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("Host=newhost", connectionString);
        Assert.Contains("Port=5433", connectionString);
        Assert.DoesNotContain("originalhost", connectionString);
    }

    [Fact]
    public void GetServerConfiguration_WhenNotConfigured_ThrowsException()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, options);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.GetServerConfiguration());
        Assert.Contains("not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetServerConfiguration_WhenConfigured_ReturnsConfigurationWithoutPassword()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=testhost;Port=5432;Username=testuser;Password=secretpassword"
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var config = service.GetServerConfiguration();

        // Assert
        Assert.Equal("testhost", config.Host);
        Assert.Equal(5432, config.Port);
        Assert.Equal("testuser", config.Username);
        Assert.Equal("***", config.Password); // Password should be masked
    }

    [Fact]
    public void BuildConnectionString_WithSslEnabled_IncludesSslConfiguration()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass",
            UseSsl = true
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("SSL Mode=Prefer", connectionString);
    }

    [Fact]
    public void BuildConnectionString_WithSslDisabled_DisablesSsl()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass",
            UseSsl = false
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("SSL Mode=Disable", connectionString);
    }

    [Fact]
    public void BuildConnectionString_IncludesPoolingConfiguration()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass",
            MaxPoolSize = 50,
            MinPoolSize = 5
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("Maximum Pool Size=50", connectionString);
        Assert.Contains("Minimum Pool Size=5", connectionString);
    }

    [Fact]
    public void BuildConnectionString_IncludesTimeoutConfiguration()
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass",
            ConnectionTimeoutSeconds = 45,
            CommandTimeoutSeconds = 90
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("Timeout=45", connectionString);
        Assert.Contains("Command Timeout=90", connectionString);
    }

    [Theory]
    [InlineData("mydb")]
    [InlineData("production_db")]
    [InlineData("test-database")]
    [InlineData("db_123")]
    public void BuildConnectionString_WithVariousDatabaseNames_CreatesValidConnectionStrings(string databaseName)
    {
        // Arrange
        var options = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass"
        });
        var service = new ConnectionBuilderService(_logger, options);

        // Act
        var connectionString = service.BuildConnectionString(databaseName);

        // Assert
        Assert.Contains($"Database={databaseName}", connectionString);
    }
}
