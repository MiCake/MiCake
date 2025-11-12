using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using MiCake.Util.Cache;
using Xunit;

namespace MiCake.Core.Tests.Util.Cache
{
    /// <summary>
    /// Comprehensive tests for BoundedLruCache to ensure proper memory management and LRU behavior
    /// </summary>
    public class BoundedLruCacheTests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithDefaultMaxSize()
        {
            // Arrange & Act
            var cache = new BoundedLruCache<string, int>();

            // Assert
            Assert.Equal(1000, cache.MaxSize);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Constructor_ShouldInitializeWithCustomMaxSize()
        {
            // Arrange & Act
            var cache = new BoundedLruCache<string, int>(maxSize: 500);

            // Assert
            Assert.Equal(500, cache.MaxSize);
            Assert.Equal(0, cache.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Constructor_ShouldThrowException_WhenMaxSizeIsInvalid(int invalidMaxSize)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedLruCache<string, int>(invalidMaxSize));
        }

        [Fact]
        public void GetOrAdd_ShouldCreateValueOnFirstAccess()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            var key = "test-key";
            var expectedValue = 42;

            // Act
            var actualValue = cache.GetOrAdd(key, k => expectedValue);

            // Assert
            Assert.Equal(expectedValue, actualValue);
            Assert.Equal(1, cache.Count);
            Assert.True(cache.ContainsKey(key));
        }

        [Fact]
        public void GetOrAdd_ShouldReturnCachedValueOnSubsequentAccess()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            var key = "test-key";
            var callCount = 0;

            // Act
            var value1 = cache.GetOrAdd(key, k => ++callCount);
            var value2 = cache.GetOrAdd(key, k => ++callCount);

            // Assert
            Assert.Equal(1, value1);
            Assert.Equal(1, value2);
            Assert.Equal(1, callCount); // Factory should only be called once
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void GetOrAdd_ShouldThrowException_WhenKeyIsNull()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd(null, k => 1));
        }

        [Fact]
        public void GetOrAdd_ShouldThrowException_WhenFactoryIsNull()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache.GetOrAdd("key", null));
        }

        [Fact]
        public void TryGetValue_ShouldReturnFalse_WhenKeyNotExists()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);

            // Act
            var found = cache.TryGetValue("non-existent", out var value);

            // Assert
            Assert.False(found);
            Assert.Equal(default(int), value);
        }

        [Fact]
        public void TryGetValue_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            cache.GetOrAdd("test-key", k => 42);

            // Act
            var found = cache.TryGetValue("test-key", out var value);

            // Assert
            Assert.True(found);
            Assert.Equal(42, value);
        }

        [Fact]
        public void AddOrUpdate_ShouldAddNewValue()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);

            // Act
            cache.AddOrUpdate("key1", 10);

            // Assert
            Assert.Equal(1, cache.Count);
            Assert.True(cache.TryGetValue("key1", out var value));
            Assert.Equal(10, value);
        }

        [Fact]
        public void AddOrUpdate_ShouldUpdateExistingValue()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            cache.AddOrUpdate("key1", 10);

            // Act
            cache.AddOrUpdate("key1", 20);

            // Assert
            Assert.Equal(1, cache.Count);
            Assert.True(cache.TryGetValue("key1", out var value));
            Assert.Equal(20, value);
        }

        [Fact]
        public void AddOrUpdate_ShouldThrowException_WhenKeyIsNull()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache.AddOrUpdate(null, 1));
        }

        [Fact]
        public void Remove_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            cache.AddOrUpdate("key1", 10);

            // Act
            var removed = cache.Remove("key1");

            // Assert
            Assert.True(removed);
            Assert.Equal(0, cache.Count);
            Assert.False(cache.ContainsKey("key1"));
        }

        [Fact]
        public void Remove_ShouldReturnFalse_WhenKeyNotExists()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);

            // Act
            var removed = cache.Remove("non-existent");

            // Assert
            Assert.False(removed);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Remove_ShouldReturnFalse_WhenKeyIsNull()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);

            // Act
            var removed = cache.Remove(null);

            // Assert
            Assert.False(removed);
        }

        [Fact]
        public void Cache_ShouldEvictLeastRecentlyUsedItems_WhenMaxSizeExceeded()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 2);

            // Act - Add 3 items to a cache with max size 2
            cache.GetOrAdd("key1", k => 1);
            cache.GetOrAdd("key2", k => 2);
            cache.GetOrAdd("key3", k => 3); // This should evict key1

            // Assert
            Assert.Equal(2, cache.Count);
            Assert.False(cache.ContainsKey("key1")); // key1 should be evicted
            Assert.True(cache.ContainsKey("key2"));
            Assert.True(cache.ContainsKey("key3"));
            Assert.True(cache.TryGetValue("key2", out var value2));
            Assert.True(cache.TryGetValue("key3", out var value3));
            Assert.Equal(2, value2);
            Assert.Equal(3, value3);
        }

        [Fact]
        public void TryGetValue_ShouldUpdateAccessOrder()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 2);
            cache.GetOrAdd("key1", k => 1);
            cache.GetOrAdd("key2", k => 2);

            // Act - Access key1 to make it most recently used
            cache.TryGetValue("key1", out _);
            cache.GetOrAdd("key3", k => 3); // This should evict key2, not key1

            // Assert
            Assert.True(cache.ContainsKey("key1")); // key1 should still be present
            Assert.False(cache.ContainsKey("key2")); // key2 should be evicted
            Assert.True(cache.ContainsKey("key3")); // key3 should be present
        }

        [Fact]
        public void GetOrAdd_ShouldUpdateAccessOrder()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 2);
            cache.GetOrAdd("key1", k => 1);
            cache.GetOrAdd("key2", k => 2);

            // Act - Access key1 again to make it most recently used
            cache.GetOrAdd("key1", k => 100); // Should return cached value, not call factory
            cache.GetOrAdd("key3", k => 3); // This should evict key2, not key1

            // Assert
            Assert.True(cache.ContainsKey("key1")); // key1 should still be present
            Assert.False(cache.ContainsKey("key2")); // key2 should be evicted
            Assert.True(cache.ContainsKey("key3")); // key3 should be present
            Assert.True(cache.TryGetValue("key1", out var value1));
            Assert.Equal(1, value1); // Should be original value, not 100
        }

        [Fact]
        public void Clear_ShouldRemoveAllItems()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            cache.GetOrAdd("key1", k => 1);
            cache.GetOrAdd("key2", k => 2);

            // Act
            cache.Clear();

            // Assert
            Assert.Equal(0, cache.Count);
            Assert.False(cache.ContainsKey("key1"));
            Assert.False(cache.ContainsKey("key2"));
            Assert.False(cache.TryGetValue("key1", out _));
            Assert.False(cache.TryGetValue("key2", out _));
        }

        [Fact]
        public void ContainsKey_ShouldReturnCorrectValue()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            cache.AddOrUpdate("existing-key", 42);

            // Act & Assert
            Assert.True(cache.ContainsKey("existing-key"));
            Assert.False(cache.ContainsKey("non-existent-key"));
        }

        [Fact]
        public async Task Cache_ShouldBeThreadSafe()
        {
            // Arrange
            var cache = new BoundedLruCache<int, string>(maxSize: 1000);
            var taskCount = 10;
            var itemsPerTask = 100;
            var results = new ConcurrentBag<string>();

            // Act
            var tasks = new Task[taskCount];
            for (int t = 0; t < taskCount; t++)
            {
                var taskId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < itemsPerTask; i++)
                    {
                        var key = taskId * itemsPerTask + i;
                        var value = cache.GetOrAdd(key, k => $"value-{k}");
                        results.Add(value);
                        Assert.Equal($"value-{key}", value);
                    }
                });
            }

            // Assert - Should not throw any exceptions
            await Task.WhenAll(tasks);
            Assert.Equal(taskCount * itemsPerTask, results.Count);
        }

        [Fact]
        public async Task Cache_ShouldHandleConcurrentEvictions()
        {
            // Arrange
            var cache = new BoundedLruCache<int, string>(maxSize: 50);
            var taskCount = 5;
            var itemsPerTask = 100; // Total 500 items, but cache only holds 50

            // Act
            var tasks = new Task[taskCount];
            for (int t = 0; t < taskCount; t++)
            {
                var taskId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < itemsPerTask; i++)
                    {
                        var key = taskId * itemsPerTask + i;
                        cache.GetOrAdd(key, k => $"value-{k}");
                    }
                });
            }

            // Assert
            await Task.WhenAll(tasks);
            Assert.True(cache.Count <= 50); // Should not exceed max size
            Assert.True(cache.Count > 0); // Should have some items
        }

        [Fact]
        public void Dispose_ShouldClearCache()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            cache.GetOrAdd("key1", k => 1);

            // Act
            cache.Dispose();

            // Assert
            Assert.Equal(0, cache.Count);
            Assert.Throws<ObjectDisposedException>(() => cache.GetOrAdd("key2", k => 2));
            Assert.Throws<ObjectDisposedException>(() => cache.TryGetValue("key1", out _));
            Assert.Throws<ObjectDisposedException>(() => cache.AddOrUpdate("key3", 3));
            Assert.Throws<ObjectDisposedException>(() => cache.Remove("key1"));
            Assert.Throws<ObjectDisposedException>(() => cache.ContainsKey("key1"));
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            cache.GetOrAdd("key1", k => 1);

            // Act
            cache.Dispose();
            cache.Dispose(); // Second disposal should not throw

            // Assert
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void Cache_ShouldSupportNullValues()
        {
            // Arrange
            var cache = new BoundedLruCache<string, string>(maxSize: 100);

            // Act
            var value = cache.GetOrAdd("null-value", k => null);

            // Assert
            Assert.Null(value);
            Assert.True(cache.ContainsKey("null-value"));
            Assert.True(cache.TryGetValue("null-value", out var retrievedValue));
            Assert.Null(retrievedValue);
        }

        [Fact]
        public void Cache_ShouldSupportComplexObjects()
        {
            // Arrange
            var cache = new BoundedLruCache<string, TestObject>(maxSize: 100);
            var testObj = new TestObject { Id = 1, Name = "Test" };

            // Act
            var cachedObj = cache.GetOrAdd("complex-key", k => testObj);

            // Assert
            Assert.Same(testObj, cachedObj);
            Assert.Equal(1, cachedObj.Id);
            Assert.Equal("Test", cachedObj.Name);
        }

        [Fact]
        public void Cache_ShouldHandleVeryLargeEvictions()
        {
            // Arrange
            var cache = new BoundedLruCache<int, int>(maxSize: 5);

            // Act - Add many more items than cache capacity
            for (int i = 0; i < 1000; i++)
            {
                cache.GetOrAdd(i, k => k * 2);
            }

            // Assert
            Assert.Equal(5, cache.Count); // Should maintain exactly max size
            
            // The last 5 items should be in cache (995-999)
            for (int i = 995; i < 1000; i++)
            {
                Assert.True(cache.ContainsKey(i));
                Assert.True(cache.TryGetValue(i, out var value));
                Assert.Equal(i * 2, value);
            }
        }

        /// <summary>
        /// Test helper class for complex object caching
        /// </summary>
        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}