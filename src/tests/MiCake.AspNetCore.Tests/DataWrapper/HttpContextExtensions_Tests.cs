using MiCake.AspNetCore.Responses.Internals;
using MiCake.Core;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    /// <summary>
    /// Unit tests for HttpContextExtensions methods.
    /// Tests verify consistent BusinessException storage and retrieval across middleware and filters.
    /// </summary>
    public class HttpContextExtensions_Tests
    {
        [Fact]
        public void SetBusinessException_StoresExceptionInContext()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test error", null, "TEST_CODE");

            // Act
            httpContext.SetBusinessExceptionContext(exception);

            // Assert
            var retrieved = httpContext.GetBusinessException();
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void TryGetBusinessException_WithValidException_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test error", null, "TEST_CODE");
            httpContext.SetBusinessExceptionContext(exception);

            // Act
            var result = httpContext.TryGetBusinessException(out var retrieved);

            // Assert
            Assert.True(result);
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void TryGetBusinessException_WithNoException_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var result = httpContext.TryGetBusinessException(out var retrieved);

            // Assert
            Assert.False(result);
            Assert.Null(retrieved);
        }

        [Fact]
        public void HasBusinessException_WithStoredException_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test error", null, "TEST_CODE");
            httpContext.SetBusinessExceptionContext(exception);

            // Act
            var hasException = httpContext.HasBusinessException();

            // Assert
            Assert.True(hasException);
        }

        [Fact]
        public void HasBusinessException_WithoutStoredException_ReturnsFalse()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var hasException = httpContext.HasBusinessException();

            // Assert
            Assert.False(hasException);
        }

        [Fact]
        public void GetBusinessException_WithValidException_ReturnsException()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test error", null, "TEST_CODE");
            httpContext.SetBusinessExceptionContext(exception);

            // Act
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void GetBusinessException_WithNoException_ReturnsNull()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            // Act
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.Null(retrieved);
        }

        [Fact]
        public void SetBusinessException_OverwritesPreviousValue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception1 = new BusinessException("Error 1", null, "ERROR_1");
            var exception2 = new BusinessException("Error 2", null, "ERROR_2");

            // Act
            httpContext.SetBusinessExceptionContext(exception1);
            var first = httpContext.GetBusinessException();
            httpContext.SetBusinessExceptionContext(exception2);
            var second = httpContext.GetBusinessException();

            // Assert
            Assert.Equal("ERROR_1", first.Code);
            Assert.Equal("ERROR_2", second.Code);
        }

        [Fact]
        public void SetBusinessException_WithDifferentMessages_PreservesAll()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Detailed message", null, "CODE_123");

            // Act
            httpContext.SetBusinessExceptionContext(exception);
            var retrieved = httpContext.GetBusinessException();

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
            var exception1 = new BusinessException("Error 1", null, "ERROR_1");
            var exception2 = new BusinessException("Error 2", null, "ERROR_2");

            // Act
            context1.SetBusinessExceptionContext(exception1);
            context2.SetBusinessExceptionContext(exception2);

            // Assert
            Assert.Equal("ERROR_1", context1.GetBusinessException().Code);
            Assert.Equal("ERROR_2", context2.GetBusinessException().Code);
        }

        [Fact]
        public void HasBusinessException_AfterSet_ReturnsTrue()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test error", null, "TEST_CODE");

            // Act
            Assert.False(httpContext.HasBusinessException());
            httpContext.SetBusinessExceptionContext(exception);

            // Assert
            Assert.True(httpContext.HasBusinessException());
        }

        [Fact]
        public void BusinessException_WithNullMessage_HandledGracefully()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException(null, null, "TEST_CODE");

            // Act
            httpContext.SetBusinessExceptionContext(exception);
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("TEST_CODE", retrieved.Code);
        }

        [Fact]
        public void BusinessException_WithComplexDetails_Preserved()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Error with details", null, "ERROR_WITH_DETAILS");

            // Act
            httpContext.SetBusinessExceptionContext(exception);
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("ERROR_WITH_DETAILS", retrieved.Code);
            Assert.Equal("Error with details", retrieved.Message);
        }

        [Fact]
        public void TryGetBusinessException_OutParameter_SetCorrectly()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test", null, "TEST");
            httpContext.SetBusinessExceptionContext(exception);

            // Act
            httpContext.TryGetBusinessException(out var outException);

            // Assert
            Assert.NotNull(outException);
            Assert.Equal("TEST", outException.Code);
        }

        [Fact]
        public void Extension_ChainedCalls_WorkCorrectly()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test", null, "TEST");

            // Act & Assert - Chain of calls should work
            httpContext.SetBusinessExceptionContext(exception);
            Assert.True(httpContext.HasBusinessException());
            var result = httpContext.TryGetBusinessException(out var retrieved);
            Assert.True(result);
            Assert.Equal("TEST", retrieved.Code);
        }

        [Fact]
        public void GetBusinessException_ConsistentWithTryGet()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test", null, "TEST");
            httpContext.SetBusinessExceptionContext(exception);

            // Act
            httpContext.TryGetBusinessException(out var fromTry);
            var fromGet = httpContext.GetBusinessException();

            // Assert
            Assert.Equal(fromTry.Code, fromGet.Code);
            Assert.Equal(fromTry.Message, fromGet.Message);
        }

        [Fact]
        public void BusinessException_Type_AlwaysPreserved()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var exception = new BusinessException("Test", null, "TEST");

            // Act
            httpContext.SetBusinessExceptionContext(exception);
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.IsType<BusinessException>(retrieved);
        }

        [Fact]
        public void SetAndGet_RoundTrip_Preserves_Equality()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            var originalCode = "ROUND_TRIP_TEST";
            var originalMessage = "Round trip test message";
            var exception = new BusinessException(originalMessage, null, originalCode);

            // Act
            httpContext.SetBusinessExceptionContext(exception);
            var retrieved = httpContext.GetBusinessException();

            // Assert
            Assert.Equal(originalCode, retrieved.Code);
            Assert.Equal(originalMessage, retrieved.Message);
        }
    }
}
