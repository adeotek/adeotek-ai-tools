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
    public void Constructor_WithValidConnectionString_ConfiguresService()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=testdb"
        });

        // Act
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Assert
        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_DoesNotConfigure()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = ""
        });

        // Act
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Assert
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void Constructor_WithNullConnectionString_DoesNotConfigure()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = null
        });

        // Act
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Assert
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void Constructor_WithInvalidConnectionString_DoesNotConfigure()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "InvalidConnectionString"
        });

        // Act
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Assert
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void ConfigureServer_WithValidOptions_ConfiguresService()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "testhost",
            Port = 5432,
            Username = "testuser",
            Password = "testpass"
        };

        // Act
        service.ConfigureServer(serverOptions);

        // Assert
        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void ConfigureServer_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.ConfigureServer(null!));
    }

    [Fact]
    public void ConfigureServer_WithEmptyUsername_ThrowsArgumentException()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "testhost",
            Port = 5432,
            Username = "",
            Password = "testpass"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.ConfigureServer(serverOptions));
    }

    [Fact]
    public void ConfigureServer_WithEmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "testhost",
            Port = 5432,
            Username = "testuser",
            Password = ""
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.ConfigureServer(serverOptions));
    }

    [Fact]
    public void BuildConnectionString_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.BuildConnectionString("testdb"));
    }

    [Fact]
    public void BuildConnectionString_WithNullDatabase_ThrowsArgumentException()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=postgres"
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.BuildConnectionString(null!));
    }

    [Fact]
    public void BuildConnectionString_WithEmptyDatabase_ThrowsArgumentException()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=postgres"
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.BuildConnectionString(""));
    }

    [Fact]
    public void BuildConnectionString_FromConfiguration_ReturnsValidConnectionString()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=postgres",
            ConnectionTimeoutSeconds = 30,
            CommandTimeoutSeconds = 60,
            MaxPoolSize = 100,
            MinPoolSize = 10
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act
        var connectionString = service.BuildConnectionString("mydb");

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("Database=mydb", connectionString);
        Assert.Contains("Host=localhost", connectionString);
        Assert.Contains("Username=testuser", connectionString);
    }

    [Fact]
    public void BuildConnectionString_FromRuntimeConfiguration_ReturnsValidConnectionString()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionTimeoutSeconds = 30,
            CommandTimeoutSeconds = 60,
            MaxPoolSize = 100,
            MinPoolSize = 10
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "runtimehost",
            Port = 5433,
            Username = "runtimeuser",
            Password = "runtimepass"
        };

        service.ConfigureServer(serverOptions);

        // Act
        var connectionString = service.BuildConnectionString("mydb");

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("Database=mydb", connectionString);
        Assert.Contains("Host=runtimehost", connectionString);
        Assert.Contains("Port=5433", connectionString);
        Assert.Contains("Username=runtimeuser", connectionString);
    }

    [Fact]
    public void BuildConnectionString_DifferentDatabases_ReturnsDifferentConnectionStrings()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=postgres"
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act
        var connectionString1 = service.BuildConnectionString("db1");
        var connectionString2 = service.BuildConnectionString("db2");

        // Assert
        Assert.NotEqual(connectionString1, connectionString2);
        Assert.Contains("Database=db1", connectionString1);
        Assert.Contains("Database=db2", connectionString2);
    }

    [Fact]
    public void BuildConnectionString_AppliesPoolingOptions()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=postgres",
            MaxPoolSize = 50,
            MinPoolSize = 5
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("Maximum Pool Size=50", connectionString);
        Assert.Contains("Minimum Pool Size=5", connectionString);
    }

    [Fact]
    public void BuildConnectionString_AppliesTimeoutOptions()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=postgres",
            ConnectionTimeoutSeconds = 45,
            CommandTimeoutSeconds = 90
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("Timeout=45", connectionString);
        Assert.Contains("Command Timeout=90", connectionString);
    }

    [Fact]
    public void GetServerConfiguration_WhenNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GetServerConfiguration());
    }

    [Fact]
    public void GetServerConfiguration_FromConfiguration_ReturnsConfigurationWithoutPassword()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=testhost;Port=5433;Username=testuser;Password=secret123;Database=postgres"
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act
        var config = service.GetServerConfiguration();

        // Assert
        Assert.Equal("testhost", config.Host);
        Assert.Equal(5433, config.Port);
        Assert.Equal("testuser", config.Username);
        Assert.Equal("***", config.Password); // Password should be masked
        Assert.True(config.IsConfigured);
    }

    [Fact]
    public void GetServerConfiguration_FromRuntime_ReturnsConfigurationWithoutPassword()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "runtimehost",
            Port = 5434,
            Username = "runtimeuser",
            Password = "secret456"
        };

        service.ConfigureServer(serverOptions);

        // Act
        var config = service.GetServerConfiguration();

        // Assert
        Assert.Equal("runtimehost", config.Host);
        Assert.Equal(5434, config.Port);
        Assert.Equal("runtimeuser", config.Username);
        Assert.Equal("***", config.Password); // Password should be masked
        Assert.True(config.IsConfigured);
    }

    [Fact]
    public void ConfigureServer_OverridesConfigurationBasedSetup()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=confighost;Port=5432;Username=configuser;Password=configpass;Database=postgres"
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "runtimehost",
            Port = 5433,
            Username = "runtimeuser",
            Password = "runtimepass"
        };

        // Act
        service.ConfigureServer(serverOptions);
        var connectionString = service.BuildConnectionString("testdb");

        // Assert
        Assert.Contains("Host=runtimehost", connectionString);
        Assert.Contains("Port=5433", connectionString);
        Assert.Contains("Username=runtimeuser", connectionString);
    }

    [Fact]
    public void IsConfigured_AfterConfigureServer_ReturnsTrue()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions());
        var service = new ConnectionBuilderService(_logger, postgresOptions);
        var serverOptions = new ServerConnectionOptions
        {
            Host = "testhost",
            Port = 5432,
            Username = "testuser",
            Password = "testpass"
        };

        // Act
        service.ConfigureServer(serverOptions);

        // Assert
        Assert.True(service.IsConfigured);
    }

    [Fact]
    public async Task IsConfigured_ThreadSafe_HandlesMultipleCalls()
    {
        // Arrange
        var postgresOptions = Options.Create(new PostgresOptions
        {
            ConnectionString = "Host=localhost;Port=5432;Username=testuser;Password=testpass;Database=postgres"
        });
        var service = new ConnectionBuilderService(_logger, postgresOptions);

        // Act - Simulate multiple threads checking IsConfigured
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            var isConfigured = service.IsConfigured;
            Assert.True(isConfigured);
        })).ToArray();

        // Assert - All tasks should complete without exception
        await Task.WhenAll(tasks);
    }
}
