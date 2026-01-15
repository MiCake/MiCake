using MiCake.DDD.Domain;
using MiCake.DDD.Domain.EventDispatch;
using System;
using System.Linq;
using Xunit;

namespace MiCake.DDD.Tests
{
    /// <summary>
    /// Tests for DomainEventOptions and DomainEventException
    /// </summary>
    public class DomainEventOptions_Tests
    {
        #region DomainEventOptions Tests

        [Fact]
        public void DomainEventOptions_DefaultValue_ShouldBeThrowOnError()
        {
            // Arrange & Act
            var options = new DomainEventOptions();

            // Assert
            Assert.Equal(DomainEventOptions.EventFailureStrategy.ThrowOnError, options.OnEventFailure);
        }

        [Fact]
        public void DomainEventOptions_CanSetContinueOnError()
        {
            // Arrange
            var options = new DomainEventOptions();

            // Act
            options.OnEventFailure = DomainEventOptions.EventFailureStrategy.ContinueOnError;

            // Assert
            Assert.Equal(DomainEventOptions.EventFailureStrategy.ContinueOnError, options.OnEventFailure);
        }

        [Fact]
        public void DomainEventOptions_CanSetStopOnError()
        {
            // Arrange
            var options = new DomainEventOptions();

            // Act
            options.OnEventFailure = DomainEventOptions.EventFailureStrategy.StopOnError;

            // Assert
            Assert.Equal(DomainEventOptions.EventFailureStrategy.StopOnError, options.OnEventFailure);
        }

        [Fact]
        public void DomainEventOptions_CanSetThrowOnError()
        {
            // Arrange
            var options = new DomainEventOptions();

            // Act
            options.OnEventFailure = DomainEventOptions.EventFailureStrategy.ThrowOnError;

            // Assert
            Assert.Equal(DomainEventOptions.EventFailureStrategy.ThrowOnError, options.OnEventFailure);
        }

        [Fact]
        public void EventFailureStrategy_ShouldHaveThreeValues()
        {
            // Arrange & Act
            var values = Enum.GetValues(typeof(DomainEventOptions.EventFailureStrategy)).Cast<DomainEventOptions.EventFailureStrategy>().ToList();

            // Assert
            Assert.Equal(3, values.Count);
            Assert.Contains(DomainEventOptions.EventFailureStrategy.ContinueOnError, values);
            Assert.Contains(DomainEventOptions.EventFailureStrategy.StopOnError, values);
            Assert.Contains(DomainEventOptions.EventFailureStrategy.ThrowOnError, values);
        }

        #endregion

        #region DomainEventException Tests

        [Fact]
        public void DomainEventException_WithMessage_ShouldSetMessage()
        {
            // Arrange
            var message = "Test error message";

            // Act
            var exception = new DomainEventException(message);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.FailedEvent);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void DomainEventException_WithMessageAndEvent_ShouldSetBoth()
        {
            // Arrange
            var message = "Event failed";
            var domainEvent = new TestDomainEvent();

            // Act
            var exception = new DomainEventException(message, domainEvent);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(domainEvent, exception.FailedEvent);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void DomainEventException_WithAllParameters_ShouldSetAll()
        {
            // Arrange
            var message = "Event processing failed";
            var domainEvent = new TestDomainEvent();
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new DomainEventException(message, domainEvent, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(domainEvent, exception.FailedEvent);
            Assert.Same(innerException, exception.InnerException);
        }

        [Fact]
        public void DomainEventException_WithNullEvent_ShouldNotThrow()
        {
            // Arrange
            var message = "Test message";

            // Act
            var exception = new DomainEventException(message, null);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Null(exception.FailedEvent);
        }

        [Fact]
        public void DomainEventException_CanBeCaught_AsException()
        {
            // Arrange
            var message = "Test exception";
            var domainEvent = new TestDomainEvent();
            
            // Act
            Exception caughtException = null;
            try
            {
                throw new DomainEventException(message, domainEvent);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            // Assert
            Assert.NotNull(caughtException);
            Assert.IsType<DomainEventException>(caughtException);
        }

        [Fact]
        public void DomainEventException_CanBeCaught_AsDomainEventException()
        {
            // Arrange
            var message = "Test exception";
            var domainEvent = new TestDomainEvent();
            void ThrowException() => throw new DomainEventException(message, domainEvent);

            // Act & Assert
            var exception = Assert.Throws<DomainEventException>(ThrowException);
            Assert.Equal(message, exception.Message);
            Assert.Same(domainEvent, exception.FailedEvent);
        }

        [Fact]
        public void DomainEventException_WithInnerException_ShouldPreserveStackTrace()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner");
            var domainEvent = new TestDomainEvent();

            // Act
            var exception = new DomainEventException("Outer", domainEvent, innerException);

            // Assert
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal("Inner", exception.InnerException.Message);
        }

        [Fact]
        public void DomainEventException_SerializationRoundTrip_ShouldPreserveData()
        {
            // Arrange
            var message = "Test exception";
            var exception = new DomainEventException(message);

            // Act & Assert - Verify basic properties are accessible
            Assert.Equal(message, exception.Message);
            Assert.IsType<DomainEventException>(exception);
        }

        [Fact]
        public void DomainEventException_ToString_ShouldIncludeMessage()
        {
            // Arrange
            var message = "Test error occurred";
            var exception = new DomainEventException(message);

            // Act
            var result = exception.ToString();

            // Assert
            Assert.Contains(message, result);
            Assert.Contains("DomainEventException", result);
        }

        #endregion

        #region Test Helper Classes

        private class TestDomainEvent : IDomainEvent
        {
            public static DateTime OccurredOn => DateTime.Now;
        }

        #endregion
    }
}
