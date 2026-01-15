using System;
using System.Linq;
using System.Threading.Tasks;
using MiCake.Util.Cache;
using Xunit;

namespace MiCake.Core.Tests.Util.Cache
{
    /// <summary>
    /// Usage-oriented tests that demonstrate how the segmented and lock-free modes behave
    /// in deterministic and concurrent scenarios.
    /// These tests are intended to act as examples for consumers and are not intended to
    /// replace the more focused unit tests already present in BoundedLruCacheTests.
    /// </summary>
    public class BoundedLruCacheUsageTests
    {
        [Fact]
        public void SegmentedEviction_PerSegmentCapacityRespected()
        {
            // Arrange: total maxSize 2, 2 segments => each segment capacity 1
            var cache = new BoundedLruCache<int, string>(maxSize: 2, segments: 2);

            // Act: add several keys that map to the same segment (even numbers -> segment 0)
            cache.GetOrAdd(0, k => "v0");
            cache.GetOrAdd(2, k => "v2"); // should evict 0 in its segment because segment 0 capacity == 1

            // add a key into the other segment (odd numbers -> segment 1)
            cache.GetOrAdd(1, k => "v1");

            // Assert: segment-local eviction was performed and cache respects global size
            Assert.False(cache.ContainsKey(0), "Key 0 should have been evicted from its segment");
            Assert.True(cache.ContainsKey(2), "Key 2 should remain in its segment");
            Assert.True(cache.ContainsKey(1), "Key 1 should be present in the other segment");
            Assert.Equal(2, cache.Count);
        }

        [Fact]
        public async Task LockFreeApproximation_ConcurrentStress_KeepsWithinMaxSize()
        {
            // Arrange: enable lock-free approximation with multiple segments
            var cache = new BoundedLruCache<int, string>(maxSize: 50, segments: 4, useLockFreeApproximation: true);

            var taskCount = 20;
            var itemsPerTask = 200; // produce a large number of distinct keys

            var tasks = Enumerable.Range(0, taskCount).Select(t => Task.Run(() =>
            {
                var rnd = new Random(t);
                for (int i = 0; i < itemsPerTask; i++)
                {
                    var key = rnd.Next(0, 10000);
                    cache.GetOrAdd(key, k => $"v-{k}");
                }
            })).ToArray();

            // Act
            await Task.WhenAll(tasks);

            // Assert: no exceptions and the cache respects the configured maximum size
            Assert.InRange(cache.Count, 0, cache.MaxSize);
            Assert.True(cache.MaxSize == 50);
        }
    }
}
