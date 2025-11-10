using MiCake.Core.Util;
using System;
using System.Collections.Generic;
using Xunit;

namespace MiCake.Core.Tests.Util
{
    /// <summary>
    /// Tests for CheckValue utility class
    /// </summary>
    public class CheckValueTests
    {
        #region NotEmpty Tests for Collections

        [Fact]
        public void NotEmpty_WithValidCollection_ShouldReturnCollection()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 1, 2, 3 };

            // Act
            var result = CheckValue.NotEmpty(list, nameof(list));

            // Assert
            Assert.Same(list, result);
        }

        [Fact]
        public void NotEmpty_WithNullCollection_ShouldThrowArgumentNullException()
        {
            // Arrange
            IReadOnlyList<int> list = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CheckValue.NotEmpty(list, nameof(list)));
        }

        [Fact]
        public void NotEmpty_WithEmptyCollection_ShouldThrowArgumentException()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => CheckValue.NotEmpty(list, nameof(list)));
            Assert.Contains("must contain at least one element", exception.Message);
        }

        #endregion

        #region NotEmpty Tests for Strings

        [Fact]
        public void NotEmpty_WithValidString_ShouldReturnString()
        {
            // Arrange
            var str = "test";

            // Act
            var result = CheckValue.NotEmpty(str, nameof(str));

            // Assert
            Assert.Equal(str, result);
        }

        [Fact]
        public void NotEmpty_WithNullString_ShouldThrowArgumentNullException()
        {
            // Arrange
            string str = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CheckValue.NotEmpty(str, nameof(str)));
        }

        [Fact]
        public void NotEmpty_WithEmptyString_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => CheckValue.NotEmpty(str, nameof(str)));
            Assert.Contains("cannot be empty", exception.Message);
        }

        [Fact]
        public void NotEmpty_WithWhitespaceString_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "   ";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => CheckValue.NotEmpty(str, nameof(str)));
            Assert.Contains("cannot be empty", exception.Message);
        }

        #endregion

        #region NotNull Tests

        [Fact]
        public void NotNull_WithValidValue_ShouldReturnValue()
        {
            // Arrange
            var obj = new object();

            // Act
            var result = CheckValue.NotNull(obj, nameof(obj));

            // Assert
            Assert.Same(obj, result);
        }

        [Fact]
        public void NotNull_WithNullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            object obj = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CheckValue.NotNull(obj, nameof(obj)));
        }

        [Fact]
        public void NotNull_WithCustomMessage_ShouldThrowWithMessage()
        {
            // Arrange
            object obj = null;
            var customMessage = "Custom error message";

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => 
                CheckValue.NotNull(obj, nameof(obj), customMessage));
            Assert.Contains(customMessage, exception.Message);
        }

        #endregion

        #region NotNull String with Length Tests

        [Fact]
        public void NotNull_StringWithValidLength_ShouldReturnString()
        {
            // Arrange
            var str = "test";

            // Act
            var result = CheckValue.NotNull(str, nameof(str), maxLength: 10, minLength: 1);

            // Assert
            Assert.Equal(str, result);
        }

        [Fact]
        public void NotNull_StringWithNullValue_ShouldThrowArgumentNullException()
        {
            // Arrange
            string str = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => CheckValue.NotNull(str, nameof(str)));
        }

        [Fact]
        public void NotNull_StringExceedsMaxLength_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "toolongstring";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.NotNull(str, nameof(str), maxLength: 5));
            Assert.Contains("must be equal to or lower than 5", exception.Message);
        }

        [Fact]
        public void NotNull_StringBelowMinLength_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "ab";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.NotNull(str, nameof(str), minLength: 5));
            Assert.Contains("must be equal to or bigger than 5", exception.Message);
        }

        #endregion

        #region NotNullOrWhiteSpace Tests

        [Fact]
        public void NotNullOrWhiteSpace_WithValidString_ShouldReturnString()
        {
            // Arrange
            var str = "test";

            // Act
            var result = CheckValue.NotNullOrWhiteSpace(str, nameof(str));

            // Assert
            Assert.Equal(str, result);
        }

        [Fact]
        public void NotNullOrWhiteSpace_WithNullString_ShouldThrowArgumentException()
        {
            // Arrange
            string str = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.NotNullOrWhiteSpace(str, nameof(str)));
            Assert.Contains("can not be null, empty or white space", exception.Message);
        }

        [Fact]
        public void NotNullOrWhiteSpace_WithWhitespaceString_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "   ";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.NotNullOrWhiteSpace(str, nameof(str)));
            Assert.Contains("can not be null, empty or white space", exception.Message);
        }

        [Fact]
        public void NotNullOrWhiteSpace_WithEmptyString_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.NotNullOrWhiteSpace(str, nameof(str)));
            Assert.Contains("can not be null, empty or white space", exception.Message);
        }

        #endregion

        #region NotNullOrEmpty Tests

        [Fact]
        public void NotNullOrEmpty_WithValidString_ShouldReturnString()
        {
            // Arrange
            var str = "test";

            // Act
            var result = CheckValue.NotNullOrEmpty(str, nameof(str));

            // Assert
            Assert.Equal(str, result);
        }

        [Fact]
        public void NotNullOrEmpty_WithNullString_ShouldThrowArgumentException()
        {
            // Arrange
            string str = null;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.NotNullOrEmpty(str, nameof(str)));
            Assert.Contains("can not be null or empty", exception.Message);
        }

        [Fact]
        public void NotNullOrEmpty_WithEmptyString_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.NotNullOrEmpty(str, nameof(str)));
            Assert.Contains("can not be null or empty", exception.Message);
        }

        [Fact]
        public void NotNullOrEmpty_Collection_WithValidCollection_ShouldReturnCollection()
        {
            // Arrange
            ICollection<int> collection = new List<int> { 1, 2, 3 };

            // Act
            var result = CheckValue.NotNullOrEmpty(collection, nameof(collection));

            // Assert
            Assert.Same(collection, result);
        }

        [Fact]
        public void NotNullOrEmpty_Collection_WithNullCollection_ShouldThrowArgumentException()
        {
            // Arrange
            ICollection<int> collection = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CheckValue.NotNullOrEmpty(collection, nameof(collection)));
        }

        [Fact]
        public void NotNullOrEmpty_Collection_WithEmptyCollection_ShouldThrowArgumentException()
        {
            // Arrange
            ICollection<int> collection = new List<int>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => CheckValue.NotNullOrEmpty(collection, nameof(collection)));
        }

        #endregion

        #region Length Tests

        [Fact]
        public void Length_WithValidString_ShouldReturnString()
        {
            // Arrange
            var str = "test";

            // Act
            var result = CheckValue.Length(str, nameof(str), maxLength: 10, minLength: 1);

            // Assert
            Assert.Equal(str, result);
        }

        [Fact]
        public void Length_WithNullAndNoMinLength_ShouldReturnNull()
        {
            // Arrange
            string str = null;

            // Act
            var result = CheckValue.Length(str, nameof(str), maxLength: 10, minLength: 0);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Length_WithNullAndMinLength_ShouldThrowArgumentException()
        {
            // Arrange
            string str = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                CheckValue.Length(str, nameof(str), maxLength: 10, minLength: 1));
        }

        [Fact]
        public void Length_ExceedsMaxLength_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "toolongstring";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.Length(str, nameof(str), maxLength: 5));
            Assert.Contains("must be equal to or lower than 5", exception.Message);
        }

        [Fact]
        public void Length_BelowMinLength_ShouldThrowArgumentException()
        {
            // Arrange
            var str = "ab";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                CheckValue.Length(str, nameof(str), maxLength: 10, minLength: 5));
            Assert.Contains("must be equal to or bigger than 5", exception.Message);
        }

        #endregion
    }
}
