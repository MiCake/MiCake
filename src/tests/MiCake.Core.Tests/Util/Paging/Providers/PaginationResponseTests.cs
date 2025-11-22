using System.Collections.Generic;
using MiCake.Util.Query.Paging.Providers;
using Xunit;

namespace MiCake.Core.Tests.Util.Paging.Providers;

public class PaginationResponseTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var response = new PaginationResponse<string>();

        // Assert
        Assert.Null(response.Data);
        Assert.False(response.HasMore);
        Assert.Null(response.NextOffset);
        Assert.Null(response.ErrorMessage);
        Assert.True(response.IsSuccess); // No error message means success
    }

    [Fact]
    public void Data_ShouldAllowAssignment()
    {
        // Arrange
        var response = new PaginationResponse<string>();
        var testData = new List<string> { "item1", "item2", "item3" };

        // Act
        response.Data = testData;

        // Assert
        Assert.Equal(testData, response.Data);
        Assert.Equal(3, response.Data.Count);
        Assert.Contains("item1", response.Data);
    }

    [Fact]
    public void HasMore_ShouldIndicateContinuation()
    {
        // Arrange
        var response = new PaginationResponse<string>();

        // Act & Assert
        response.HasMore = true;
        Assert.True(response.HasMore);

        response.HasMore = false;
        Assert.False(response.HasMore);
    }

    [Fact]
    public void NextOffset_ShouldAllowNullAndIntValues()
    {
        // Arrange
        var response = new PaginationResponse<string>();

        // Act & Assert
        response.NextOffset = null;
        Assert.Null(response.NextOffset);

        response.NextOffset = 100;
        Assert.Equal(100, response.NextOffset);

        response.NextOffset = 0;
        Assert.Equal(0, response.NextOffset);
    }

    [Fact]
    public void ErrorMessage_ShouldAllowNullAndStringValues()
    {
        // Arrange
        var response = new PaginationResponse<string>();

        // Act & Assert
        response.ErrorMessage = null;
        Assert.Null(response.ErrorMessage);
        Assert.True(response.IsSuccess);

        response.ErrorMessage = "Test error";
        Assert.Equal("Test error", response.ErrorMessage);
        Assert.False(response.IsSuccess);

        response.ErrorMessage = string.Empty;
        Assert.Equal(string.Empty, response.ErrorMessage);
        Assert.True(response.IsSuccess); // Empty string is considered success
    }

    [Fact]
    public void IsSuccess_ShouldDependOnErrorMessage()
    {
        // Arrange
        var response = new PaginationResponse<string>();

        // Act & Assert - No error message
        response.ErrorMessage = null;
        Assert.True(response.IsSuccess);

        // Empty error message
        response.ErrorMessage = string.Empty;
        Assert.True(response.IsSuccess);

        // Whitespace error message
        response.ErrorMessage = "   ";
        Assert.False(response.IsSuccess);

        // Actual error message
        response.ErrorMessage = "Something went wrong";
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public void CompleteResponse_ShouldSupportAllProperties()
    {
        // Arrange
        var data = new List<int> { 1, 2, 3, 4, 5 };
        var response = new PaginationResponse<int>
        {
            Data = data,
            HasMore = true,
            NextOffset = 5,
            ErrorMessage = null
        };

        // Act & Assert
        Assert.Equal(data, response.Data);
        Assert.True(response.HasMore);
        Assert.Equal(5, response.NextOffset);
        Assert.Null(response.ErrorMessage);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void ErrorResponse_ShouldIndicateFailure()
    {
        // Arrange
        var response = new PaginationResponse<string>
        {
            Data = null,
            HasMore = false,
            NextOffset = null,
            ErrorMessage = "Network timeout"
        };

        // Act & Assert
        Assert.Null(response.Data);
        Assert.False(response.HasMore);
        Assert.Null(response.NextOffset);
        Assert.Equal("Network timeout", response.ErrorMessage);
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public void EmptyDataResponse_ShouldStillBeSuccess()
    {
        // Arrange
        var response = new PaginationResponse<string>
        {
            Data = new List<string>(),
            HasMore = false,
            NextOffset = null,
            ErrorMessage = null
        };

        // Act & Assert
        Assert.NotNull(response.Data);
        Assert.Empty(response.Data);
        Assert.False(response.HasMore);
        Assert.Null(response.NextOffset);
        Assert.Null(response.ErrorMessage);
        Assert.True(response.IsSuccess);
    }
}