using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiCake.Util.Cache;
using Xunit;

namespace MiCake.Core.Tests.Util.Cache
{
    /// <summary>
        
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
        public async Task GetOrAdd_ConcurrentSingleKey_FactoryCalledOnce()
        {
            // Arrange
            var cache = new BoundedLruCache<int, string>(maxSize: 100);
            var factoryCalls = 0;

            // Act
            var tasks = new Task[50];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var value = cache.GetOrAdd(42, k =>
                    {
                        Interlocked.Increment(ref factoryCalls);
                        // small delay to increase chance of race
                        Thread.Sleep(5);
                        return "the-value";
                    });
                    Assert.Equal("the-value", value);
                });
            }

            await Task.WhenAll(tasks);

            // Assert: factory should have been called exactly once
            Assert.Equal(1, factoryCalls);
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
        public void Cache_ShouldThrowOnOperationsAfterDispose()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 10);
            cache.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => cache.GetOrAdd("key", _ => 1));
            Assert.Throws<ObjectDisposedException>(() => cache.AddOrUpdate("key", 2));
            Assert.Throws<ObjectDisposedException>(() => cache.TryGetValue("key", out _));
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
          // Removed stray brace here
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

        [Fact]
        public void Cache_LockFree_ShouldRespectMaxSizeAfterHeavyInsertions()
        {
            // Arrange
            var cache = new BoundedLruCache<int, string>(maxSize: 5, segments: 1, useLockFreeApproximation: true);

            // Act
            for (int i = 0; i < 100; i++)
            {
                cache.GetOrAdd(i, k => $"value-{k}");
            }

            // Assert
            Assert.True(cache.Count <= 5, $"Lock-free segment exceeded max size. Actual count: {cache.Count}");
        }

        [Fact]
        public async Task Cache_LockFreeMode_ShouldMaintainLruUnderConcurrentMixedOperations()
        {
            // Arrange
            var cache = new BoundedLruCache<int, string>(maxSize: 50, useLockFreeApproximation: true);
            var taskCount = 8;
            var operationsPerTask = 200;
            var errors = new ConcurrentBag<string>();

            // Act
            var tasks = new Task[taskCount];
            for (int t = 0; t < taskCount; t++)
            {
                var taskId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerTask; i++)
                    {
                        try
                        {
                            var keyType = i % 4;
                            var baseKey = (taskId * 100) + (i / 4);
                            
                            if (keyType == 0)
                                cache.GetOrAdd(baseKey, k => $"value-{k}");
                            else if (keyType == 1)
                                cache.AddOrUpdate(baseKey, $"updated-{baseKey}");
                            else if (keyType == 2)
                                cache.TryGetValue(baseKey, out _);
                            else
                                cache.Remove(baseKey);
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Task {taskId}: {ex.Message}");
                        }
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Empty(errors); // No exceptions should occur
            Assert.InRange(cache.Count, 0, 50);
        }

        [Fact]
        public async Task Cache_LockBasedMode_ShouldMaintainConsistencyUnderConcurrentMixedOperations()
        {
            // Arrange
            var cache = new BoundedLruCache<int, string>(maxSize: 30, useLockFreeApproximation: false);
            var taskCount = 6;
            var operationsPerTask = 150;
            var errors = new ConcurrentBag<string>();

            // Act
            var tasks = new Task[taskCount];
            for (int t = 0; t < taskCount; t++)
            {
                var taskId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerTask; i++)
                    {
                        try
                        {
                            var keyType = i % 3;
                            var key = (taskId * 50) + i;
                            
                            if (keyType == 0)
                                cache.GetOrAdd(key, k => $"val-{k}");
                            else if (keyType == 1)
                                cache.TryGetValue(key, out _);
                            else
                                cache.AddOrUpdate(key, $"updated-{key}");
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Task {taskId}: {ex.Message}");
                        }
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.Empty(errors); // No exceptions should occur
            Assert.True(cache.Count <= 30); // Should not exceed max size
            Assert.True(cache.Count > 0); // Should have some items
        }

        [Fact]
        public async Task Cache_ShouldHandleRapidAddAndRemove()
        {
            // Arrange
            var cache = new BoundedLruCache<int, int>(maxSize: 100);
            var iterations = 1000;

            // Act - Rapidly add and remove the same keys
            await Task.WhenAll(
                Task.Run(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        for (int key = 0; key < 10; key++)
                            cache.GetOrAdd(key, k => k * 10);
                    }
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        for (int key = 0; key < 10; key++)
                            cache.Remove(key);
                    }
                })
            );

            // Assert - No exceptions, valid state
            Assert.True(cache.Count <= 100);
        }

        [Fact]
        public void TryGetValue_ShouldUpdateAccessOrder_ForLockBasedMode()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 3, useLockFreeApproximation: false);
            cache.GetOrAdd("a", k => 1);
            cache.GetOrAdd("b", k => 2);
            cache.GetOrAdd("c", k => 3);

            // Act - Access 'a' to make it most recently used
            cache.TryGetValue("a", out _);
            cache.GetOrAdd("d", k => 4); // Should evict 'b', not 'a'

            // Assert
            Assert.True(cache.ContainsKey("a"));
            Assert.False(cache.ContainsKey("b")); // 'b' should be evicted
            Assert.True(cache.ContainsKey("c"));
            Assert.True(cache.ContainsKey("d"));
        }

        [Fact]
        public void GetOrAdd_ShouldUpdateAccessOrder_ForLockBasedMode()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 3, useLockFreeApproximation: false);
            cache.GetOrAdd("a", k => 1);
            cache.GetOrAdd("b", k => 2);
            cache.GetOrAdd("c", k => 3);

            // Act - Call GetOrAdd on existing key to update access order
            var value = cache.GetOrAdd("a", k => 999); // Should return cached 1, not 999
            cache.GetOrAdd("d", k => 4); // Should evict 'b', not 'a'

            // Assert
            Assert.Equal(1, value); // Cached value, not 999
            Assert.True(cache.ContainsKey("a"));
            Assert.False(cache.ContainsKey("b")); // 'b' should be evicted
            Assert.True(cache.ContainsKey("c"));
            Assert.True(cache.ContainsKey("d"));
        }

        [Fact]
        public void AddOrUpdate_ShouldUpdateAccessOrder_ForNewItems()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 3, useLockFreeApproximation: false);
            cache.GetOrAdd("a", k => 1);
            cache.GetOrAdd("b", k => 2);

            // Act
            cache.AddOrUpdate("c", 3);
            cache.AddOrUpdate("a", 100); // Update 'a' to make it most recently used
            cache.AddOrUpdate("d", 4); // Should evict 'b', not 'a'

            // Assert
            Assert.True(cache.ContainsKey("a"));
            Assert.False(cache.ContainsKey("b")); // 'b' should be evicted
            Assert.True(cache.ContainsKey("c"));
            Assert.True(cache.ContainsKey("d"));
            Assert.True(cache.TryGetValue("a", out var value));
            Assert.Equal(100, value); // Should have updated value
        }

        [Fact]
        public async Task Cache_ShouldHandleHighContentionOnSingleKey()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            var iterations = 5000;
            var results = new ConcurrentBag<int>();

            // Act - Many threads contending on single key
            var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    var value = cache.GetOrAdd("single-key", k => 42);
                    results.Add(value);
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(10 * iterations, results.Count);
            Assert.All(results, v => Assert.Equal(42, v)); // All should return same value
            Assert.True(cache.ContainsKey("single-key"));
            Assert.True(cache.TryGetValue("single-key", out var finalValue));
            Assert.Equal(42, finalValue);
        }

        [Fact]
        public async Task Cache_LockFreeMode_ShouldHandleTimestampIncrement()
        {
            // Arrange
            var cache = new BoundedLruCache<int, string>(maxSize: 20, useLockFreeApproximation: true);
            var threadCount = 4;
            var operationsPerThread = 1000;

            // Act - Rapidly update timestamps
            await Task.WhenAll(Enumerable.Range(0, threadCount).Select(t => Task.Run(() =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    var key = i % 10;
                    cache.GetOrAdd(key, k => $"value-{k}");
                    if (cache.TryGetValue(key, out var val))
                        cache.AddOrUpdate(key, val); // Update and refresh timestamp
                }
            })).ToArray());

            // Assert - Cache should be valid and not corrupt
            Assert.True(cache.Count <= 20);
            Assert.True(cache.Count > 0);
        }

        [Fact]
        public void Cache_SmallMaxSize_ShouldSingleSegment()
        {
            // Arrange & Act
            var cache = new BoundedLruCache<int, int>(maxSize: 5); // < 16, uses single segment

            for (int i = 0; i < 100; i++)
            {
                cache.GetOrAdd(i, k => k);
            }

            // Assert
            Assert.Equal(5, cache.Count);
            // Last 5 items should be present
            for (int i = 95; i < 100; i++)
            {
                Assert.True(cache.ContainsKey(i));
            }
        }

        [Fact]
        public async Task Cache_ConcurrentRemoveDuringEviction()
        {
            // Arrange
            var cache = new BoundedLruCache<int, int>(maxSize: 50);

            // Act - Add items while another thread removes
            var addTask = Task.Run(() =>
            {
                for (int i = 0; i < 500; i++)
                {
                    cache.GetOrAdd(i, k => k);
                    Thread.Sleep(1); // Small delay
                }
            });

            var removeTask = Task.Run(() =>
            {
                for (int i = 100; i < 400; i++)
                {
                    Thread.Sleep(2); // Stagger removals
                    cache.Remove(i);
                }
            });

            await Task.WhenAll(addTask, removeTask);

            // Assert - Should maintain valid state
            Assert.True(cache.Count <= 50);
            Assert.True(cache.Count >= 0);
        }

        [Fact]
        public void Cache_ShouldHandleExceptionInFactory()
        {
            // Arrange
            var cache = new BoundedLruCache<string, int>(maxSize: 100);
            var callCount = 0;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                cache.GetOrAdd("error-key", k =>
                {
                    callCount++;
                    throw new InvalidOperationException("Factory error");
                });
            });

            Assert.Equal(1, callCount); // Factory was called
            Assert.False(cache.ContainsKey("error-key")); // Key not added on exception

            // Second call should retry
            Assert.Throws<InvalidOperationException>(() =>
            {
                cache.GetOrAdd("error-key", k =>
                {
                    callCount++;
                    throw new InvalidOperationException("Factory error");
                });
            });

            Assert.Equal(2, callCount);
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