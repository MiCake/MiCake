using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using System;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    /// <summary>
    /// Unit tests for HttpContextExtensions methods.
    /// Tests verify consistent SlightException storage and retrieval across middleware and filters.
    /// </summary>
    public class HttpContextExtensions_Tests
    {
        [Fact]
        public void SetSlightException_StoresExceptionInContext()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test error", null, "TEST_CODE");

            // Act
            httpContext.SetSlightException(exception);

            // Assert
            var retrieved = httpContext.GetSlightException();
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void TryGetSlightException_WithValidException_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test error", null, "TEST_CODE");
            httpContext.SetSlightException(exception);

            // Act
            var result = httpContext.TryGetSlightException(out var retrieved);

            // Assert
            Assert.True(result);
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void TryGetSlightException_WithNoException_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var result = httpContext.TryGetSlightException(out var retrieved);

            // Assert
            Assert.False(result);
            Assert.Null(retrieved);
        }

        [Fact]
        public void HasSlightException_WithStoredException_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test error", null, "TEST_CODE");
            httpContext.SetSlightException(exception);

            // Act
            var hasException = httpContext.HasSlightException();

            // Assert
            Assert.True(hasException);
        }

        [Fact]
        public void HasSlightException_WithoutStoredException_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var hasException = httpContext.HasSlightException();

            // Assert
            Assert.False(hasException);
        }

        [Fact]
        public void GetSlightException_WithValidException_ReturnsException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test error", null, "TEST_CODE");
            httpContext.SetSlightException(exception);

            // Act
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void GetSlightException_WithNoException_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.Null(retrieved);
        }

        [Fact]
        public void SetSlightException_OverwritesPreviousValue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception1 = new SlightMiCakeException("Error 1", null, "ERROR_1");
            var exception2 = new SlightMiCakeException("Error 2", null, "ERROR_2");

            // Act
            httpContext.SetSlightException(exception1);
            var first = httpContext.GetSlightException();
            httpContext.SetSlightException(exception2);
            var second = httpContext.GetSlightException();

            // Assert
            Assert.Equal("ERROR_1", first.Code);
            Assert.Equal("ERROR_2", second.Code);
        }

        [Fact]
        public void SetSlightException_WithDifferentMessages_PreservesAll()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Detailed message", null, "CODE_123");

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.Equal("Detailed message", retrieved.Message);
            Assert.Equal("CODE_123", retrieved.Code);
        }

        [Fact]
        public void MultipleContexts_IsolateExceptions()
        {
            // Arrange
            var context1 = new DefaultHttpContext();
            var context2 = new DefaultHttpContext();
            var exception1 = new SlightMiCakeException("Error 1", null, "ERROR_1");
            var exception2 = new SlightMiCakeException("Error 2", null, "ERROR_2");

            // Act
            context1.SetSlightException(exception1);
            context2.SetSlightException(exception2);

            // Assert
            Assert.Equal("ERROR_1", context1.GetSlightException().Code);
            Assert.Equal("ERROR_2", context2.GetSlightException().Code);
        }

        [Fact]
        public void HasSlightException_AfterSet_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test error", null, "TEST_CODE");

            // Act
            Assert.False(httpContext.HasSlightException());
            httpContext.SetSlightException(exception);

            // Assert
            Assert.True(httpContext.HasSlightException());
        }

        [Fact]
        public void SlightException_WithNullMessage_HandledGracefully()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException(null, null, "TEST_CODE");

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void SlightException_WithComplexDetails_Preserved()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Error with details", null, "ERROR_WITH_DETAILS");

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("ERROR_WITH_DETAILS", retrieved.Code);
            Assert.Equal("Error with details", retrieved.Message);
        }

        [Fact]
        public void TryGetSlightException_OutParameter_SetCorrectly()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test", null, "TEST");
            httpContext.SetSlightException(exception);

            // Act
            httpContext.TryGetSlightException(out var outException);

            // Assert
            Assert.NotNull(outException);
            Assert.Equal("TEST", outException.Code);
        }

        [Fact]
        public void Extension_ChainedCalls_WorkCorrectly()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test", null, "TEST");

            // Act & Assert - Chain of calls should work
            httpContext.SetSlightException(exception);
            Assert.True(httpContext.HasSlightException());
            var result = httpContext.TryGetSlightException(out var retrieved);
            Assert.True(result);
            Assert.Equal("TEST", retrieved.Code);
        }

        [Fact]
        public void GetSlightException_ConsistentWithTryGet()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test", null, "TEST");
            httpContext.SetSlightException(exception);

            // Act
            httpContext.TryGetSlightException(out var fromTry);
            var fromGet = httpContext.GetSlightException();

            // Assert
            Assert.Equal(fromTry.Code, fromGet.Code);
            Assert.Equal(fromTry.Message, fromGet.Message);
        }

        [Fact]
        public void SlightException_Type_AlwaysPreserved()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new SlightMiCakeException("Test", null, "TEST");

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.IsType<SlightMiCakeException>(retrieved);
        }

        [Fact]
        public void SetAndGet_RoundTrip_Preserves_Equality()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var originalCode = "ROUND_TRIP_TEST";
            var originalMessage = "Round trip test message";
            var exception = new SlightMiCakeException(originalMessage, null, originalCode);

            // Act
            httpContext.SetSlightException(exception);
            var retrieved = httpContext.GetSlightException();

            // Assert
            Assert.Equal(originalCode, retrieved.Code);
            Assert.Equal(originalMessage, retrieved.Message);
        }
    }
}
