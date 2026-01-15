using MiCake.AspNetCore.ApiLogging;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for API Logging attributes:
    /// - <see cref="SkipApiLoggingAttribute"/>
    /// - <see cref="AlwaysLogAttribute"/>
    /// - <see cref="LogFullResponseAttribute"/>
    /// </summary>
    public class ApiLoggingAttributes_Tests
    {
        #region SkipApiLoggingAttribute Tests

        [Fact]
        public void SkipApiLoggingAttribute_CanBeAppliedToMethod()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.SkippedAction));
            var attribute = method?.GetCustomAttribute<SkipApiLoggingAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void SkipApiLoggingAttribute_CanBeAppliedToClass()
        {
            // Arrange & Act
            var attribute = typeof(SkippedController).GetCustomAttribute<SkipApiLoggingAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void SkipApiLoggingAttribute_NotPresentOnNormalMethod()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.NormalAction));
            var attribute = method?.GetCustomAttribute<SkipApiLoggingAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void SkipApiLoggingAttribute_CanBeInheritedFromBaseController()
        {
            // Arrange & Act
            var attribute = typeof(DerivedSkippedController)
                .GetCustomAttribute<SkipApiLoggingAttribute>(inherit: true);

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void SkipApiLoggingAttribute_NotAllowMultiple()
        {
            // Arrange & Act
            var attrUsage = typeof(SkipApiLoggingAttribute).GetCustomAttribute<System.AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attrUsage);
            Assert.False(attrUsage.AllowMultiple);
        }

        [Fact]
        public void SkipApiLoggingAttribute_ValidTargets()
        {
            // Arrange & Act
            var attrUsage = typeof(SkipApiLoggingAttribute).GetCustomAttribute<System.AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attrUsage);
            Assert.True(attrUsage.ValidOn.HasFlag(System.AttributeTargets.Class));
            Assert.True(attrUsage.ValidOn.HasFlag(System.AttributeTargets.Method));
        }

        #endregion

        #region AlwaysLogAttribute Tests

        [Fact]
        public void AlwaysLogAttribute_CanBeAppliedToMethod()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.AlwaysLoggedAction));
            var attribute = method?.GetCustomAttribute<AlwaysLogAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void AlwaysLogAttribute_NotPresentOnNormalMethod()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.NormalAction));
            var attribute = method?.GetCustomAttribute<AlwaysLogAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void AlwaysLogAttribute_NotAllowMultiple()
        {
            // Arrange & Act
            var attrUsage = typeof(AlwaysLogAttribute).GetCustomAttribute<System.AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attrUsage);
            Assert.False(attrUsage.AllowMultiple);
        }

        [Fact]
        public void AlwaysLogAttribute_ValidTargets()
        {
            // Arrange & Act
            var attrUsage = typeof(AlwaysLogAttribute).GetCustomAttribute<System.AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attrUsage);
            Assert.True(attrUsage.ValidOn.HasFlag(System.AttributeTargets.Method));
        }

        #endregion

        #region LogFullResponseAttribute Tests

        [Fact]
        public void LogFullResponseAttribute_CanBeAppliedToMethod()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.FullLogAction));
            var attribute = method?.GetCustomAttribute<LogFullResponseAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void LogFullResponseAttribute_DefaultMaxSize_IsZero()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.FullLogAction));
            var attribute = method?.GetCustomAttribute<LogFullResponseAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(0, attribute.MaxSize);
        }

        [Fact]
        public void LogFullResponseAttribute_CanSetMaxSize()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.FullLogWithSizeAction));
            var attribute = method?.GetCustomAttribute<LogFullResponseAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.Equal(10000, attribute.MaxSize);
        }

        [Fact]
        public void LogFullResponseAttribute_NotPresentOnNormalMethod()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.NormalAction));
            var attribute = method?.GetCustomAttribute<LogFullResponseAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void LogFullResponseAttribute_NotAllowMultiple()
        {
            // Arrange & Act
            var attrUsage = typeof(LogFullResponseAttribute).GetCustomAttribute<System.AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attrUsage);
            Assert.False(attrUsage.AllowMultiple);
        }

        [Fact]
        public void LogFullResponseAttribute_ValidTargets()
        {
            // Arrange & Act
            var attrUsage = typeof(LogFullResponseAttribute).GetCustomAttribute<System.AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attrUsage);
            Assert.True(attrUsage.ValidOn.HasFlag(System.AttributeTargets.Method));
        }

        #endregion

        #region Combination Tests

        [Fact]
        public void Attributes_CanCombineAlwaysLogAndLogFullResponse()
        {
            // Arrange & Act
            var method = typeof(TestController).GetMethod(nameof(TestController.CombinedAttributesAction));
            var alwaysLog = method?.GetCustomAttribute<AlwaysLogAttribute>();
            var fullResponse = method?.GetCustomAttribute<LogFullResponseAttribute>();

            // Assert
            Assert.NotNull(alwaysLog);
            Assert.NotNull(fullResponse);
        }

        [Fact]
        public void Attributes_SkipAndAlwaysLogOnSameMethod_BothDetectable()
        {
            // NOTE: This is technically a conflicting configuration, but we test that both can be detected
            // The filter should handle the priority (Skip takes precedence)
            var method = typeof(ConflictingController).GetMethod(nameof(ConflictingController.ConflictingAction));
            var skip = method?.GetCustomAttribute<SkipApiLoggingAttribute>();
            var alwaysLog = method?.GetCustomAttribute<AlwaysLogAttribute>();

            // Both attributes can be present (though it's a misconfiguration)
            Assert.NotNull(skip);
            Assert.NotNull(alwaysLog);
        }

        #endregion

        #region Test Controllers

        /// <summary>
        /// Test controller with various attribute configurations
        /// </summary>
        public class TestController : ControllerBase
        {
            [SkipApiLogging]
            public IActionResult SkippedAction() => Ok();

            [AlwaysLog]
            public IActionResult AlwaysLoggedAction() => Ok();

            [LogFullResponse]
            public IActionResult FullLogAction() => Ok();

            [LogFullResponse(MaxSize = 10000)]
            public IActionResult FullLogWithSizeAction() => Ok();

            [AlwaysLog]
            [LogFullResponse]
            public IActionResult CombinedAttributesAction() => Ok();

            public IActionResult NormalAction() => Ok();
        }

        /// <summary>
        /// Controller with SkipApiLogging at controller level
        /// </summary>
        [SkipApiLogging]
        public class SkippedController : ControllerBase
        {
            public IActionResult SomeAction() => Ok();
        }

        /// <summary>
        /// Base controller with SkipApiLogging
        /// </summary>
        [SkipApiLogging]
        public class BaseSkippedController : ControllerBase
        {
        }

        /// <summary>
        /// Derived controller inheriting SkipApiLogging
        /// </summary>
        public class DerivedSkippedController : BaseSkippedController
        {
            public IActionResult ChildAction() => Ok();
        }

        /// <summary>
        /// Controller with conflicting attributes (for testing purposes only)
        /// </summary>
        public class ConflictingController : ControllerBase
        {
            [SkipApiLogging]
            [AlwaysLog]
            public IActionResult ConflictingAction() => Ok();
        }

        #endregion
    }
}
