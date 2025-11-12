using System.Collections.Generic;
using MiCake.Util.Paging.Providers;
using Xunit;

namespace MiCake.Core.Tests.Util.Paging.Providers;

public class PaginationRequestTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var request = new PaginationRequest<string>();

        // Assert
        Assert.Null(request.Request);
        Assert.Equal(0, request.Offset);
        Assert.Equal(0, request.Limit);
        Assert.Null(request.Identifier);
    }

    [Fact]
    public void Properties_ShouldAllowAssignment()
    {
        // Arrange
        var request = new PaginationRequest<string>();
        var testRequest = "test-request";
        var identifier = "test-identifier";

        // Act
        request.Request = testRequest;
        request.Offset = 100;
        request.Limit = 50;
        request.Identifier = identifier;

        // Assert
        Assert.Equal(testRequest, request.Request);
        Assert.Equal(100, request.Offset);
        Assert.Equal(50, request.Limit);
        Assert.Equal(identifier, request.Identifier);
    }

    [Fact]
    public void Request_ShouldSupportGenericTypes()
    {
        // Arrange
        var complexRequest = new Dictionary<string, object>
        {
            ["filter"] = "active",
            ["sort"] = "date_desc"
        };

        // Act
        var request = new PaginationRequest<Dictionary<string, object>>
        {
            Request = complexRequest,
            Offset = 20,
            Limit = 10,
            Identifier = "complex-request"
        };

        // Assert
        Assert.Equal(complexRequest, request.Request);
        Assert.Equal("active", request.Request["filter"]);
        Assert.Equal("date_desc", request.Request["sort"]);
    }

    [Fact]
    public void Offset_ShouldAllowNonNegativeValues()
    {
        // Arrange
        var request = new PaginationRequest<string>();

        // Act & Assert
        request.Offset = 0;
        Assert.Equal(0, request.Offset);

        request.Offset = 1000;
        Assert.Equal(1000, request.Offset);
    }

    [Fact]
    public void Limit_ShouldAllowPositiveValues()
    {
        // Arrange
        var request = new PaginationRequest<string>();

        // Act & Assert
        request.Limit = 1;
        Assert.Equal(1, request.Limit);

        request.Limit = 1000;
        Assert.Equal(1000, request.Limit);
    }

    [Fact]
    public void Identifier_ShouldAllowNullAndStringValues()
    {
        // Arrange
        var request = new PaginationRequest<string>();

        // Act & Assert
        request.Identifier = null;
        Assert.Null(request.Identifier);

        request.Identifier = "test";
        Assert.Equal("test", request.Identifier);

        request.Identifier = string.Empty;
        Assert.Equal(string.Empty, request.Identifier);
    }
}