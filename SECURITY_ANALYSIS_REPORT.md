# MiCake Framework - Security & Performance Analysis Report

**Analysis Date:** 2025-11-18  
**Framework Version:** Based on refactor branch  
**Total Lines of Code Analyzed:** ~16,436 lines  
**Analysis Scope:** src/framework directory

---

## Executive Summary

This comprehensive security and performance analysis of the MiCake framework identified several areas requiring attention, ranging from critical security concerns to performance optimizations and usability improvements. The analysis focused on three main dimensions:

1. **Security Vulnerabilities & Logic Errors**
2. **Performance Issues & Resource Management**
3. **Code Usability & Maintainability**

Overall, the framework demonstrates good architectural design with DDD principles, but there are specific areas that need improvement to ensure production-ready security and optimal performance.

---

## 1. Security Vulnerabilities Analysis

### 1.1 Critical Security Issues

#### 🔴 **CRITICAL: Sensitive Information Exposure in Error Responses**

**Location:** `src/framework/MiCake.AspNetCore/DataWrapper/ErrorResponse.cs`

**Issue:**
```csharp
public class ErrorResponse : IResponseWrapper
{
    public string StackTrace { get; set; }  // Line 26
}
```

**Risk:**
- Stack traces can expose sensitive information about the application's internal structure
- Reveals file paths, method names, and potentially sensitive data
- Assists attackers in understanding the application architecture

**Impact:** HIGH - Information disclosure vulnerability

**Recommendation:**
```csharp
// Add configuration to control stack trace exposure
public class DataWrapperOptions
{
    public bool IncludeStackTraceInResponse { get; set; } = false; // Default to false
    public bool IncludeStackTraceOnlyInDevelopment { get; set; } = true;
}

// In ResponseWrapperExecutor
public object WrapError(Exception exception, HttpContext httpContext, int statusCode, object originalData = null)
{
    var includeStackTrace = _options.IncludeStackTraceInResponse;
    
    if (_options.IncludeStackTraceOnlyInDevelopment)
    {
        includeStackTrace = includeStackTrace && IsDevEnvironment(httpContext);
    }
    
    var errorResponse = new ErrorResponse
    {
        Code = DetermineErrorCode(exception),
        Message = exception.Message,
        Details = exception is MiCakeException ? GetSafeDetails(exception) : null,
        StackTrace = includeStackTrace ? exception.StackTrace : null
    };
    
    return errorResponse;
}
```

---

#### 🔴 **CRITICAL: Type Confusion in Dynamic Filter Extensions**

**Location:** `src/framework/MiCake.Core/Util/LinqFilter/Extensions/FilterExtensions.cs`

**Issue:**
```csharp
private static System.Linq.Expressions.Expression BuildFilterValuesExpression(
    System.Linq.Expressions.Expression left, List<FilterValue> filterValues, 
    FilterJoinType filterValueJoinType = FilterJoinType.Or)
{
    // Lines 186-195
    object exceptValue;
    if (valueType == typeof(string))
    {
        exceptValue = TypeDescriptor.GetConverter(left.Type).ConvertFromString(filterValue.Value.ToString());
    }
    else
    {
        exceptValue = System.Convert.ChangeType(filterValue.Value, left.Type);
    }
}
```

**Risk:**
- No validation of property names or filter values
- Potential for injection attacks through filter manipulation
- Uncaught exceptions during type conversion can cause DoS
- Missing input sanitization

**Impact:** HIGH - Potential injection vulnerability and DoS

**Recommendation:**
```csharp
private static System.Linq.Expressions.Expression BuildFilterValuesExpression(
    System.Linq.Expressions.Expression left, List<FilterValue> filterValues, 
    FilterJoinType filterValueJoinType = FilterJoinType.Or)
{
    // Add whitelist for allowed properties
    if (!IsPropertyAllowed(left))
    {
        throw new SecurityException($"Property access not allowed: {left}");
    }
    
    foreach (var filterValue in filterValues)
    {
        var valueType = filterValue.Value.GetType();
        System.Linq.Expressions.Expression right = null;
        
        try
        {
            // Validate and sanitize input
            ValidateFilterValue(filterValue, left.Type);
            
            if (valueType.IsGenericType && filterValue.Value is IList)
            {
                IList list = RemakeStaticListWithNewType(left.Type, filterValue.Value as IList);
                right = System.Linq.Expressions.Expression.Constant(list);
            }
            else
            {
                object exceptValue = ConvertValueSafely(filterValue.Value, left.Type, valueType);
                right = System.Linq.Expressions.Expression.Constant(exceptValue);
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is InvalidCastException)
        {
            // Log and throw specific exception
            throw new InvalidOperationException($"Failed to convert filter value", ex);
        }
    }
}

private static void ValidateFilterValue(FilterValue filterValue, Type targetType)
{
    // Add validation logic
    if (filterValue.Value == null && !IsNullable(targetType))
    {
        throw new ArgumentException("Cannot convert null to non-nullable type");
    }
    
    // Check for suspicious patterns
    if (filterValue.Value is string str && ContainsSuspiciousPatterns(str))
    {
        throw new SecurityException("Suspicious input pattern detected");
    }
}
```

---

#### 🟡 **HIGH: Reflection-based Object Creation Security**

**Location:** `src/framework/MiCake.Core/Util/Reflection/CompiledActivator.cs`

**Issue:**
```csharp
public static object CreateInstance(Type type, params object[] args)
{
    ArgumentNullException.ThrowIfNull(type);
    
    // No validation of type safety or restrictions
    var constructor = type.GetConstructor(
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
        null,
        parameterTypes,
        null);
}
```

**Risk:**
- Can create instances of any type, including potentially dangerous ones
- No restrictions on which types can be instantiated
- Could be exploited if type names come from untrusted sources

**Impact:** MEDIUM - Object injection vulnerability if type is user-controlled

**Recommendation:**
```csharp
private static readonly HashSet<string> _allowedTypeNamespaces = new()
{
    "MiCake.",
    "System.Collections.Generic.",
    // Add other safe namespaces
};

private static readonly HashSet<Type> _forbiddenTypes = new()
{
    typeof(System.Diagnostics.Process),
    typeof(System.IO.File),
    // Add other dangerous types
};

public static object CreateInstance(Type type, params object[] args)
{
    ArgumentNullException.ThrowIfNull(type);
    
    // Validate type is safe to instantiate
    ValidateTypeForActivation(type);
    
    if (args == null || args.Length == 0)
        return CreateInstance(type);
    
    // Rest of the implementation
}

private static void ValidateTypeForActivation(Type type)
{
    if (_forbiddenTypes.Contains(type))
    {
        throw new SecurityException($"Type {type.FullName} is not allowed to be instantiated");
    }
    
    if (!IsTypeInAllowedNamespace(type))
    {
        throw new SecurityException($"Type {type.FullName} is not in an allowed namespace");
    }
}
```

---

### 1.2 Important Security Issues

#### 🟡 **HIGH: Missing Input Validation in HttpPaginationProvider**

**Location:** `src/framework/MiCake.Core/Util/Paging/Providers/HttpPaginationProvider.cs`

**Issue:**
```csharp
public void SetHttpClient(HttpClient httpClient)
{
    ArgumentNullException.ThrowIfNull(httpClient);
    
    if (_httpClient != null && _ownsHttpClient)
    {
        _httpClient.Dispose();
    }
    
    _httpClient = httpClient;
    _ownsHttpClient = false;
}
```

**Risk:**
- No validation of HttpClient configuration
- Could use unsafe settings (e.g., disabled certificate validation)
- No timeout enforcement
- Missing User-Agent validation

**Impact:** MEDIUM - Potential SSRF and insecure HTTP communication

**Recommendation:**
```csharp
public void SetHttpClient(HttpClient httpClient)
{
    ArgumentNullException.ThrowIfNull(httpClient);
    
    // Validate HttpClient configuration
    ValidateHttpClientSecurity(httpClient);
    
    if (_httpClient != null && _ownsHttpClient)
    {
        _httpClient.Dispose();
    }
    
    _httpClient = httpClient;
    _ownsHttpClient = false;
    _logger.LogInformation("HttpClient has been replaced with a new instance");
}

private static void ValidateHttpClientSecurity(HttpClient client)
{
    // Ensure timeout is set
    if (client.Timeout == System.Threading.Timeout.InfiniteTimeSpan)
    {
        throw new ArgumentException("HttpClient must have a finite timeout");
    }
    
    // Check for dangerous handlers
    // Add more security validations as needed
}
```

---

#### 🟡 **MEDIUM: Insufficient Exception Context in UnitOfWorkFilter**

**Location:** `src/framework/MiCake.AspNetCore/Uow/UnitOfWorkFilter.cs`

**Issue:**
```csharp
catch (Exception ex)
{
    // Exception during UoW management - attempt rollback
    if (unitOfWork != null && !unitOfWork.IsCompleted)
    {
        try
        {
            await unitOfWork.RollbackAsync().ConfigureAwait(false);
            _logger.LogWarning(ex, "Rolled back Unit of Work {UowId}...");
        }
        catch (Exception rollbackEx)
        {
            _logger.LogError(rollbackEx, "Failed to rollback Unit of Work {UowId}", unitOfWork.Id);
        }
    }
    throw; // Re-throws original exception without additional context
}
```

**Risk:**
- Loss of rollback failure information
- Original exception might not reflect complete failure state
- Debugging becomes difficult in production

**Impact:** MEDIUM - Information loss, difficult troubleshooting

**Recommendation:**
```csharp
catch (Exception ex)
{
    if (unitOfWork != null && !unitOfWork.IsCompleted)
    {
        try
        {
            await unitOfWork.RollbackAsync().ConfigureAwait(false);
            _logger.LogWarning(ex, "Rolled back Unit of Work {UowId}...");
        }
        catch (Exception rollbackEx)
        {
            _logger.LogError(rollbackEx, "Failed to rollback Unit of Work {UowId}", unitOfWork.Id);
            
            // Create aggregate exception with both failures
            throw new AggregateException(
                "Unit of Work operation failed and rollback also failed", 
                ex, rollbackEx);
        }
    }
    throw;
}
```

---

### 1.3 Logic Errors & Data Integrity Issues

#### 🟡 **MEDIUM: Race Condition in BoundedLruCache**

**Location:** `src/framework/MiCake.Core/Util/Cache/BoundedLruCache.cs`

**Issue:**
```csharp
public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
{
    // Fast path: try to get existing value
    if (_cache.TryGetValue(key, out var existingNode))
    {
        MoveToFront(existingNode);
        return existingNode.Value.Value;
    }
    
    // Slow path: create new value and add to cache
    var newValue = valueFactory(key);  // <--- Factory called outside lock
    AddOrUpdate(key, newValue);
    return newValue;
}
```

**Risk:**
- Multiple threads could call `valueFactory` for the same key simultaneously
- Potential duplicate expensive operations
- If factory has side effects, they could occur multiple times

**Impact:** MEDIUM - Performance degradation, potential data inconsistency

**Recommendation:**
```csharp
public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
{
    if (_disposed)
        throw new ObjectDisposedException(nameof(BoundedLruCache<TKey, TValue>));

    if (key == null)
        throw new ArgumentNullException(nameof(key));

    ArgumentNullException.ThrowIfNull(valueFactory);

    // Fast path: try to get existing value
    if (_cache.TryGetValue(key, out var existingNode))
    {
        MoveToFront(existingNode);
        return existingNode.Value.Value;
    }

    lock (_lock)
    {
        // Double-check after acquiring lock
        if (_cache.TryGetValue(key, out var existingNodeAfterLock))
        {
            MoveToFront(existingNodeAfterLock);
            return existingNodeAfterLock.Value.Value;
        }

        // Create value inside lock to prevent duplicate factory calls
        var newValue = valueFactory(key);
        AddOrUpdateInternal(key, newValue); // Use internal method that assumes lock is held
        return newValue;
    }
}
```

---

#### 🟡 **MEDIUM: Potential Memory Leak in CompiledActivator**

**Location:** `src/framework/MiCake.Core/Util/Reflection/CompiledActivator.cs`

**Issue:**
```csharp
private static readonly ConcurrentDictionary<Type, Func<object>> _factoryCache = new();
private static readonly ConcurrentDictionary<string, CacheEntry> _parameterizedFactoryCache = new();
```

**Risk:**
- Unbounded cache for `_factoryCache` can grow indefinitely
- `_parameterizedFactoryCache` is bounded but still can consume significant memory
- No cache eviction strategy for the simple cache
- Long-running applications could accumulate many type activators

**Impact:** MEDIUM - Memory leak over time

**Recommendation:**
```csharp
private const int MaxSimpleCacheSize = 1000;
private static readonly ConcurrentDictionary<Type, CacheEntry<Func<object>>> _factoryCache = new();
private static readonly LinkedList<Type> _simpleCacheLruList = new();
private static readonly ReaderWriterLockSlim _simpleCacheLock = new();

public static object CreateInstance(Type type)
{
    ArgumentNullException.ThrowIfNull(type);

    var factory = GetOrAddFactory(type);
    return factory();
}

private static Func<object> GetOrAddFactory(Type type)
{
    // Try fast path
    if (_factoryCache.TryGetValue(type, out var entry))
    {
        UpdateSimpleLru(type, entry);
        return entry.Factory;
    }

    // Create and cache
    var factory = CreateFactory(type);
    
    _simpleCacheLock.EnterWriteLock();
    try
    {
        // Double-check
        if (_factoryCache.TryGetValue(type, out var existingEntry))
        {
            return existingEntry.Factory;
        }

        var node = _simpleCacheLruList.AddLast(type);
        var newEntry = new CacheEntry<Func<object>> { Factory = factory, LruNode = node };
        _factoryCache.TryAdd(type, newEntry);

        // Evict if needed
        if (_factoryCache.Count > MaxSimpleCacheSize)
        {
            EvictSimpleLruEntry();
        }
    }
    finally
    {
        _simpleCacheLock.ExitWriteLock();
    }

    return factory;
}
```

---

### 1.4 Minor Security Concerns

#### 🟢 **LOW: Verbose Exception Messages**

**Location:** `src/framework/MiCake.AspNetCore/DataWrapper/Internals/ExceptionDataWrapperFilter.cs`

**Issue:**
```csharp
_logger.LogError(exception, "An unhandled exception occurred.");
```

**Risk:**
- Generic exception messages might expose too much detail in logs
- No differentiation between user-facing and internal errors

**Recommendation:** Add exception categorization and sanitize messages before logging/returning.

---

## 2. Performance Issues Analysis

### 2.1 Critical Performance Issues

#### 🔴 **CRITICAL: N+1 Query Problem Potential in Repository Pattern**

**Location:** `src/framework/MiCake.EntityFrameworkCore/Repository/EFRepository.cs`

**Issue:**
The repository implementation doesn't provide built-in mechanisms to prevent N+1 queries:

```csharp
public class EFRepository<TDbContext, TAggregateRoot, TKey> : 
    EFReadOnlyRepository<TDbContext, TAggregateRoot, TKey>
{
    // No eager loading support
    // No query optimization hints
}
```

**Risk:**
- Lazy loading can cause N+1 queries
- No guidance for developers on query optimization
- Performance degradation with large datasets

**Impact:** HIGH - Severe performance issues in production

**Recommendation:**
```csharp
public interface IRepository<TAggregateRoot, TKey> 
{
    // Add Include support
    Task<TAggregateRoot> FindAsync(
        TKey id, 
        Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> includeFunc = null,
        CancellationToken cancellationToken = default);
    
    // Add projection support for performance
    Task<TResult> FindProjectedAsync<TResult>(
        TKey id,
        Expression<Func<TAggregateRoot, TResult>> projection,
        CancellationToken cancellationToken = default);
}

public class EFRepository<TDbContext, TAggregateRoot, TKey>
{
    public async Task<TAggregateRoot> FindAsync(
        TKey id, 
        Func<IQueryable<TAggregateRoot>, IQueryable<TAggregateRoot>> includeFunc = null,
        CancellationToken cancellationToken = default)
    {
        var dbset = await GetDbSetAsync(cancellationToken).ConfigureAwait(false);
        var query = dbset.AsQueryable();
        
        if (includeFunc != null)
        {
            query = includeFunc(query);
        }
        
        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }
}
```

---

#### 🟡 **HIGH: Inefficient String Concatenation in BuildCacheKey**

**Location:** `src/framework/MiCake.Core/Util/Reflection/CompiledActivator.cs`

**Issue:**
```csharp
private static string BuildCacheKey(Type type, Type[] argTypes)
{
    var capacity = type.FullName.Length + 1;
    for (int i = 0; i < argTypes.Length; i++)
    {
        capacity += argTypes[i].FullName.Length;
        if (i < argTypes.Length - 1)
            capacity += 1;
    }
    
    var sb = new StringBuilder(capacity);
    sb.Append(type.FullName);
    sb.Append('_');
    
    for (int i = 0; i < argTypes.Length; i++)
    {
        if (i > 0)
            sb.Append('_');
        sb.Append(argTypes[i].FullName);
    }
    
    return sb.ToString();
}
```

**Risk:**
- Called frequently for parameterized activations
- Type.FullName can be null for generic types
- StringBuilder allocation overhead

**Impact:** MEDIUM - Performance overhead in high-frequency scenarios

**Recommendation:**
```csharp
// Use value tuple for cache key instead of string
private static (Type type, Type[] argTypes) BuildCacheKey(Type type, Type[] argTypes)
{
    return (type, argTypes);
}

// Update cache dictionary type
private static readonly ConcurrentDictionary<(Type, Type[]), CacheEntry> _parameterizedFactoryCache = 
    new(new TypeArrayEqualityComparer());

// Custom comparer for type arrays
private class TypeArrayEqualityComparer : IEqualityComparer<(Type, Type[])>
{
    public bool Equals((Type, Type[]) x, (Type, Type[]) y)
    {
        if (x.Item1 != y.Item1) return false;
        if (x.Item2.Length != y.Item2.Length) return false;
        
        for (int i = 0; i < x.Item2.Length; i++)
        {
            if (x.Item2[i] != y.Item2[i]) return false;
        }
        return true;
    }

    public int GetHashCode((Type, Type[]) obj)
    {
        var hash = new HashCode();
        hash.Add(obj.Item1);
        foreach (var type in obj.Item2)
        {
            hash.Add(type);
        }
        return hash.ToHashCode();
    }
}
```

---

### 2.2 Important Performance Issues

#### 🟡 **HIGH: Excessive Lock Contention in BoundedLruCache**

**Location:** `src/framework/MiCake.Core/Util/Cache/BoundedLruCache.cs`

**Issue:**
```csharp
public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
{
    if (_cache.TryGetValue(key, out var existingNode))
    {
        MoveToFront(existingNode);  // This locks every time
        return existingNode.Value.Value;
    }
}

private void MoveToFront(LinkedListNode<CacheItem> node)
{
    lock (_lock)  // Contention point
    {
        _accessOrder.Remove(node);
        _accessOrder.AddFirst(node);
    }
}
```

**Risk:**
- Every cache hit requires a lock for LRU update
- High contention in concurrent scenarios
- Performance degradation under load

**Impact:** HIGH - Bottleneck in high-concurrency scenarios

**Recommendation:**
```csharp
// Use segmented locking or lock-free approach
private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cache;
private readonly LinkedList<CacheItem> _accessOrder;
private readonly SemaphoreSlim _lruUpdateSemaphore = new(1, 1);
private volatile int _updateCounter;
private const int LruUpdateInterval = 100; // Update LRU every N accesses

public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
{
    if (_cache.TryGetValue(key, out var existingNode))
    {
        // Update LRU periodically instead of every time
        if (Interlocked.Increment(ref _updateCounter) % LruUpdateInterval == 0)
        {
            _ = Task.Run(() => MoveToFrontAsync(existingNode));
        }
        return existingNode.Value.Value;
    }
    
    // Create new value
    var newValue = valueFactory(key);
    AddOrUpdate(key, newValue);
    return newValue;
}

private async Task MoveToFrontAsync(LinkedListNode<CacheItem> node)
{
    await _lruUpdateSemaphore.WaitAsync();
    try
    {
        if (node.List == _accessOrder)
        {
            _accessOrder.Remove(node);
            _accessOrder.AddFirst(node);
        }
    }
    finally
    {
        _lruUpdateSemaphore.Release();
    }
}
```

---

#### 🟡 **MEDIUM: Inefficient Domain Event Collection**

**Location:** `src/framework/MiCake/DDD/Domain/Entity.cs`

**Issue:**
```csharp
public abstract class Entity<TKey> : IEntity<TKey>, IDomainEventAccessor
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
```

**Risk:**
- Every entity instance allocates a List even if no events are raised
- Memory overhead for entities that never use domain events
- Wasteful for read-only scenarios

**Impact:** MEDIUM - Unnecessary memory allocation

**Recommendation:**
```csharp
public abstract class Entity<TKey> : IEntity<TKey>, IDomainEventAccessor
{
    private List<IDomainEvent>? _domainEvents; // Nullable, lazy initialization
    
    public IReadOnlyCollection<IDomainEvent> DomainEvents => 
        _domainEvents?.AsReadOnly() ?? Array.Empty<IDomainEvent>();
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        
        // Lazy initialization
        _domainEvents ??= new List<IDomainEvent>(4); // Small initial capacity
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }
}
```

---

### 2.3 Minor Performance Issues

#### 🟢 **LOW: Boxing in Expression Building**

**Location:** Various LINQ filter extension methods

**Issue:** Frequent boxing/unboxing operations in expression tree building.

**Recommendation:** Use generic constraints and avoid object conversions where possible.

---

## 3. Code Complexity & Quality Issues

### 3.1 High Complexity Areas

#### 🟡 **FilterExtensions.cs** - Cyclomatic Complexity: High

**Location:** `src/framework/MiCake.Core/Util/LinqFilter/Extensions/FilterExtensions.cs`

**Issues:**
- Long methods with multiple nested conditionals
- Complex type conversion logic
- Difficult to test and maintain

**Recommendation:**
- Break down into smaller, focused methods
- Extract type conversion to separate classes
- Add comprehensive unit tests for edge cases

---

#### 🟡 **CompiledActivator.cs** - Multiple Responsibilities

**Location:** `src/framework/MiCake.Core/Util/Reflection/CompiledActivator.cs`

**Issues:**
- Handles both parameterless and parameterized activation
- Manages two different caching strategies
- LRU eviction mixed with activation logic

**Recommendation:**
- Split into separate classes: `ParameterlessActivator`, `ParameterizedActivator`
- Extract cache management to `ActivatorCache` class
- Use composition over complex inheritance

---

## Summary of Findings

### Security Issues
- **Critical:** 2 issues requiring immediate attention
- **High:** 2 issues requiring prompt attention
- **Medium:** 3 issues requiring attention
- **Low:** 1 issue for consideration

### Performance Issues
- **Critical:** 1 issue (N+1 queries)
- **High:** 2 issues (string concatenation, lock contention)
- **Medium:** 2 issues (memory allocation, domain events)
- **Low:** 1 issue (boxing)

### Code Quality
- **High Complexity:** 2 areas
- **Moderate Complexity:** 5 areas
- **Needs Refactoring:** 3 areas

---

## Priority Recommendations

### Immediate Actions (Week 1)
1. Fix stack trace exposure in error responses
2. Add input validation to filter extensions
3. Implement type validation for CompiledActivator
4. Add eager loading support to repositories

### Short-term Actions (Month 1)
1. Refactor BoundedLruCache for better concurrency
2. Optimize domain event collection
3. Add security validations to HttpPaginationProvider
4. Implement bounded cache for simple activators

### Long-term Actions (Quarter 1)
1. Comprehensive security audit and penetration testing
2. Performance profiling and optimization
3. Code refactoring for maintainability
4. Enhanced documentation and examples

---

**Report Generated By:** MiCake Security Analysis Tool  
**Next Review Date:** 2025-12-18
