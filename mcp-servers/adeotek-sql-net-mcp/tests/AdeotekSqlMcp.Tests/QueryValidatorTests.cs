using Xunit;
using FluentAssertions;
using AdeotekSqlMcp.Security;
using AdeotekSqlMcp.Utilities;

namespace AdeotekSqlMcp.Tests;

public class QueryValidatorTests
{
    private readonly QueryValidator _validator = new();

    [Theory]
    [InlineData("SELECT * FROM users")]
    [InlineData("SELECT id, name FROM products WHERE active = true")]
    [InlineData("SELECT COUNT(*) FROM orders")]
    [InlineData("WITH cte AS (SELECT * FROM users) SELECT * FROM cte")]
    [InlineData("EXPLAIN SELECT * FROM users")]
    public void Validate_ValidSelectQueries_ReturnsValid(string query)
    {
        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("INSERT INTO users VALUES (1, 'Test')")]
    [InlineData("UPDATE users SET name = 'Test'")]
    [InlineData("DELETE FROM users")]
    [InlineData("DROP TABLE users")]
    [InlineData("CREATE TABLE test (id int)")]
    [InlineData("ALTER TABLE users ADD COLUMN email VARCHAR(255)")]
    [InlineData("TRUNCATE TABLE users")]
    public void Validate_ModificationQueries_ReturnsInvalid(string query)
    {
        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("SELECT * FROM users; DROP TABLE users;")]
    [InlineData("SELECT * FROM users;SELECT * FROM products;")]
    public void Validate_MultipleStatements_ReturnsInvalid(string query)
    {
        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Multiple statements"));
    }

    [Theory]
    [InlineData("SELECT pg_read_file('/etc/passwd')")]
    [InlineData("SELECT * FROM users WHERE id = 1; EXEC xp_cmdshell 'dir'")]
    public void Validate_DangerousFunctions_ReturnsInvalid(string query)
    {
        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Dangerous function") || e.Contains("Blocked keyword"));
    }

    [Fact]
    public void ValidateOrThrow_InvalidQuery_ThrowsException()
    {
        // Arrange
        var query = "DROP TABLE users";

        // Act & Assert
        Assert.Throws<QueryValidationException>(() => _validator.ValidateOrThrow(query));
    }

    [Fact]
    public void EnforceLimit_QueryWithoutLimit_AddsLimit()
    {
        // Arrange
        var query = "SELECT * FROM users";

        // Act
        var result = _validator.EnforceLimit(query, 100);

        // Assert
        result.Should().Contain("LIMIT 100");
    }

    [Fact]
    public void EnforceLimit_QueryWithHighLimit_ReducesLimit()
    {
        // Arrange
        var query = "SELECT * FROM users LIMIT 50000";

        // Act
        var result = _validator.EnforceLimit(query, 1000);

        // Assert
        result.Should().Contain("LIMIT 1000");
        result.Should().NotContain("50000");
    }

    [Theory]
    [InlineData("users", "users")]
    [InlineData("my_table", "my_table")]
    [InlineData("schema.table", "schema.table")]
    public void SanitizeIdentifier_ValidIdentifiers_ReturnsUnchanged(string identifier, string expected)
    {
        // Act
        var result = _validator.SanitizeIdentifier(identifier);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("users; DROP TABLE")]
    [InlineData("users' OR '1'='1")]
    [InlineData("users--")]
    public void SanitizeIdentifier_InvalidIdentifiers_ThrowsException(string identifier)
    {
        // Act & Assert
        Assert.Throws<QueryValidationException>(() => _validator.SanitizeIdentifier(identifier));
    }
}
