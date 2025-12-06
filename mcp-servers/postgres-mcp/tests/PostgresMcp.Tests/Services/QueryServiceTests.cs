using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using PostgresMcp.Models;
using PostgresMcp.Services;
using Xunit;

namespace PostgresMcp.Tests.Services;

public class QueryServiceTests
{
    private readonly ILogger<QueryService> _logger;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOptions<PostgresOptions> _postgresOptions;
    private readonly IOptions<SecurityOptions> _securityOptions;
    private readonly IOptions<McpLoggingOptions> _loggingOptions;
    private readonly QueryService _service;

    public QueryServiceTests()
    {
        _logger = Substitute.For<ILogger<QueryService>>();
        _connectionFactory = Substitute.For<IDbConnectionFactory>();
        _postgresOptions = Options.Create(new PostgresOptions());
        _securityOptions = Options.Create(new SecurityOptions());
        _loggingOptions = Options.Create(new McpLoggingOptions());

        _service = new QueryService(
            _logger,
            _connectionFactory,
            _postgresOptions,
            _securityOptions,
            _loggingOptions);
    }

    [Theory]
    [InlineData("SELECT * FROM users")]
    [InlineData("SELECT id, name FROM products WHERE price > 10")]
    [InlineData("WITH summary AS (SELECT * FROM sales) SELECT * FROM summary")]
    [InlineData("select * from users")] // Case insensitive
    public void ValidateQuerySafety_ValidQueries_ReturnsTrue(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("INSERT INTO users VALUES (1, 'test')")]
    [InlineData("UPDATE users SET name = 'test'")]
    [InlineData("DELETE FROM users")]
    [InlineData("DROP TABLE users")]
    [InlineData("ALTER TABLE users ADD COLUMN age INT")]
    [InlineData("TRUNCATE TABLE users")]
    [InlineData("GRANT SELECT ON users TO public")]
    public void ValidateQuerySafety_ModificationKeywords_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("BEGIN; SELECT 1; COMMIT;")]
    [InlineData("ROLLBACK")]
    [InlineData("SAVEPOINT sp1")]
    public void ValidateQuerySafety_TransactionKeywords_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("SELECT * FROM pg_read_file('test')")]
    [InlineData("COPY users TO stdout")]
    [InlineData("VACUUM FULL users")]
    public void ValidateQuerySafety_DangerousFunctionsAndMaintenance_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("; DROP TABLE users")]
    [InlineData("SELECT * FROM users; DELETE FROM users")]
    public void ValidateQuerySafety_InjectionAttempts_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("SET session_replication_role = 'replica'")]
    [InlineData("RESET ALL")]
    public void ValidateQuerySafety_ConfigurationCommands_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("LISTEN events")]
    [InlineData("NOTIFY channel, 'message'")]
    [InlineData("UNLISTEN *")]
    public void ValidateQuerySafety_MessagingCommands_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("LOCK TABLE users IN ACCESS EXCLUSIVE MODE")]
    [InlineData("SELECT * FROM users FOR UPDATE")]
    public void ValidateQuerySafety_LockingCommands_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("DO $$ BEGIN RAISE NOTICE 'test'; END $$")]
    [InlineData("SELECT * FROM pg_read_file('/etc/passwd')")]
    public void ValidateQuerySafety_ProceduralCodeAndDangerousFunctions_ReturnsFalse(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateQuerySafety_WithNullQuery_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateQuerySafety(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateQuerySafety_WithEmptyQuery_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateQuerySafety("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateQuerySafety_WithWhitespaceQuery_ReturnsFalse()
    {
        // Act
        var result = _service.ValidateQuerySafety("   ");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("SELECT * FROM users -- inline comment")]
    [InlineData("SELECT * /* mid-query comment */ FROM users")]
    [InlineData("SELECT col1, col2 FROM users /* end comment */")]
    public void ValidateQuerySafety_CommentsInQueries_AreHandledCorrectly(string query)
    {
        // Act
        var result = _service.ValidateQuerySafety(query);

        // Assert
        Assert.True(result);
    }
}
