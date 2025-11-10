using MiCake.Core.Util.Reflection;
using System;
using Xunit;

namespace MiCake.Core.Tests.Util.Reflection
{
    /// <summary>
    /// Tests for TypeHelper utility class
    /// </summary>
    public class TypeHelperTests
    {
        #region IsFunc Tests

        [Fact]
        public void IsFunc_WithFuncObject_ShouldReturnTrue()
        {
            // Arrange
            Func<int> func = () => 42;

            // Act
            var result = TypeHelper.IsFunc(func);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsFunc_WithNonFuncObject_ShouldReturnFalse()
        {
            // Arrange
            Action action = () => { };

            // Act
            var result = TypeHelper.IsFunc(action);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsFunc_WithNullObject_ShouldReturnFalse()
        {
            // Arrange
            object obj = null;

            // Act
            var result = TypeHelper.IsFunc(obj);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsFunc_Generic_WithMatchingFunc_ShouldReturnTrue()
        {
            // Arrange
            Func<int> func = () => 42;

            // Act
            var result = TypeHelper.IsFunc<int>(func);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsFunc_Generic_WithNonMatchingFunc_ShouldReturnFalse()
        {
            // Arrange
            Func<string> func = () => "test";

            // Act
            var result = TypeHelper.IsFunc<int>(func);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsPrimitiveExtended Tests

        [Theory]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(string), true)]
        [InlineData(typeof(decimal), true)]
        [InlineData(typeof(DateTime), true)]
        [InlineData(typeof(DateTimeOffset), true)]
        [InlineData(typeof(TimeSpan), true)]
        [InlineData(typeof(Guid), true)]
        [InlineData(typeof(bool), true)]
        [InlineData(typeof(byte), true)]
        [InlineData(typeof(object), false)]
        public void IsPrimitiveExtended_WithVariousTypes_ShouldReturnExpected(Type type, bool expected)
        {
            // Act
            var result = TypeHelper.IsPrimitiveExtended(type, includeNullables: false, includeEnums: false);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsPrimitiveExtended_WithNullableInt_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(int?);

            // Act
            var result = TypeHelper.IsPrimitiveExtended(type, includeNullables: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPrimitiveExtended_WithNullableInt_ExcludingNullables_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(int?);

            // Act
            var result = TypeHelper.IsPrimitiveExtended(type, includeNullables: false);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPrimitiveExtended_WithEnum_IncludingEnums_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(TestEnum);

            // Act
            var result = TypeHelper.IsPrimitiveExtended(type, includeEnums: true);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPrimitiveExtended_WithEnum_ExcludingEnums_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(TestEnum);

            // Act
            var result = TypeHelper.IsPrimitiveExtended(type, includeEnums: false);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetFirstGenericArgumentIfNullable Tests

        [Fact]
        public void GetFirstGenericArgumentIfNullable_WithNullableType_ShouldReturnUnderlyingType()
        {
            // Arrange
            var type = typeof(int?);

            // Act
            var result = type.GetFirstGenericArgumentIfNullable();

            // Assert
            Assert.Equal(typeof(int), result);
        }

        [Fact]
        public void GetFirstGenericArgumentIfNullable_WithNonNullableType_ShouldReturnSameType()
        {
            // Arrange
            var type = typeof(int);

            // Act
            var result = type.GetFirstGenericArgumentIfNullable();

            // Assert
            Assert.Equal(typeof(int), result);
        }

        [Fact]
        public void GetFirstGenericArgumentIfNullable_WithGenericNonNullable_ShouldReturnSameType()
        {
            // Arrange
            var type = typeof(System.Collections.Generic.List<int>);

            // Act
            var result = type.GetFirstGenericArgumentIfNullable();

            // Assert
            Assert.Equal(typeof(System.Collections.Generic.List<int>), result);
        }

        #endregion

        #region GetGenericArguments Tests

        [Fact]
        public void GetGenericArguments_WithMatchingInterface_ShouldReturnArguments()
        {
            // Arrange
            var type = typeof(GenericClass);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.GetGenericArguments(type, genericType);

            // Assert
            Assert.Single(result);
            Assert.Equal(typeof(string), result[0]);
        }

        [Fact]
        public void GetGenericArguments_WithMultipleMatchingInterfaces_ShouldReturnAllArguments()
        {
            // Arrange
            var type = typeof(MultiGenericClass);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.GetGenericArguments(type, genericType);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(typeof(string), result);
            Assert.Contains(typeof(int), result);
        }

        [Fact]
        public void GetGenericArguments_WithNoMatchingInterface_ShouldReturnEmpty()
        {
            // Arrange
            var type = typeof(SimpleClass);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.GetGenericArguments(type, genericType);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetGenericInterface Tests

        [Fact]
        public void GetGenericInterface_WithMatchingInterface_ShouldReturnInterface()
        {
            // Arrange
            var type = typeof(GenericClass);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.GetGenericInterface(type, genericType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(typeof(IGenericInterface<string>), result);
        }

        [Fact]
        public void GetGenericInterface_WithNoMatchingInterface_ShouldReturnNull()
        {
            // Arrange
            var type = typeof(SimpleClass);
            var genericType = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.GetGenericInterface(type, genericType);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region IsImplementedGenericInterface Tests

        [Fact]
        public void IsImplementedGenericInterface_WithMatchingInterface_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(GenericClass);
            var generic = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.IsImplementedGenericInterface(type, generic);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsImplementedGenericInterface_WithNonMatchingInterface_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(SimpleClass);
            var generic = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.IsImplementedGenericInterface(type, generic);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsConcrete Tests

        [Fact]
        public void IsConcrete_WithConcreteClass_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act
            var result = TypeHelper.IsConcrete(type);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsConcrete_WithAbstractClass_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(AbstractClass);

            // Act
            var result = TypeHelper.IsConcrete(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsConcrete_WithInterface_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(IGenericInterface<>);

            // Act
            var result = TypeHelper.IsConcrete(type);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsOpenGeneric Tests

        [Fact]
        public void IsOpenGeneric_WithOpenGenericType_ShouldReturnTrue()
        {
            // Arrange
            var type = typeof(System.Collections.Generic.List<>);

            // Act
            var result = TypeHelper.IsOpenGeneric(type);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsOpenGeneric_WithClosedGenericType_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(System.Collections.Generic.List<int>);

            // Act
            var result = TypeHelper.IsOpenGeneric(type);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsOpenGeneric_WithNonGenericType_ShouldReturnFalse()
        {
            // Arrange
            var type = typeof(SimpleClass);

            // Act
            var result = TypeHelper.IsOpenGeneric(type);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsInheritedFrom Tests

        [Fact]
        public void IsInheritedFrom_WithDirectInheritance_ShouldReturnTrue()
        {
            // Arrange
            var givenType = typeof(DerivedClass);
            var baseType = typeof(SimpleClass);

            // Act
            var result = TypeHelper.IsInheritedFrom(givenType, baseType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsInheritedFrom_WithIndirectInheritance_ShouldReturnTrue()
        {
            // Arrange
            var givenType = typeof(DoubleDerivedClass);
            var baseType = typeof(SimpleClass);

            // Act
            var result = TypeHelper.IsInheritedFrom(givenType, baseType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsInheritedFrom_WithNoInheritance_ShouldReturnFalse()
        {
            // Arrange
            var givenType = typeof(SimpleClass);
            var baseType = typeof(DerivedClass);

            // Act
            var result = TypeHelper.IsInheritedFrom(givenType, baseType);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Test Helper Classes

        private enum TestEnum
        {
            Value1,
            Value2
        }

        private interface IGenericInterface<T> { }

        private class SimpleClass { }

        private abstract class AbstractClass { }

        private class GenericClass : IGenericInterface<string> { }

        private class MultiGenericClass : IGenericInterface<string>, IGenericInterface<int> { }

        private class DerivedClass : SimpleClass { }

        private class DoubleDerivedClass : DerivedClass { }

        #endregion
    }
}
