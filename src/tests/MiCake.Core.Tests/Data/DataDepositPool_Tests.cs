using MiCake.Core.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Core.Tests.Data
{
    /// <summary>
    /// Comprehensive tests for DataDepositPool enhancements including capacity limits and thread safety
    /// </summary>
    public class DataDepositPool_Tests
    {
        #region Constructor and Basic Property Tests

        [Fact]
        public void Constructor_WithDefaultCapacity_ShouldInitializeCorrectly()
        {
            // Act
            var pool = new DataDepositPool();

            // Assert
            Assert.Equal(0, pool.Count);
            Assert.Equal(1000, pool.MaxCapacity);
        }

        [Fact]
        public void Constructor_WithCustomCapacity_ShouldInitializeCorrectly()
        {
            // Arrange
            var customCapacity = 500;

            // Act
            var pool = new DataDepositPool(customCapacity);

            // Assert
            Assert.Equal(0, pool.Count);
            Assert.Equal(customCapacity, pool.MaxCapacity);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Constructor_WithInvalidCapacity_ShouldThrowArgumentOutOfRangeException(int invalidCapacity)
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new DataDepositPool(invalidCapacity));
            Assert.Equal("maxCapacity", exception.ParamName);
            Assert.Contains("must be greater than zero", exception.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(5000)]
        public void Constructor_WithValidCapacity_ShouldSetCorrectMaxCapacity(int capacity)
        {
            // Act
            var pool = new DataDepositPool(capacity);

            // Assert
            Assert.Equal(capacity, pool.MaxCapacity);
        }

        #endregion

        #region Deposit Operation Tests

        [Fact]
        public void Deposit_WithValidKeyAndData_ShouldStoreSuccessfully()
        {
            // Arrange
            var pool = new DataDepositPool();
            var key = "test-key";
            var data = new TestData { Value = "test-value" };

            // Act
            pool.Deposit(key, data);

            // Assert
            Assert.Equal(1, pool.Count);
            var retrieved = pool.TakeOut(key);
            Assert.NotNull(retrieved);
            Assert.IsType<TestData>(retrieved);
            Assert.Equal("test-value", ((TestData)retrieved).Value);
        }

        [Fact]
        public void Deposit_WithNullKey_ShouldThrowArgumentNullException()
        {
            // Arrange
            var pool = new DataDepositPool();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => pool.Deposit(null, new object()));
            Assert.Equal("key", exception.ParamName);
        }

        [Fact]
        public void Deposit_WithNullData_ShouldStoreNull()
        {
            // Arrange
            var pool = new DataDepositPool();
            var key = "null-key";

            // Act
            pool.Deposit(key, null);

            // Assert
            Assert.Equal(1, pool.Count);
            Assert.Null(pool.TakeOut(key));
        }

        [Fact]
        public void Deposit_WithDuplicateKey_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var pool = new DataDepositPool();
            var key = "duplicate-key";
            pool.Deposit(key, "first-value");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => pool.Deposit(key, "second-value"));
            Assert.Contains("already exists", exception.Message);
            Assert.Contains(key, exception.Message);
            Assert.Contains("DataDepositPool", exception.Message);
        }

        [Fact]
        public void Deposit_MultipleItems_ShouldIncrementCount()
        {
            // Arrange
            var pool = new DataDepositPool();

            // Act
            pool.Deposit("key1", "value1");
            Assert.Equal(1, pool.Count);

            pool.Deposit("key2", "value2");
            Assert.Equal(2, pool.Count);

            pool.Deposit("key3", "value3");
            Assert.Equal(3, pool.Count);
        }

        [Fact]
        public void Deposit_ExceedingCapacity_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var pool = new DataDepositPool(maxCapacity: 3);
            pool.Deposit("key1", "value1");
            pool.Deposit("key2", "value2");
            pool.Deposit("key3", "value3");

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => pool.Deposit("key4", "value4"));
            Assert.Contains("capacity exceeded", exception.Message);
            Assert.Contains("Maximum capacity: 3", exception.Message);
            Assert.Contains("current count: 3", exception.Message);
        }

        [Fact]
        public void Deposit_UpToCapacity_ShouldSucceed()
        {
            // Arrange
            var capacity = 5;
            var pool = new DataDepositPool(capacity);

            // Act
            for (int i = 0; i < capacity; i++)
            {
                pool.Deposit($"key{i}", $"value{i}");
            }

            // Assert
            Assert.Equal(capacity, pool.Count);
        }

        #endregion

        #region TakeOut Operation Tests

        [Fact]
        public void TakeOut_WithExistingKey_ShouldReturnData()
        {
            // Arrange
            var pool = new DataDepositPool();
            var key = "existing-key";
            var expectedData = new TestData { Value = "expected" };
            pool.Deposit(key, expectedData);

            // Act
            var result = pool.TakeOut(key);

            // Assert
            Assert.NotNull(result);
            Assert.Same(expectedData, result);
        }

        [Fact]
        public void TakeOut_WithNonExistingKey_ShouldReturnNull()
        {
            // Arrange
            var pool = new DataDepositPool();

            // Act
            var result = pool.TakeOut("non-existing-key");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TakeOut_WithNullKey_ShouldThrowArgumentNullException()
        {
            // Arrange
            var pool = new DataDepositPool();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => pool.TakeOut(null));
            Assert.Equal("key", exception.ParamName);
        }

        [Fact]
        public void TakeOut_AfterDeposit_ShouldReturnCorrectData()
        {
            // Arrange
            var pool = new DataDepositPool();
            var testData = new Dictionary<string, object>
            {
                { "key1", "string-value" },
                { "key2", 42 },
                { "key3", DateTime.Now },
                { "key4", new TestData { Value = "complex" } }
            };

            foreach (var kvp in testData)
            {
                pool.Deposit(kvp.Key, kvp.Value);
            }

            // Act & Assert
            foreach (var kvp in testData)
            {
                var result = pool.TakeOut(kvp.Key);
                Assert.Equal(kvp.Value, result);
            }
        }

        [Fact]
        public void TakeOut_DoesNotRemoveDataFromPool()
        {
            // Arrange
            var pool = new DataDepositPool();
            var key = "persistent-key";
            var data = "persistent-value";
            pool.Deposit(key, data);

            // Act
            var result1 = pool.TakeOut(key);
            var result2 = pool.TakeOut(key);
            var result3 = pool.TakeOut(key);

            // Assert
            Assert.Equal(data, result1);
            Assert.Equal(data, result2);
            Assert.Equal(data, result3);
            Assert.Equal(1, pool.Count);
        }

        #endregion

        #region Release Operation Tests

        [Fact]
        public void Release_ShouldClearAllData()
        {
            // Arrange
            var pool = new DataDepositPool();
            pool.Deposit("key1", "value1");
            pool.Deposit("key2", "value2");
            pool.Deposit("key3", "value3");
            Assert.Equal(3, pool.Count);

            // Act
            pool.Release();

            // Assert
            Assert.Equal(0, pool.Count);
            Assert.Null(pool.TakeOut("key1"));
            Assert.Null(pool.TakeOut("key2"));
            Assert.Null(pool.TakeOut("key3"));
        }

        [Fact]
        public void Release_OnEmptyPool_ShouldNotThrow()
        {
            // Arrange
            var pool = new DataDepositPool();

            // Act & Assert (should not throw)
            pool.Release();
            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void Release_AfterMultipleReleases_ShouldNotThrow()
        {
            // Arrange
            var pool = new DataDepositPool();
            pool.Deposit("key", "value");

            // Act & Assert
            pool.Release();
            pool.Release();
            pool.Release();
            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void Release_AllowsReAddingDataAfter()
        {
            // Arrange
            var pool = new DataDepositPool();
            pool.Deposit("key1", "value1");
            pool.Release();

            // Act
            pool.Deposit("key1", "new-value");

            // Assert
            Assert.Equal(1, pool.Count);
            Assert.Equal("new-value", pool.TakeOut("key1"));
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public async Task Deposit_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var pool = new DataDepositPool(maxCapacity: 1000);
            var tasks = new List<Task>();
            var errors = new List<Exception>();
            var lockObj = new object();

            // Act
            for (int i = 0; i < 100; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        pool.Deposit($"key-{taskId}", $"value-{taskId}");
                    }
                    catch (Exception ex)
                    {
                        lock (lockObj)
                        {
                            errors.Add(ex);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Empty(errors);
            Assert.Equal(100, pool.Count);
        }

        [Fact]
        public async Task Deposit_ConcurrentWithCapacityLimit_ShouldEnforceCapacity()
        {
            // Arrange
            var capacity = 50;
            var pool = new DataDepositPool(capacity);
            var tasks = new List<Task>();
            var successCount = 0;
            var failureCount = 0;
            var lockObj = new object();

            // Act - Try to add more items than capacity
            for (int i = 0; i < capacity + 20; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        pool.Deposit($"key-{taskId}", $"value-{taskId}");
                        lock (lockObj) successCount++;
                    }
                    catch (InvalidOperationException)
                    {
                        lock (lockObj) failureCount++;
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(capacity, successCount);
            Assert.Equal(20, failureCount);
            Assert.Equal(capacity, pool.Count);
        }

        [Fact]
        public async Task TakeOut_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var pool = new DataDepositPool();
            pool.Deposit("shared-key", "shared-value");
            var tasks = new List<Task<object>>();

            // Act
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(Task.Run(() => pool.TakeOut("shared-key")));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result => Assert.Equal("shared-value", result));
            Assert.Equal(1, pool.Count); // Count should not change
        }

        [Fact]
        public async Task MixedOperations_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var pool = new DataDepositPool(maxCapacity: 500);
            var tasks = new List<Task>();
            var errors = new List<Exception>();
            var lockObj = new object();

            // Pre-populate some data
            for (int i = 0; i < 50; i++)
            {
                pool.Deposit($"initial-{i}", $"value-{i}");
            }

            // Act - Mix of Deposit and TakeOut operations
            for (int i = 0; i < 100; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        if (taskId % 2 == 0)
                        {
                            pool.Deposit($"new-{taskId}", $"value-{taskId}");
                        }
                        else
                        {
                            pool.TakeOut($"initial-{taskId % 50}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Some operations might fail (duplicate keys, capacity exceeded)
                        // This is expected in concurrent scenarios
                        if (!(ex is InvalidOperationException))
                        {
                            lock (lockObj)
                            {
                                errors.Add(ex);
                            }
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - Should not have unexpected errors
            Assert.Empty(errors);
            Assert.True(pool.Count >= 0 && pool.Count <= 500);
        }

        #endregion

        #region Edge Cases Tests

        [Fact]
        public void Deposit_WithMaxCapacity_ShouldAcceptExactly()
        {
            // Arrange
            var capacity = 10;
            var pool = new DataDepositPool(capacity);

            // Act
            for (int i = 0; i < capacity; i++)
            {
                pool.Deposit($"key-{i}", $"value-{i}");
            }

            // Assert
            Assert.Equal(capacity, pool.Count);
        }

        [Fact]
        public void Deposit_OneBeyondCapacity_ShouldThrow()
        {
            // Arrange
            var capacity = 5;
            var pool = new DataDepositPool(capacity);
            for (int i = 0; i < capacity; i++)
            {
                pool.Deposit($"key-{i}", $"value-{i}");
            }

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => pool.Deposit("overflow-key", "overflow-value"));
        }

        [Fact]
        public void Deposit_DifferentDataTypes_ShouldStoreCorrectly()
        {
            // Arrange
            var pool = new DataDepositPool();

            // Act
            pool.Deposit("string", "string value");
            pool.Deposit("int", 42);
            pool.Deposit("double", 3.14);
            pool.Deposit("bool", true);
            pool.Deposit("datetime", DateTime.Now);
            pool.Deposit("object", new TestData { Value = "complex" });
            pool.Deposit("null", null);

            // Assert
            Assert.Equal(7, pool.Count);
            Assert.IsType<string>(pool.TakeOut("string"));
            Assert.IsType<int>(pool.TakeOut("int"));
            Assert.IsType<double>(pool.TakeOut("double"));
            Assert.IsType<bool>(pool.TakeOut("bool"));
            Assert.IsType<DateTime>(pool.TakeOut("datetime"));
            Assert.IsType<TestData>(pool.TakeOut("object"));
            Assert.Null(pool.TakeOut("null"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        public void Deposit_WithWhitespaceKey_ShouldSucceed(string key)
        {
            // Arrange
            var pool = new DataDepositPool();

            // Act
            pool.Deposit(key, "value");

            // Assert
            Assert.Equal(1, pool.Count);
            Assert.Equal("value", pool.TakeOut(key));
        }

        [Fact]
        public void Deposit_WithVeryLongKey_ShouldSucceed()
        {
            // Arrange
            var pool = new DataDepositPool();
            var longKey = new string('a', 10000);

            // Act
            pool.Deposit(longKey, "value");

            // Assert
            Assert.Equal(1, pool.Count);
            Assert.Equal("value", pool.TakeOut(longKey));
        }

        [Fact]
        public void Release_ThenDeposit_ShouldAllowReuse()
        {
            // Arrange
            var pool = new DataDepositPool(maxCapacity: 3);
            pool.Deposit("key1", "value1");
            pool.Deposit("key2", "value2");
            pool.Deposit("key3", "value3");

            // Act
            pool.Release();
            pool.Deposit("key1", "new-value1");
            pool.Deposit("key4", "value4");

            // Assert
            Assert.Equal(2, pool.Count);
            Assert.Equal("new-value1", pool.TakeOut("key1"));
            Assert.Equal("value4", pool.TakeOut("key4"));
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldClearAllData()
        {
            // Arrange
            var pool = new DataDepositPool();
            pool.Deposit("key1", "value1");
            pool.Deposit("key2", "value2");

            // Act
            ((IDisposable)pool).Dispose();

            // Assert
            Assert.Equal(0, pool.Count);
        }

        [Fact]
        public void Dispose_CalledTwice_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var pool = new DataDepositPool();
            ((IDisposable)pool).Dispose();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => ((IDisposable)pool).Dispose());
            Assert.Contains("already been disposed", exception.Message);
        }

        #endregion

        #region Test Helper Classes

        private class TestData
        {
            public string Value { get; set; }
        }

        #endregion
    }
}
