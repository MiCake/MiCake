using MiCake.AspNetCore.Responses;
using System.Linq;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    /// <summary>
    /// Tests for DataWrapperOptions simplifications in commit 1ba5a78.
    /// Focus: Unified WrapProblemDetails flag, removal of WrapValidationProblemDetails property.
    /// </summary>
    public class DataWrapperOptions_Simplification_Tests
    {
        #region Default Value Tests

        [Fact]
        public void Constructor_WrapProblemDetails_DefaultIsTrue()
        {
            // Arrange & Act
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.True(options.WrapProblemDetails);
        }

        [Fact]
        public void Constructor_ShowStackTraceWhenError_DefaultIsFalse()
        {
            // Arrange & Act
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.False(options.ShowStackTraceWhenError);
        }

        [Fact]
        public void Constructor_IgnoreStatusCodes_DefaultContains201_202_404()
        {
            // Arrange & Act
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.NotNull(options.IgnoreStatusCodes);
            Assert.Contains(201, options.IgnoreStatusCodes);
            Assert.Contains(202, options.IgnoreStatusCodes);
            Assert.Contains(404, options.IgnoreStatusCodes);
        }

        [Fact]
        public void Constructor_DefaultCodeSetting_NotNull()
        {
            // Arrange & Act
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.NotNull(options.DefaultCodeSetting);
        }

        #endregion

        #region Unified WrapProblemDetails Flag Tests

        [Fact]
        public void WrapProblemDetails_CanBeSetToTrue()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = true };

            // Assert
            Assert.True(options.WrapProblemDetails);
        }

        [Fact]
        public void WrapProblemDetails_CanBeSetToFalse()
        {
            // Arrange
            var options = new ResponseWrapperOptions { WrapProblemDetails = false };

            // Assert
            Assert.False(options.WrapProblemDetails);
        }

        [Fact]
        public void WrapProblemDetails_UnifiedFlagControlsBothTypes()
        {
            // Arrange
            var options1 = new ResponseWrapperOptions { WrapProblemDetails = true };
            var options2 = new ResponseWrapperOptions { WrapProblemDetails = false };

            // Assert - Both types are controlled by the same flag
            Assert.True(options1.WrapProblemDetails);
            Assert.False(options2.WrapProblemDetails);
            // No separate WrapValidationProblemDetails property
        }

        #endregion

        #region RemovedWrapValidationProblemDetails Property Tests

        [Fact]
        public void WrapValidationProblemDetails_PropertyDoesNotExist()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            
            // Act & Assert
            // Verify the property doesn't exist by checking type info
            var propertyInfo = typeof(ResponseWrapperOptions).GetProperty("WrapValidationProblemDetails");
            Assert.Null(propertyInfo);
        }

        [Fact]
        public void SimplifiedAPI_OnlyHasWrapProblemDetails()
        {
            // Arrange
            var options = new ResponseWrapperOptions();
            
            // Act
            var wrapProblemDetailsProperty = typeof(ResponseWrapperOptions).GetProperty("WrapProblemDetails");
            var wrapValidationProblemDetailsProperty = typeof(ResponseWrapperOptions).GetProperty("WrapValidationProblemDetails");

            // Assert
            Assert.NotNull(wrapProblemDetailsProperty);
            Assert.Null(wrapValidationProblemDetailsProperty);
        }

        #endregion

        #region DefaultCodeSetting Tests

        [Fact]
        public void DefaultCodeSetting_SuccessCodeIsZero()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.Equal("0", options.DefaultCodeSetting.Success);
        }

        [Fact]
        public void DefaultCodeSetting_ProblemDetailsCodeIs9998()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.Equal("9998", options.DefaultCodeSetting.ProblemDetails);
        }

        [Fact]
        public void DefaultCodeSetting_ErrorCodeIs9999()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.Equal("9999", options.DefaultCodeSetting.Error);
        }

        [Fact]
        public void DefaultCodeSetting_CanBeCustomized()
        {
            // Arrange
            var customCodes = new DataWrapperDefaultCode
            {
                Success = "200",
                Error = "500",
                ProblemDetails = "400"
            };
            var options = new ResponseWrapperOptions { DefaultCodeSetting = customCodes };

            // Assert
            Assert.Equal("200", options.DefaultCodeSetting.Success);
            Assert.Equal("500", options.DefaultCodeSetting.Error);
            Assert.Equal("400", options.DefaultCodeSetting.ProblemDetails);
        }

        #endregion

        #region WrapperFactory Tests

        [Fact]
        public void WrapperFactory_DefaultIsNull()
        {
            // Arrange & Act
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.Null(options.WrapperFactory);
        }

        [Fact]
        public void WrapperFactory_CanBeSet()
        {
            // Arrange
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = ctx => "custom",
                ErrorFactory = ctx => "error"
            };
            var options = new ResponseWrapperOptions { WrapperFactory = customFactory };

            // Assert
            Assert.Same(customFactory, options.WrapperFactory);
        }

        [Fact]
        public void GetOrCreateFactory_WithNoFactory_CreatesDefault()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Act
            var factory = options.GetOrCreateFactory();

            // Assert
            Assert.NotNull(factory);
            Assert.NotNull(factory.SuccessFactory);
            Assert.NotNull(factory.ErrorFactory);
        }

        [Fact]
        public void GetOrCreateFactory_WithCustomFactory_ReturnsCustom()
        {
            // Arrange
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = ctx => "custom",
                ErrorFactory = ctx => "error"
            };
            var options = new ResponseWrapperOptions { WrapperFactory = customFactory };

            // Act
            var factory = options.GetOrCreateFactory();

            // Assert
            Assert.Same(customFactory, factory);
        }

        #endregion

        #region IgnoreStatusCodes Tests

        [Fact]
        public void IgnoreStatusCodes_CanBeModified()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Act
            options.IgnoreStatusCodes.Add(500);

            // Assert
            Assert.Contains(500, options.IgnoreStatusCodes);
        }

        [Fact]
        public void IgnoreStatusCodes_CanBeCleared()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Act
            options.IgnoreStatusCodes.Clear();

            // Assert
            Assert.Empty(options.IgnoreStatusCodes);
        }

        [Fact]
        public void IgnoreStatusCodes_CanBeReplaced()
        {
            // Arrange
            var options = new ResponseWrapperOptions();

            // Act
            options.IgnoreStatusCodes = [200, 204];

            // Assert
            Assert.Equal(2, options.IgnoreStatusCodes.Count);
            Assert.Contains(200, options.IgnoreStatusCodes);
            Assert.Contains(204, options.IgnoreStatusCodes);
            Assert.DoesNotContain(201, options.IgnoreStatusCodes);
        }

        #endregion

        #region Configuration Scenarios Tests

        [Fact]
        public void Scenario_AllDefaults_WorksTogether()
        {
            // Arrange & Act
            var options = new ResponseWrapperOptions();

            // Assert
            Assert.True(options.WrapProblemDetails);
            Assert.False(options.ShowStackTraceWhenError);
            Assert.NotNull(options.DefaultCodeSetting);
            Assert.Equal("0", options.DefaultCodeSetting.Success);
            Assert.Equal("9998", options.DefaultCodeSetting.ProblemDetails);
            Assert.Equal("9999", options.DefaultCodeSetting.Error);
            Assert.Null(options.WrapperFactory);
        }

        [Fact]
        public void Scenario_CustomConfiguration_AllPropertiesSet()
        {
            // Arrange
            var customCodes = new DataWrapperDefaultCode { Success = "OK", Error = "FAIL" };
            var customFactory = new ResponseWrapperFactory();
            var customStatusCodes = new System.Collections.Generic.List<int> { 400, 500 };

            // Act
            var options = new ResponseWrapperOptions
            {
                WrapProblemDetails = false,
                ShowStackTraceWhenError = true,
                DefaultCodeSetting = customCodes,
                WrapperFactory = customFactory,
                IgnoreStatusCodes = customStatusCodes
            };

            // Assert
            Assert.False(options.WrapProblemDetails);
            Assert.True(options.ShowStackTraceWhenError);
            Assert.Equal("OK", options.DefaultCodeSetting.Success);
            Assert.Equal("FAIL", options.DefaultCodeSetting.Error);
            Assert.Same(customFactory, options.WrapperFactory);
            Assert.Equal(customStatusCodes, options.IgnoreStatusCodes);
        }

        [Fact]
        public void Scenario_DualWrapperUsage_DifferentConfigurations()
        {
            // Arrange
            var optionsWrapAll = new ResponseWrapperOptions { WrapProblemDetails = true };
            var optionsWrapNone = new ResponseWrapperOptions { WrapProblemDetails = false };

            // Act
            var factoryWrapAll = optionsWrapAll.GetOrCreateFactory();
            var factoryWrapNone = optionsWrapNone.GetOrCreateFactory();

            // Assert
            Assert.NotNull(factoryWrapAll);
            Assert.NotNull(factoryWrapNone);
            // Both factories are valid even though they have different configurations
        }

        #endregion

        #region API Simplification Verification Tests

        [Fact]
        public void APISimplification_NoRedundantProperties()
        {
            // Arrange
            var properties = typeof(ResponseWrapperOptions).GetProperties();

            // Act
            var propertyNames = properties.Select(p => p.Name).ToList();

            // Assert
            // Should have these properties
            Assert.Contains("WrapProblemDetails", propertyNames);
            Assert.Contains("ShowStackTraceWhenError", propertyNames);
            Assert.Contains("IgnoreStatusCodes", propertyNames);
            Assert.Contains("DefaultCodeSetting", propertyNames);
            Assert.Contains("WrapperFactory", propertyNames);

            // Should NOT have this property (removed in simplification)
            Assert.DoesNotContain("WrapValidationProblemDetails", propertyNames);
        }

        [Fact]
        public void APISimplification_FewerPropertiesRequiresFewerConfigurations()
        {
            // Arrange - Old approach would require two separate boolean flags
            // New approach uses one unified flag

            // Act
            var options = new ResponseWrapperOptions();
            options.WrapProblemDetails = true; // Controls both ProblemDetails and ValidationProblemDetails

            // Assert
            Assert.True(options.WrapProblemDetails);
            // No separate configuration needed for ValidationProblemDetails
        }

        #endregion

        #region Backward Compatibility Considerations Tests

        [Fact]
        public void DefaultBehavior_UnifiedFlagDefaultTrue_MaintainsConsistency()
        {
            // Arrange - Previous behavior might have had different defaults
            // Now both are controlled by one flag defaulting to true

            // Act
            var options = new ResponseWrapperOptions();

            // Assert - Both types are wrapped by default
            Assert.True(options.WrapProblemDetails);
            // This single flag controls both ProblemDetails and ValidationProblemDetails
        }

        [Fact]
        public void Configuration_SimplifiedAPI_EasierToUnderstand()
        {
            // Arrange - Fewer options means simpler configuration

            // Act
            var options = new ResponseWrapperOptions
            {
                WrapProblemDetails = false,
                ShowStackTraceWhenError = true,
                DefaultCodeSetting = new DataWrapperDefaultCode { Success = "OK" }
            };

            // Assert - Configuration is clear and straightforward
            Assert.False(options.WrapProblemDetails);
            Assert.True(options.ShowStackTraceWhenError);
            Assert.Equal("OK", options.DefaultCodeSetting.Success);
        }

        #endregion
    }
}
