using Xunit;
using FluentAssertions;
using AdeotekSqlMcp.Database;
using AdeotekSqlMcp.Utilities;

namespace AdeotekSqlMcp.Tests;

public class ConnectionStringParserTests
{
    [Fact]
    public void Parse_ValidPostgresConnectionString_ReturnsConfig()
    {
        // Arrange
        var connStr = "type=postgres;host=localhost;port=5432;user=postgres;password=secret;database=mydb;ssl=true";

        // Act
        var config = ConnectionStringParser.Parse(connStr);

        // Assert
        config.Type.Should().Be("postgres");
        config.Host.Should().Be("localhost");
        config.Port.Should().Be(5432);
        config.User.Should().Be("postgres");
        config.Password.Should().Be("secret");
        config.Database.Should().Be("mydb");
        config.UseSsl.Should().BeTrue();
    }

    [Fact]
    public void Parse_ValidMssqlConnectionString_ReturnsConfig()
    {
        // Arrange
        var connStr = "type=mssql;host=localhost;port=1433;user=sa;password=StrongPass123;database=master";

        // Act
        var config = ConnectionStringParser.Parse(connStr);

        // Assert
        config.Type.Should().Be("mssql");
        config.Host.Should().Be("localhost");
        config.Port.Should().Be(1433);
        config.User.Should().Be("sa");
        config.Password.Should().Be("StrongPass123");
        config.Database.Should().Be("master");
    }

    [Fact]
    public void Parse_MissingType_ThrowsException()
    {
        // Arrange
        var connStr = "host=localhost;user=postgres;password=secret";

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => ConnectionStringParser.Parse(connStr));
    }

    [Fact]
    public void Parse_InvalidType_ThrowsException()
    {
        // Arrange
        var connStr = "type=mysql;host=localhost;user=root;password=secret";

        // Act & Assert
        var exception = Assert.Throws<ConfigurationException>(() => ConnectionStringParser.Parse(connStr));
        exception.Message.Should().Contain("Invalid database type");
    }

    [Fact]
    public void Parse_MissingHost_ThrowsException()
    {
        // Arrange
        var connStr = "type=postgres;user=postgres;password=secret";

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => ConnectionStringParser.Parse(connStr));
    }

    [Fact]
    public void Parse_MissingUser_ThrowsException()
    {
        // Arrange
        var connStr = "type=postgres;host=localhost;password=secret";

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => ConnectionStringParser.Parse(connStr));
    }

    [Fact]
    public void Parse_MissingPassword_ThrowsException()
    {
        // Arrange
        var connStr = "type=postgres;host=localhost;user=postgres";

        // Act & Assert
        Assert.Throws<ConfigurationException>(() => ConnectionStringParser.Parse(connStr));
    }

    [Fact]
    public void Parse_DefaultPort_UsesCorrectDefault()
    {
        // Arrange - Postgres
        var pgConnStr = "type=postgres;host=localhost;user=postgres;password=secret";

        // Act
        var pgConfig = ConnectionStringParser.Parse(pgConnStr);

        // Assert
        pgConfig.Port.Should().Be(5432);

        // Arrange - SQL Server
        var mssqlConnStr = "type=mssql;host=localhost;user=sa;password=secret";

        // Act
        var mssqlConfig = ConnectionStringParser.Parse(mssqlConnStr);

        // Assert
        mssqlConfig.Port.Should().Be(1433);
    }

    [Fact]
    public void Parse_AlternativeKeys_ParsesCorrectly()
    {
        // Arrange - Using alternative keys
        var connStr = "type=postgres;server=localhost;username=postgres;pwd=secret;initial catalog=mydb";

        // Act
        var config = ConnectionStringParser.Parse(connStr);

        // Assert
        config.Host.Should().Be("localhost");
        config.User.Should().Be("postgres");
        config.Password.Should().Be("secret");
        config.Database.Should().Be("mydb");
    }
}
