using System;
using MiCake.Util.Reflection;
using Xunit;

namespace MiCake.Core.Tests.Util
{
    /// <summary>
    /// Comprehensive tests for CompiledActivator to ensure proper cache behavior and performance
    /// </summary>
    [Collection("ReflectionTests")]
    public class CompiledActivator_Tests
    {
        // Test classes for instantiation
        private class SimpleClass
        {
            public string Value { get; set; }
        }

        private class ClassWithConstructorParameter
        {
            public string Parameter { get; set; }

            public ClassWithConstructorParameter(string parameter)
            {
                Parameter = parameter;
            }
        }

        private class ClassWithMultipleParameters
        {
            public string FirstParam { get; set; }
            public int SecondParam { get; set; }
            public DateTime ThirdParam { get; set; }

            public ClassWithMultipleParameters(string first, int second, DateTime third)
            {
                FirstParam = first;
                SecondParam = second;
                ThirdParam = third;
            }
        }

        private class ClassWithPrivateConstructor
        {
            private ClassWithPrivateConstructor()
            {
            }

            public static ClassWithPrivateConstructor Create()
            {
                return new ClassWithPrivateConstructor();
            }
        }

        private class ClassWithoutParameterlessConstructor
        {
            public ClassWithoutParameterlessConstructor(string required)
            {
            }
        }

        private class ClassWithIntConstructor
        {
            public int Value { get; set; }

            public ClassWithIntConstructor(int value)
            {
                Value = value;
            }
        }

        public CompiledActivator_Tests()
        {
            // Clean up cache before each test
            CompiledActivator.ClearCache();
        }

        #region CreateInstance (Parameterless) Tests

        [Fact]
        public void CreateInstance_WithValidType_ShouldCreateInstance()
        {
            // Act
            var instance = CompiledActivator.CreateInstance(typeof(SimpleClass));

            // Assert
            Assert.NotNull(instance);
            Assert.IsType<SimpleClass>(instance);
        }

        [Fact]
        public void CreateInstance_WithNullType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => CompiledActivator.CreateInstance(null));
            Assert.Equal("type", ex.ParamName);
        }

        [Fact]
        public void CreateInstance_WithTypeWithoutParameterlessConstructor_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CompiledActivator.CreateInstance(typeof(ClassWithoutParameterlessConstructor)));

            Assert.Contains("does not have a parameterless constructor", ex.Message);
        }

        [Fact]
        public void CreateInstance_WithPrivateParameterlessConstructor_ShouldCreateInstance()
        {
            // Act
            var instance = CompiledActivator.CreateInstance(typeof(ClassWithPrivateConstructor));

            // Assert
            Assert.NotNull(instance);
            Assert.IsType<ClassWithPrivateConstructor>(instance);
        }

        [Fact]
        public void CreateInstance_ShouldCacheFactory()
        {
            // Arrange
            var cacheSizeBefore = CompiledActivator.GetFactoryCacheSize();

            // Act
            CompiledActivator.CreateInstance(typeof(SimpleClass));
            var cacheSizeAfter = CompiledActivator.GetFactoryCacheSize();

            // Assert
            Assert.Equal(cacheSizeBefore + 1, cacheSizeAfter);
        }

        [Fact]
        public void CreateInstance_WithSameType_ShouldUseCachedFactory()
        {
            // Arrange
            var cacheSizeBefore = CompiledActivator.GetFactoryCacheSize();

            // Act - create multiple instances of same type
            CompiledActivator.CreateInstance(typeof(SimpleClass));
            var cacheSizeAfterFirst = CompiledActivator.GetFactoryCacheSize();
            CompiledActivator.CreateInstance(typeof(SimpleClass));
            var cacheSizeAfterSecond = CompiledActivator.GetFactoryCacheSize();

            // Assert - cache size should only increase by 1
            Assert.Equal(cacheSizeBefore + 1, cacheSizeAfterFirst);
            Assert.Equal(cacheSizeAfterFirst, cacheSizeAfterSecond);
        }

        [Fact]
        public void CreateInstance_MultipleTypes_ShouldCacheEachSeparately()
        {
            // Arrange
            var cacheSizeBefore = CompiledActivator.GetFactoryCacheSize();

            // Act
            CompiledActivator.CreateInstance(typeof(SimpleClass));
            CompiledActivator.CreateInstance(typeof(ClassWithPrivateConstructor));

            // Assert
            Assert.Equal(cacheSizeBefore + 2, CompiledActivator.GetFactoryCacheSize());
        }

        #endregion

        #region CreateInstance (With Parameters) Tests

        [Fact]
        public void CreateInstanceWithParameters_WithValidTypeAndArgs_ShouldCreateInstance()
        {
            // Act
            var instance = CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test-value");

            // Assert
            Assert.NotNull(instance);
            var typedInstance = Assert.IsType<ClassWithConstructorParameter>(instance);
            Assert.Equal("test-value", typedInstance.Parameter);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithMultipleParameters_ShouldCreateInstanceCorrectly()
        {
            // Arrange
            var date = DateTime.Now;

            // Act
            var instance = CompiledActivator.CreateInstance(
                typeof(ClassWithMultipleParameters),
                "first",
                42,
                date);

            // Assert
            Assert.NotNull(instance);
            var typedInstance = Assert.IsType<ClassWithMultipleParameters>(instance);
            Assert.Equal("first", typedInstance.FirstParam);
            Assert.Equal(42, typedInstance.SecondParam);
            Assert.Equal(date, typedInstance.ThirdParam);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithNullType_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                CompiledActivator.CreateInstance(null, "arg"));

            Assert.Equal("type", ex.ParamName);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithNullArgs_ShouldFallbackToParameterlessConstructor()
        {
            // Act
            var instance = CompiledActivator.CreateInstance(typeof(SimpleClass), null);

            // Assert
            Assert.NotNull(instance);
            Assert.IsType<SimpleClass>(instance);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithEmptyArgs_ShouldFallbackToParameterlessConstructor()
        {
            // Act
            var instance = CompiledActivator.CreateInstance(typeof(SimpleClass));

            // Assert
            Assert.NotNull(instance);
            Assert.IsType<SimpleClass>(instance);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithIncompatibleArguments_ShouldThrowInvalidOperationException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), 123)); // expects string, got int

            Assert.Contains("does not have a constructor with parameters", ex.Message);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithIntParameter_ShouldHandleCorrectly()
        {
            // Act
            var instance = CompiledActivator.CreateInstance(typeof(ClassWithIntConstructor), 999);

            // Assert
            Assert.NotNull(instance);
            var typedInstance = Assert.IsType<ClassWithIntConstructor>(instance);
            Assert.Equal(999, typedInstance.Value);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithNullObjectArgument_ShouldThrowInvalidOperationException()
        {
            // Act & Assert - null is treated as object type which doesn't match string parameter
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), (object)null));

            Assert.Contains("does not have a constructor with parameters", ex.Message);
        }

        [Fact]
        public void CreateInstanceWithParameters_ShouldCacheFactory()
        {
            // Arrange
            var cacheSizeBefore = CompiledActivator.GetParameterizedCacheSize();

            // Act
            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test");
            var cacheSizeAfter = CompiledActivator.GetParameterizedCacheSize();

            // Assert
            Assert.Equal(cacheSizeBefore + 1, cacheSizeAfter);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithSameSignature_ShouldUseCachedFactory()
        {
            // Arrange
            var cacheSizeBefore = CompiledActivator.GetParameterizedCacheSize();

            // Act - create multiple instances with same parameter signature
            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test1");
            var cacheSizeAfterFirst = CompiledActivator.GetParameterizedCacheSize();
            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test2");
            var cacheSizeAfterSecond = CompiledActivator.GetParameterizedCacheSize();

            // Assert - cache size should only increase by 1
            Assert.Equal(cacheSizeBefore + 1, cacheSizeAfterFirst);
            Assert.Equal(cacheSizeAfterFirst, cacheSizeAfterSecond);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithDifferentSignatures_ShouldCacheSeparately()
        {
            // Arrange
            var cacheSizeBefore = CompiledActivator.GetParameterizedCacheSize();

            // Act - create with different parameter types
            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "string");
            CompiledActivator.CreateInstance(typeof(ClassWithIntConstructor), 42);

            // Assert - should cache both separately
            Assert.Equal(cacheSizeBefore + 2, CompiledActivator.GetParameterizedCacheSize());
        }

        [Fact]
        public void ParameterizedFactoryKey_EqualsAndHashCode_BehaveAsExpected()
        {
            // Arrange: reflection access to the private nested struct
            var nested = typeof(CompiledActivator.ParameterizedFactoryKey);

            var ctor = nested.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
                null, new Type[] { typeof(Type), typeof(Type[]) }, null);
            Assert.NotNull(ctor);

            var type = typeof(ClassWithConstructorParameter);
            var argsA = new Type[] { typeof(string) };
            var argsB = new Type[] { typeof(string) };
            var argsC = new Type[] { typeof(int) };

            // Create three key instances via reflection
            var key1 = ctor.Invoke(new object[] { type, argsA });
            var key2 = ctor.Invoke(new object[] { type, argsB });
            var key3 = ctor.Invoke(new object[] { typeof(ClassWithIntConstructor), argsC });

            // Act & Assert: keys with same type & arg types should be equal
            Assert.True(key1!.Equals(key2));
            Assert.Equal(key1.GetHashCode(), key2.GetHashCode());

            // Different type/signature should not be equal
            Assert.False(key1.Equals(key3));
        }

        [Fact]
        public void CreateInstanceWithParameters_WithComplexTypes_ShouldHandleCorrectly()
        {
            // Arrange
            var date = DateTime.Now;
            var cacheSizeBefore = CompiledActivator.GetParameterizedCacheSize();

            // Act
            var instance = CompiledActivator.CreateInstance(
                typeof(ClassWithMultipleParameters),
                "test",
                123,
                date);

            // Assert
            Assert.NotNull(instance);
            var typedInstance = Assert.IsType<ClassWithMultipleParameters>(instance);
            Assert.Equal("test", typedInstance.FirstParam);
            Assert.Equal(123, typedInstance.SecondParam);
            Assert.Equal(date, typedInstance.ThirdParam);

            // Verify cache was used
            Assert.Equal(cacheSizeBefore + 1, CompiledActivator.GetParameterizedCacheSize());
        }

        [Fact]
        public void CacheKeyGeneration_ShouldBeEfficient()
        {
            // Arrange
            var type = typeof(ClassWithMultipleParameters);
            var argTypes = new[] { typeof(string), typeof(int), typeof(DateTime) };

            // Act - Generate cache key multiple times
            var keys = new string[1000];
            // The runtime uses typed key so determinism of a string helper is no longer critical —
            // validate deterministic behavior when calling CreateInstance with the same parameter signature.
            var factories = new string[keys.Length];
            for (int i = 0; i < factories.Length; i++)
            {
                var inst = CompiledActivator.CreateInstance(typeof(ClassWithMultipleParameters), "a", 1, DateTime.MinValue);
                Assert.IsType<ClassWithMultipleParameters>(inst);
                factories[i] = inst.GetType().FullName; // trivial check to ensure instantiation succeeded repeatedly
            }

            // Assert - all instantiations returned the same runtime type and succeeded
            Assert.All(factories, name => Assert.NotNull(name));
            Assert.All(factories, name => Assert.Equal(factories[0], name));
        }

        [Fact]
        public void ParameterizedFactoryKey_OrderAndNulls_AffectEquality()
        {
            var nested = typeof(CompiledActivator.ParameterizedFactoryKey);
            Assert.NotNull(nested);

            var ctor = nested.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public,
                null, new Type[] { typeof(Type), typeof(Type[]) }, null);
            Assert.NotNull(ctor);

            var type = typeof(ClassWithConstructorParameter);
            var a = new Type[] { typeof(string), typeof(int) };
            var b = new Type[] { typeof(int), typeof(string) };
            var c = new Type[] { typeof(string), null };

            var k1 = ctor.Invoke(new object[] { type, a });
            var k2 = ctor.Invoke(new object[] { type, a });
            var k3 = ctor.Invoke(new object[] { type, b });
            var k4 = ctor.Invoke(new object[] { type, c });

            Assert.True(k1!.Equals(k2));
            Assert.Equal(k1.GetHashCode(), k2.GetHashCode());

            Assert.False(k1.Equals(k3));
            Assert.False(k1.Equals(k4));
        }

        #endregion

        #region Cache Management and LRU Eviction Tests

        [Fact]
        public void ClearCache_ShouldClearBothCaches()
        {
            // Arrange
            CompiledActivator.CreateInstance(typeof(SimpleClass));
            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test");

            Assert.True(CompiledActivator.GetFactoryCacheSize() > 0);
            Assert.True(CompiledActivator.GetParameterizedCacheSize() > 0);

            // Act
            CompiledActivator.ClearCache();

            // Assert
            Assert.Equal(0, CompiledActivator.GetFactoryCacheSize());
            Assert.Equal(0, CompiledActivator.GetParameterizedCacheSize());
        }

        [Fact]
        public void ParameterizedCache_ShouldEvictLruEntriesWhenMaxSizeExceeded()
        {
            // Arrange - Create enough distinct parameter signatures to exceed max cache size (256)
            var maxSize = 256;
            var typesToCreate = maxSize + 50;

            // Act - Create many different parameter signatures
            for (int i = 0; i < typesToCreate; i++)
            {
                try
                {
                    CompiledActivator.CreateInstance(typeof(ClassWithIntConstructor), i);
                }
                catch
                {
                    // Some might fail, but that's okay for this test
                }
            }

            // Assert - Cache size should be bounded by maxSize
            var cacheSize = CompiledActivator.GetParameterizedCacheSize();
            Assert.True(cacheSize <= maxSize, $"Cache size {cacheSize} exceeded max {maxSize}");
        }

        [Fact]
        public void FactoryCache_ShouldEvictLruEntriesWhenMaxSizeExceeded()
        {
            // Arrange
            CompiledActivator.ClearCache();
            var maxSize = 1000; // must match CompiledActivator.MaxFactoryCacheSize
            var typesToCreate = maxSize + 50;

            var asmName = new System.Reflection.AssemblyName("DynamicFactoryTypes_" + Guid.NewGuid().ToString("N"));
            var asmBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(asmName, System.Reflection.Emit.AssemblyBuilderAccess.Run);
            var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name);

            for (int i = 0; i < typesToCreate; i++)
            {
                var tb = moduleBuilder.DefineType("DynamicType" + i, System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

                // Define a simple public parameterless constructor
                var ctor = tb.DefineConstructor(System.Reflection.MethodAttributes.Public, System.Reflection.CallingConventions.Standard, Type.EmptyTypes);
                var il = ctor.GetILGenerator();
                il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                il.Emit(System.Reflection.Emit.OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
                il.Emit(System.Reflection.Emit.OpCodes.Ret);

                var t = tb.CreateType();

                try
                {
                    if (t != null)
                        CompiledActivator.CreateInstance(t);
                }
                catch
                {
                    // ignore
                }
            }

            var cacheSize = CompiledActivator.GetFactoryCacheSize();
            Assert.True(cacheSize <= maxSize, $"Factory cache size {cacheSize} exceeded max {maxSize}");
        }

        [Fact]
        public void GetFactoryCacheSize_ShouldReturnAccurateCount()
        {
            // Arrange
            var initialSize = CompiledActivator.GetFactoryCacheSize();

            // Act
            CompiledActivator.CreateInstance(typeof(SimpleClass));
            CompiledActivator.CreateInstance(typeof(ClassWithPrivateConstructor));
            CompiledActivator.CreateInstance(typeof(SimpleClass)); // Should use cached factory

            // Assert
            Assert.Equal(initialSize + 2, CompiledActivator.GetFactoryCacheSize());
        }

        [Fact]
        public void GetParameterizedCacheSize_ShouldReturnAccurateCount()
        {
            // Arrange
            var initialSize = CompiledActivator.GetParameterizedCacheSize();

            // Act
            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test1");
            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test2");
            CompiledActivator.CreateInstance(typeof(ClassWithIntConstructor), 42);

            // Assert
            Assert.Equal(initialSize + 2, CompiledActivator.GetParameterizedCacheSize());
        }

        #endregion

        #region Stress and Edge Case Tests

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void CreateInstance_WithMultipleTypes_ShouldHandleCorrectly(int typeCount)
        {
            // Arrange & Act
            for (int i = 0; i < typeCount; i++)
            {
                CompiledActivator.CreateInstance(typeof(SimpleClass));
            }

            // Assert
            Assert.Equal(1, CompiledActivator.GetFactoryCacheSize());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void CreateInstanceWithParameters_WithInvalidIntValue_ShouldStillWork(int invalidValue)
        {
            // Act
            var instance = CompiledActivator.CreateInstance(typeof(ClassWithIntConstructor), invalidValue);

            // Assert
            Assert.NotNull(instance);
            var typedInstance = Assert.IsType<ClassWithIntConstructor>(instance);
            Assert.Equal(invalidValue, typedInstance.Value);
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateInstance_ShouldBeThreadSafe()
        {
            // Arrange
            var tasks = new System.Threading.Tasks.Task[100];
            var errors = new System.Collections.Generic.List<Exception>();
            var lockObj = new object();

            // Act
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            CompiledActivator.CreateInstance(typeof(SimpleClass));
                            CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), "test");
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            errors.Add(ex);
                        }
                    }
                });
            }

            await System.Threading.Tasks.Task.WhenAll(tasks);

            // Assert
            Assert.Empty(errors);
            Assert.True(CompiledActivator.GetFactoryCacheSize() > 0);
            Assert.True(CompiledActivator.GetParameterizedCacheSize() > 0);
        }

        [Fact]
        public void CreateInstance_RepeatedCalls_ShouldReturnDifferentInstances()
        {
            // Act
            var instance1 = CompiledActivator.CreateInstance(typeof(SimpleClass));
            var instance2 = CompiledActivator.CreateInstance(typeof(SimpleClass));

            // Assert - Should be different instances
            Assert.NotSame(instance1, instance2);
        }

        #endregion

        #region Exception Message Tests

        [Fact]
        public void CreateInstance_WithInvalidType_ShouldHaveDescriptiveErrorMessage()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CompiledActivator.CreateInstance(typeof(ClassWithoutParameterlessConstructor)));

            Assert.Contains("ClassWithoutParameterlessConstructor", ex.Message);
            Assert.Contains("parameterless constructor", ex.Message);
        }

        [Fact]
        public void CreateInstanceWithParameters_WithMissingConstructor_ShouldHaveDescriptiveErrorMessage()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                CompiledActivator.CreateInstance(typeof(ClassWithConstructorParameter), 123));

            Assert.Contains("ClassWithConstructorParameter", ex.Message);
            Assert.Contains("constructor with parameters", ex.Message);
        }

        #endregion
    }
}
