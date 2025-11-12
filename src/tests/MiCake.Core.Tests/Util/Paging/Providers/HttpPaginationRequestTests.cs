using System.Collections.Generic;
using System.Net.Http;
using MiCake.Util.Paging.Providers;
using Xunit;

namespace MiCake.Core.Tests.Util.Paging.Providers;

public class HttpPaginationRequestTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var request = new HttpPaginationRequest();

        // Assert
        Assert.Equal(string.Empty, request.BaseUrl);
        Assert.Null(request.QueryParameters);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Null(request.Body);
        Assert.Null(request.Headers);
    }

    [Fact]
    public void BaseUrl_ShouldAllowAssignment()
    {
        // Arrange
        var request = new HttpPaginationRequest();
        var url = "https://api.example.com/data";

        // Act
        request.BaseUrl = url;

        // Assert
        Assert.Equal(url, request.BaseUrl);
    }

    [Fact]
    public void QueryParameters_ShouldAllowAssignment()
    {
        // Arrange
        var request = new HttpPaginationRequest();
        var parameters = new Dictionary<string, string>
        {
            ["filter"] = "active",
            ["sort"] = "date"
        };

        // Act
        request.QueryParameters = parameters;

        // Assert
        Assert.Equal(parameters, request.QueryParameters);
        Assert.Equal("active", request.QueryParameters["filter"]);
        Assert.Equal("date", request.QueryParameters["sort"]);
    }

    [Fact]
    public void Method_ShouldAllowAllHttpMethods()
    {
        // Arrange
        var request = new HttpPaginationRequest();

        // Act & Assert
        request.Method = HttpMethod.Get;
        Assert.Equal(HttpMethod.Get, request.Method);

        request.Method = HttpMethod.Post;
        Assert.Equal(HttpMethod.Post, request.Method);

        request.Method = HttpMethod.Put;
        Assert.Equal(HttpMethod.Put, request.Method);

        request.Method = HttpMethod.Delete;
        Assert.Equal(HttpMethod.Delete, request.Method);
    }

    [Fact]
    public void Body_ShouldAllowStringContent()
    {
        // Arrange
        var request = new HttpPaginationRequest();
        var jsonBody = "{\"query\": \"test\"}";

        // Act
        request.Body = jsonBody;

        // Assert
        Assert.Equal(jsonBody, request.Body);
    }

    [Fact]
    public void Headers_ShouldAllowAssignment()
    {
        // Arrange
        var request = new HttpPaginationRequest();
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["Accept"] = "application/json"
        };

        // Act
        request.Headers = headers;

        // Assert
        Assert.Equal(headers, request.Headers);
        Assert.Equal("Bearer token123", request.Headers["Authorization"]);
        Assert.Equal("application/json", request.Headers["Accept"]);
    }

    [Fact]
    public void CompleteRequest_ShouldSupportAllProperties()
    {
        // Arrange & Act
        var request = new HttpPaginationRequest
        {
            BaseUrl = "https://api.example.com/items",
            QueryParameters = new Dictionary<string, string>
            {
                ["category"] = "electronics",
                ["status"] = "available"
            },
            Method = HttpMethod.Post,
            Body = "{\"filters\": {\"price_max\": 1000}}",
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["User-Agent"] = "TestAgent/1.0"
            }
        };

        // Assert
        Assert.Equal("https://api.example.com/items", request.BaseUrl);
        Assert.Equal(2, request.QueryParameters.Count);
        Assert.Equal("electronics", request.QueryParameters["category"]);
        Assert.Equal("available", request.QueryParameters["status"]);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Contains("price_max", request.Body);
        Assert.Equal(2, request.Headers.Count);
        Assert.Equal("application/json", request.Headers["Content-Type"]);
        Assert.Equal("TestAgent/1.0", request.Headers["User-Agent"]);
    }

    [Fact]
    public void EmptyCollections_ShouldBeAllowed()
    {
        // Arrange
        var request = new HttpPaginationRequest();

        // Act
        request.QueryParameters = new Dictionary<string, string>();
        request.Headers = new Dictionary<string, string>();

        // Assert
        Assert.NotNull(request.QueryParameters);
        Assert.Empty(request.QueryParameters);
        Assert.NotNull(request.Headers);
        Assert.Empty(request.Headers);
    }

    [Fact]
    public void NullValues_ShouldBeAllowed()
    {
        // Arrange
        var request = new HttpPaginationRequest();

        // Act
        request.BaseUrl = null!;
        request.QueryParameters = null;
        request.Body = null;
        request.Headers = null;

        // Assert
        Assert.Null(request.BaseUrl);
        Assert.Null(request.QueryParameters);
        Assert.Null(request.Body);
        Assert.Null(request.Headers);
    }
}