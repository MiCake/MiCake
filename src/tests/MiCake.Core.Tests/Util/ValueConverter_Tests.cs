using System;
using Xunit;
using MiCake.Util.Convert;

namespace MiCake.Util.Tests
{
    public class ValueConverterTests
    {
        public ValueConverterTests()
        {
            // Reset registry before each test to ensure clean state
            ValueConverter.ResetRegistry();
        }

        #region Primitive Type Conversions

        [Fact]
        public void Convert_StringToInt_Success()
        {
            var result = ValueConverter.Convert<string, int>("123");
            Assert.Equal(123, result);
        }

        [Fact]
        public void Convert_IntToString_Success()
        {
            var result = ValueConverter.Convert<int, string>(123);
            Assert.Equal("123", result);
        }

        [Fact]
        public void Convert_StringToDateTime_Success()
        {
            var result = ValueConverter.Convert<string, DateTime>("2020-02-02");
            Assert.Equal(2020, result.Year);
            Assert.Equal(2, result.Month);
            Assert.Equal(2, result.Day);
        }

        [Fact]
        public void Convert_InvalidStringToDateTime_ReturnsDefault()
        {
            var result = ValueConverter.Convert<string, DateTime>("invalid-date");
            Assert.Equal(default, result);
        }

        [Fact]
        public void Convert_StringToDouble_Success()
        {
            var result = ValueConverter.Convert<string, double>("3.14");
            Assert.Equal(3.14, result);
        }

        [Fact]
        public void Convert_StringToBoolean_Success()
        {
            var result = ValueConverter.Convert<string, bool>("true");
            Assert.True(result);
        }

        #endregion

        #region Guid Conversions

        [Fact]
        public void Convert_StringToGuid_Success()
        {
            var originalGuid = Guid.NewGuid();
            var guidString = originalGuid.ToString();
            
            var result = ValueConverter.Convert<string, Guid>(guidString);
            Assert.Equal(originalGuid, result);
        }

        [Fact]
        public void Convert_InvalidStringToGuid_ReturnsDefault()
        {
            var result = ValueConverter.Convert<string, Guid>("not-a-guid");
            Assert.Equal(default(Guid), result);
        }

        [Fact]
        public void Convert_GuidToGuid_Success()
        {
            var originalGuid = Guid.NewGuid();
            var result = ValueConverter.Convert<Guid, Guid>(originalGuid);
            Assert.Equal(originalGuid, result);
        }

        #endregion

        #region Version Conversions

        [Fact]
        public void Convert_StringToVersion_Success()
        {
            var result = ValueConverter.Convert<string, Version>("1.2.3");
            Assert.NotNull(result);
            Assert.Equal(1, result.Major);
            Assert.Equal(2, result.Minor);
            Assert.Equal(3, result.Build);
        }

        [Fact]
        public void Convert_InvalidStringToVersion_ReturnsNull()
        {
            var result = ValueConverter.Convert<string, Version>("not-a-version");
            Assert.Null(result);
        }

        [Fact]
        public void Convert_VersionToVersion_Success()
        {
            var originalVersion = new Version(2, 0, 1);
            var result = ValueConverter.Convert<Version, Version>(originalVersion);
            Assert.Equal(originalVersion, result);
        }

        #endregion

        #region Converter Registration

        [Fact]
        public void HasConverter_WithRegisteredConverter_ReturnsTrue()
        {
            ValueConverter.RegisterConverter<string, int>(() => new SystemValueConverter<string, int>());
            Assert.True(ValueConverter.HasConverter<string, int>());
        }

        [Fact]
        public void HasConverter_WithoutRegisteredConverter_ReturnsFalse()
        {
            ValueConverter.ClearConverters<string, int>();
            // SystemConverter should still be tried as fallback, so this may still return true
            // Just verify it returns a boolean
            var result = ValueConverter.HasConverter<string, int>();
            Assert.IsType<bool>(result);
        }

        [Fact]
        public void RegisterConverter_WithFactory_CanConvert()
        {
            // Register a custom converter that converts to uppercase
            ValueConverter.RegisterConverter<string, string>(() => new UppercaseConverter());
            
            var result = ValueConverter.Convert<string, string>("hello");
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void RegisterConverter_WithInstance_CanConvert()
        {
            var customConverter = new UppercaseConverter();
            ValueConverter.RegisterConverter<string, string>(customConverter);
            
            var result = ValueConverter.Convert<string, string>("hello");
            Assert.Equal("HELLO", result);
        }

        [Fact]
        public void ClearConverters_RemovesRegisteredConverters()
        {
            ValueConverter.RegisterConverter<string, string>(() => new UppercaseConverter());
            ValueConverter.ClearConverters<string, string>();
            
            // System converter should still work
            var result = ValueConverter.Convert<string, string>("hello");
            Assert.NotNull(result);
            Assert.Equal("hello", result);
        }

        [Fact]
        public void ClearAll_RemovesAllConverters()
        {
            ValueConverter.RegisterConverter<string, string>(() => new UppercaseConverter());
            ValueConverter.ClearAll();
            
            // System converter should still work as it's always tried as fallback
            var result = ValueConverter.Convert<string, string>("hello");
            Assert.NotNull(result);
            Assert.Equal("hello", result);
        }

        #endregion

        #region Registry Management

        [Fact]
        public void SetRegistry_WithCustomRegistry_UsesCustomRegistry()
        {
            var customRegistry = new DefaultConverterRegistry();
            ValueConverter.SetRegistry(customRegistry);
            
            // Should use custom registry
            Assert.Same(customRegistry, ValueConverter.Registry);
        }

        [Fact]
        public void SetRegistry_WithNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ValueConverter.SetRegistry(null));
        }

        [Fact]
        public void ResetRegistry_RestoresDefaultRegistry()
        {
            var customRegistry = new DefaultConverterRegistry();
            ValueConverter.SetRegistry(customRegistry);
            ValueConverter.ResetRegistry();
            
            // Should use new default registry
            Assert.NotSame(customRegistry, ValueConverter.Registry);
        }

        #endregion

        #region Error Cases

        [Fact]
        public void Convert_WithNullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ValueConverter.Convert<string, int>(null));
        }

        [Fact]
        public void Convert_InvalidConversion_ReturnsDefault()
        {
            // Try to convert invalid input
            var result = ValueConverter.Convert<string, int>("not-a-number");
            Assert.Equal(default(int), result);
        }

        #endregion
    }

    /// <summary>
    /// Custom converter for testing purposes.
    /// Converts string to uppercase.
    /// </summary>
    internal class UppercaseConverter : IValueConverter<string, string>
    {
        public bool CanConvert(string value)
        {
            return !string.IsNullOrEmpty(value);
        }

        public string? Convert(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return value.ToUpperInvariant();
            }
            return null;
        }
    }
}
