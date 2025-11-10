using MiCake.Core.Util.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MiCake.Core.Tests.Util.Reflection
{
    /// <summary>
    /// Tests for ReflectionHelper utility class
    /// </summary>
    public class ReflectionHelperTests
    {
        #region IsAssignableToGenericType Tests

        [Fact]
        public void IsAssignableToGenericType_WithMatchingGenericType_ShouldReturnTrue()
        {
            // Arrange
            var givenType = typeof(GenericList);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = ReflectionHelper.IsAssignableToGenericType(givenType, genericType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAssignableToGenericType_WithNonMatchingGenericType_ShouldReturnFalse()
        {
            // Arrange
            var givenType = typeof(GenericList);
            var genericType = typeof(IEnumerable<>);

            // Act
            var result = ReflectionHelper.IsAssignableToGenericType(givenType, genericType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsAssignableToGenericType_WithInheritedGenericType_ShouldReturnTrue()
        {
            // Arrange
            var givenType = typeof(DerivedGenericList);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = ReflectionHelper.IsAssignableToGenericType(givenType, genericType);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region GetImplementedGenericTypes Tests

        [Fact]
        public void GetImplementedGenericTypes_WithSingleImplementation_ShouldReturnImplementation()
        {
            // Arrange
            var givenType = typeof(GenericList);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = ReflectionHelper.GetImplementedGenericTypes(givenType, genericType);

            // Assert
            Assert.Single(result);
            Assert.Equal(typeof(IGenericInterface<string>), result[0]);
        }

        [Fact]
        public void GetImplementedGenericTypes_WithMultipleImplementations_ShouldReturnAll()
        {
            // Arrange
            var givenType = typeof(MultiGenericList);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = ReflectionHelper.GetImplementedGenericTypes(givenType, genericType);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(typeof(IGenericInterface<string>), result);
            Assert.Contains(typeof(IGenericInterface<int>), result);
        }

        #endregion

        #region GetSingleAttributeOrDefault Tests

        [Fact]
        public void GetSingleAttributeOrDefault_WithAttribute_ShouldReturnAttribute()
        {
            // Arrange
            var memberInfo = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.PropertyWithAttribute));

            // Act
            var result = ReflectionHelper.GetSingleAttributeOrDefault<TestAttribute>(memberInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestValue", result.Value);
        }

        [Fact]
        public void GetSingleAttributeOrDefault_WithoutAttribute_ShouldReturnDefault()
        {
            // Arrange
            var memberInfo = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.PropertyWithoutAttribute));

            // Act
            var result = ReflectionHelper.GetSingleAttributeOrDefault<TestAttribute>(memberInfo);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetSingleAttributeOrDefault_WithCustomDefault_ShouldReturnCustomDefault()
        {
            // Arrange
            var memberInfo = typeof(TestClassWithAttributes).GetProperty(nameof(TestClassWithAttributes.PropertyWithoutAttribute));
            var defaultValue = new TestAttribute("Default");

            // Act
            var result = ReflectionHelper.GetSingleAttributeOrDefault(memberInfo, defaultValue);

            // Assert
            Assert.Same(defaultValue, result);
        }

        #endregion

        #region GetValueByPath Tests

        [Fact]
        public void GetValueByPath_WithSimpleProperty_ShouldReturnValue()
        {
            // Arrange
            var obj = new TestObject { Name = "TestName" };
            var objectType = typeof(TestObject);
            var propertyPath = "Name";

            // Act
            var result = ReflectionHelper.GetValueByPath(obj, objectType, propertyPath);

            // Assert
            Assert.Equal("TestName", result);
        }

        [Fact]
        public void GetValueByPath_WithNestedProperty_ShouldReturnValue()
        {
            // Arrange
            var obj = new TestObject
            {
                Name = "Test",
                Child = new ChildObject { Value = 42 }
            };
            var objectType = typeof(TestObject);
            var propertyPath = "Child.Value";

            // Act
            var result = ReflectionHelper.GetValueByPath(obj, objectType, propertyPath);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void GetValueByPath_WithFullyQualifiedPath_ShouldStripAndReturnValue()
        {
            // Arrange
            var obj = new TestObject { Name = "TestName" };
            var objectType = typeof(TestObject);
            var propertyPath = $"{objectType.FullName}.Name";

            // Act
            var result = ReflectionHelper.GetValueByPath(obj, objectType, propertyPath);

            // Assert
            Assert.Equal("TestName", result);
        }

        #endregion

        #region GetPublicConstantsRecursively Tests

        [Fact]
        public void GetPublicConstantsRecursively_WithConstants_ShouldReturnAll()
        {
            // Arrange
            var type = typeof(TestConstants);

            // Act
            var result = ReflectionHelper.GetPublicConstantsRecursively(type);

            // Assert
            Assert.Contains("Value1", result);
            Assert.Contains("Value2", result);
            Assert.Contains("NestedValue", result);
        }

        [Fact]
        public void GetPublicConstantsRecursively_WithNoConstants_ShouldReturnEmpty()
        {
            // Arrange
            var type = typeof(TestObject);

            // Act
            var result = ReflectionHelper.GetPublicConstantsRecursively(type);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetHasAttributeProperties Tests

        [Fact]
        public void GetHasAttributeProperties_WithMatchingProperties_ShouldReturnThem()
        {
            // Arrange
            var classType = typeof(TestClassWithAttributes);
            var attributeType = typeof(TestAttribute);

            // Act
            var result = ReflectionHelper.GetHasAttributeProperties(classType, attributeType).ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("PropertyWithAttribute", result[0].Name);
        }

        [Fact]
        public void GetHasAttributeProperties_WithNoMatchingProperties_ShouldReturnEmpty()
        {
            // Arrange
            var classType = typeof(TestObject);
            var attributeType = typeof(TestAttribute);

            // Act
            var result = ReflectionHelper.GetHasAttributeProperties(classType, attributeType).ToList();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetHasAttributeProperties_WithNullClassType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ReflectionHelper.GetHasAttributeProperties(null, typeof(TestAttribute)).ToList());
        }

        [Fact]
        public void GetHasAttributeProperties_WithNullAttributeType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ReflectionHelper.GetHasAttributeProperties(typeof(TestObject), null).ToList());
        }

        #endregion

        #region Test Helper Classes

        private interface IGenericInterface<T> { }

        private class GenericList : IGenericInterface<string> { }

        private class DerivedGenericList : GenericList { }

        private class MultiGenericList : IGenericInterface<string>, IGenericInterface<int> { }

        [AttributeUsage(AttributeTargets.Property)]
        private class TestAttribute : Attribute
        {
            public string Value { get; }

            public TestAttribute(string value)
            {
                Value = value;
            }
        }

        private class TestClassWithAttributes
        {
            [TestAttribute("TestValue")]
            public string PropertyWithAttribute { get; set; }

            public string PropertyWithoutAttribute { get; set; }
        }

        private class TestObject
        {
            public string Name { get; set; }
            public ChildObject Child { get; set; }
        }

        private class ChildObject
        {
            public int Value { get; set; }
        }

        private static class TestConstants
        {
            public const string Constant1 = "Value1";
            public const string Constant2 = "Value2";

            public static class Nested
            {
                public const string NestedConstant = "NestedValue";
            }
        }

        #endregion
    }
}
