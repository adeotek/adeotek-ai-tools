using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PostgresMcp.Models;
using PostgresMcp.Services;

namespace PostgresMcp.Tests.Services;

public class SqlGenerationServiceTests
{
    private readonly Mock<ILogger<SqlGenerationService>> _loggerMock;
    private readonly Mock<IDatabaseSchemaService> _schemaServiceMock;
    private readonly Mock<IQueryService> _queryServiceMock;
    private readonly IOptions<SecurityOptions> _securityOptions;
    private readonly IOptions<AiOptions> _aiOptions;

    public SqlGenerationServiceTests()
    {
        _loggerMock = new Mock<ILogger<SqlGenerationService>>();
        _schemaServiceMock = new Mock<IDatabaseSchemaService>();
        _queryServiceMock = new Mock<IQueryService>();

        _securityOptions = Options.Create(new SecurityOptions
        {
            AllowDataModification = false,
            AllowSchemaModification = false
        });

        _aiOptions = Options.Create(new AiOptions
        {
            Enabled = true,
            Model = "gpt-4"
        });
    }

    [Fact]
    public void ValidateSqlSafety_SelectQuery_ReturnsTrue()
    {
        // Arrange
        var service = new SqlGenerationService(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _queryServiceMock.Object,
            _securityOptions,
            _aiOptions);

        var sql = "SELECT * FROM users WHERE id = 1";

        // Act
        var result = service.ValidateSqlSafety(sql);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("INSERT INTO users (name) VALUES ('test')")]
    [InlineData("UPDATE users SET name = 'test' WHERE id = 1")]
    [InlineData("DELETE FROM users WHERE id = 1")]
    [InlineData("TRUNCATE TABLE users")]
    public void ValidateSqlSafety_DataModificationQuery_ReturnsFalse(string sql)
    {
        // Arrange
        var service = new SqlGenerationService(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _queryServiceMock.Object,
            _securityOptions,
            _aiOptions);

        // Act
        var result = service.ValidateSqlSafety(sql);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("CREATE TABLE test (id INT)")]
    [InlineData("ALTER TABLE users ADD COLUMN email VARCHAR(100)")]
    [InlineData("DROP TABLE users")]
    [InlineData("RENAME TABLE users TO people")]
    public void ValidateSqlSafety_SchemaModificationQuery_ReturnsFalse(string sql)
    {
        // Arrange
        var service = new SqlGenerationService(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _queryServiceMock.Object,
            _securityOptions,
            _aiOptions);

        // Act
        var result = service.ValidateSqlSafety(sql);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("SELECT pg_read_file('/etc/passwd')")]
    [InlineData("COPY users TO '/tmp/users.csv'")]
    [InlineData("SELECT pg_ls_dir('.')")]
    public void ValidateSqlSafety_DangerousFunctions_ReturnsFalse(string sql)
    {
        // Arrange
        var service = new SqlGenerationService(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _queryServiceMock.Object,
            _securityOptions,
            _aiOptions);

        // Act
        var result = service.ValidateSqlSafety(sql);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateSqlSafety_WithClause_ReturnsTrue()
    {
        // Arrange
        var service = new SqlGenerationService(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _queryServiceMock.Object,
            _securityOptions,
            _aiOptions);

        var sql = @"
            WITH recent_orders AS (
                SELECT * FROM orders WHERE order_date > NOW() - INTERVAL '30 days'
            )
            SELECT * FROM recent_orders";

        // Act
        var result = service.ValidateSqlSafety(sql);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateSqlSafety_ComplexSelectWithJoins_ReturnsTrue()
    {
        // Arrange
        var service = new SqlGenerationService(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _queryServiceMock.Object,
            _securityOptions,
            _aiOptions);

        var sql = @"
            SELECT
                c.customer_id,
                c.first_name,
                c.last_name,
                COUNT(o.order_id) as order_count,
                SUM(o.total_amount) as total_spent
            FROM customers c
            LEFT JOIN orders o ON c.customer_id = o.customer_id
            WHERE c.created_at > '2024-01-01'
            GROUP BY c.customer_id, c.first_name, c.last_name
            HAVING COUNT(o.order_id) > 5
            ORDER BY total_spent DESC
            LIMIT 10";

        // Act
        var result = service.ValidateSqlSafety(sql);

        // Assert
        result.Should().BeTrue();
    }
}
