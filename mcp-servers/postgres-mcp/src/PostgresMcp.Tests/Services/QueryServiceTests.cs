using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PostgresMcp.Models;
using PostgresMcp.Services;

namespace PostgresMcp.Tests.Services;

public class QueryServiceTests
{
    private readonly Mock<ILogger<QueryService>> _loggerMock;
    private readonly Mock<IDatabaseSchemaService> _schemaServiceMock;
    private readonly IOptions<SecurityOptions> _securityOptions;

    public QueryServiceTests()
    {
        _loggerMock = new Mock<ILogger<QueryService>>();
        _schemaServiceMock = new Mock<IDatabaseSchemaService>();

        _securityOptions = Options.Create(new SecurityOptions
        {
            MaxRowsPerQuery = 1000,
            MaxQueryExecutionSeconds = 30,
            AllowDataModification = false,
            AllowSchemaModification = false
        });
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act
        var service = new QueryService(
            _loggerMock.Object,
            _schemaServiceMock.Object,
            _securityOptions);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void SecurityOptions_ShouldBeConfigured()
    {
        // Arrange & Act
        var options = _securityOptions.Value;

        // Assert
        Assert.Equal(1000, options.MaxRowsPerQuery);
        Assert.Equal(30, options.MaxQueryExecutionSeconds);
        Assert.False(options.AllowDataModification);
        Assert.False(options.AllowSchemaModification);
    }
}
