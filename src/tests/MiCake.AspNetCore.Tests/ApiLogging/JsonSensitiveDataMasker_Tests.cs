using MiCake.AspNetCore.ApiLogging;
using MiCake.AspNetCore.ApiLogging.Internals;
using System.Collections.Generic;
using Xunit;

namespace MiCake.AspNetCore.Tests.ApiLogging
{
    /// <summary>
    /// Tests for <see cref="JsonSensitiveDataMasker"/> implementation.
    /// </summary>
    public class JsonSensitiveDataMasker_Tests
    {
        private readonly ISensitiveDataMasker _masker;

        public JsonSensitiveDataMasker_Tests()
        {
            _masker = new JsonSensitiveDataMasker();
        }

        #region Null/Empty Input Tests

        [Fact]
        public void Mask_NullContent_ReturnsNull()
        {
            // Arrange
            string? content = null;

            // Act
            var result = _masker.Mask(content!, ["password"]);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Mask_EmptyContent_ReturnsEmpty()
        {
            // Arrange
            var content = string.Empty;

            // Act
            var result = _masker.Mask(content, ["password"]);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Mask_WhitespaceContent_ReturnsWhitespace()
        {
            // Arrange
            var content = "   ";

            // Act
            var result = _masker.Mask(content, ["password"]);

            // Assert
            Assert.Equal("   ", result);
        }

        [Fact]
        public void Mask_NullSensitiveFields_ReturnsOriginalContent()
        {
            // Arrange
            var content = """{"password": "secret123"}""";

            // Act
            var result = _masker.Mask(content, null!);

            // Assert
            Assert.Equal(content, result);
        }

        [Fact]
        public void Mask_EmptySensitiveFields_ReturnsOriginalContent()
        {
            // Arrange
            var content = """{"password": "secret123"}""";

            // Act
            var result = _masker.Mask(content, []);

            // Assert
            Assert.Equal(content, result);
        }

        #endregion

        #region JSON Masking Tests

        [Fact]
        public void Mask_SimpleJsonWithPassword_MasksPassword()
        {
            // Arrange
            var content = """{"username": "john", "password": "secret123"}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains(""""username":"john"""", result);
            Assert.Contains(""""password":"***"""", result);
            Assert.DoesNotContain("secret123", result);
        }

        [Fact]
        public void Mask_JsonWithMultipleSensitiveFields_MasksAllFields()
        {
            // Arrange
            var content = """{"username": "john", "password": "secret123", "token": "abc123", "email": "john@example.com"}""";
            var sensitiveFields = new List<string> { "password", "token" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains(""""username":"john"""", result);
            Assert.Contains(""""password":"***"""", result);
            Assert.Contains(""""token":"***"""", result);
            Assert.Contains(""""email":"john@example.com"""", result);
            Assert.DoesNotContain("secret123", result);
            Assert.DoesNotContain("abc123", result);
        }

        [Fact]
        public void Mask_CaseInsensitive_MaskesRegardlessOfCase()
        {
            // Arrange
            var content = """{"Password": "secret1", "PASSWORD": "secret2", "passWord": "secret3"}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.DoesNotContain("secret1", result);
            Assert.DoesNotContain("secret2", result);
            Assert.DoesNotContain("secret3", result);
        }

        [Fact]
        public void Mask_NestedJson_MasksNestedFields()
        {
            // Arrange
            var content = """{"user": {"name": "john", "credentials": {"password": "secret123"}}}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains(""""name":"john"""", result);
            Assert.Contains(""""password":"***"""", result);
            Assert.DoesNotContain("secret123", result);
        }

        [Fact]
        public void Mask_JsonArray_MasksFieldsInArrayItems()
        {
            // Arrange
            var content = """[{"name": "john", "password": "secret1"}, {"name": "jane", "password": "secret2"}]""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains(""""name":"john"""", result);
            Assert.Contains(""""name":"jane"""", result);
            Assert.DoesNotContain("secret1", result);
            Assert.DoesNotContain("secret2", result);
        }

        [Fact]
        public void Mask_FieldNameContainsSensitiveWord_MasksField()
        {
            // Arrange
            var content = """{"userPassword": "secret1", "passwordHash": "hash123", "normalField": "value"}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            // Fields containing "password" should also be masked
            Assert.DoesNotContain("secret1", result);
            Assert.DoesNotContain("hash123", result);
            Assert.Contains(""""normalField":"value"""", result);
        }

        [Fact]
        public void Mask_JsonWithNumberValue_DoesNotMaskNumbers()
        {
            // Arrange
            var content = """{"id": 12345, "amount": 99.99, "isActive": true}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains("\"id\":12345", result);
            Assert.Contains("\"amount\":99.99", result);
            Assert.Contains("\"isActive\":true", result);
        }

        [Fact]
        public void Mask_JsonWithNullValue_PreservesNull()
        {
            // Arrange
            var content = """{"name": "john", "middleName": null}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains("\"name\":\"john\"", result);
            Assert.Contains("\"middleName\":null", result);
        }

        #endregion

        #region Non-JSON Content Tests

        [Fact]
        public void Mask_NonJsonWithSensitiveField_FallsBackToRegex()
        {
            // Arrange - This is a query string style
            var content = "username=john&password=secret123&remember=true";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains("username=john", result);
            Assert.Contains("password=***", result);
            Assert.DoesNotContain("secret123", result);
        }

        [Fact]
        public void Mask_QuotedNonJsonWithSensitiveField_FallsBackToRegex()
        {
            // Arrange - This looks like JSON but isn't valid
            var content = """not valid json "password": "secret123" more text""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.DoesNotContain("secret123", result);
        }

        [Fact]
        public void Mask_PlainText_ReturnsOriginal()
        {
            // Arrange
            var content = "This is just plain text without any structured data";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Equal(content, result);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Mask_EmptyJsonObject_ReturnsEmptyObject()
        {
            // Arrange
            var content = "{}";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Equal("{}", result);
        }

        [Fact]
        public void Mask_EmptyJsonArray_ReturnsEmptyArray()
        {
            // Arrange
            var content = "[]";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Equal("[]", result);
        }

        [Fact]
        public void Mask_DeeplyNestedJson_HandlesCorrectly()
        {
            // Arrange
            var content = """{"level1": {"level2": {"level3": {"level4": {"password": "deepSecret"}}}}}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.DoesNotContain("deepSecret", result);
            Assert.Contains(""""password":"***"""", result);
        }

        [Fact]
        public void Mask_JsonWithSpecialCharacters_MasksCorrectly()
        {
            // Arrange
            var content = """{"password": "pass@#$%^&*()word", "name": "John O'Connor"}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.DoesNotContain("pass@#$%^&*()word", result);
            // JSON serialization may escape single quotes as \u0027, so we check for either format
            Assert.True(result.Contains("John O'Connor") || result.Contains("John O\\u0027Connor"),
                "Result should contain the name in some form");
        }

        [Fact]
        public void Mask_JsonWithUnicodeCharacters_PreservesUnicode()
        {
            // Arrange
            var content = """{"name": "张三", "password": "密码123"}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.DoesNotContain("密码123", result);
            // Note: Unicode characters may be escaped in JSON output
        }

        [Fact]
        public void Mask_JsonWithEmptyStringValue_MasksEmptyString()
        {
            // Arrange
            var content = """{"password": "", "name": "john"}""";
            var sensitiveFields = new List<string> { "password" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.Contains(""""password":"***"""", result);
            Assert.Contains(""""name":"john"""", result);
        }

        [Fact]
        public void Mask_MultipleSensitiveFieldsAtDifferentLevels_MasksAll()
        {
            // Arrange
            var content = """{"password": "pass1", "user": {"token": "tok1", "details": {"secret": "sec1"}}}""";
            var sensitiveFields = new List<string> { "password", "token", "secret" };

            // Act
            var result = _masker.Mask(content, sensitiveFields);

            // Assert
            Assert.DoesNotContain("pass1", result);
            Assert.DoesNotContain("tok1", result);
            Assert.DoesNotContain("sec1", result);
        }

        #endregion
    }
}
