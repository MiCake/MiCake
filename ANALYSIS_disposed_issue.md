# MiCake IntegrationTests Disposed Object Issue - Root Cause Analysis

## 问题表现

当同时运行所有60个tests时，有8个tests失败，错误信息为：
```
System.ObjectDisposedException: Cannot access a disposed object.
Object name: 'IServiceProvider'.
```

但当这8个tests**单独运行**时全部通过。这是一个典型的**test isolation/lifecycle问题**。

## 根本原因分析

### 1. 问题的核心机制

```
Test1.Constructor  → Build ServiceProvider1 → Singleton[LazyEFSaveChangesLifetime] stored ServiceScopeFactory1
                   ↓
Test1.Run         → Calls _saveChangesLifetime.BeforeSaveChangesAsync()
                   ↓
Test1.Dispose     → (ServiceProvider as IDisposable)?.Dispose()
                       ↑
                    **ServiceProvider1 disposed here**
                       
Test2.Constructor  → Build ServiceProvider2 → Singleton[LazyEFSaveChangesLifetime] **REUSED** from DI container
                   ↓
Test2.Run         → Calls _saveChangesLifetime.BeforeSaveChangesAsync()
                   ↓
                   → ExecuteWithScopeAsync() tries to use ServiceScopeFactory1
                   ↓
                   → _serviceScopeFactory.CreateScope() on DISPOSED provider
                   ↓
                   💥 **ObjectDisposedException**
```

### 2. 为什么会这样？

#### 问题代码位置：IntegrationTestBase.cs

```csharp
// Line 39-40: 首先构建第一个ServiceProvider
ServiceProvider = services.BuildServiceProvider();

// Line 49-51: 添加DbContext
services.AddDbContext<TestDbContext>(options =>
    options.UseSqlite(_connection));

// Line 53: 重新构建第二个ServiceProvider
ServiceProvider = services.BuildServiceProvider();
```

**第一个ServiceProvider中注册的所有Singleton服务（包括LazyEFSaveChangesLifetime）从未被Dispose!**

它们在line 53被遗弃，但当新services collection重新调用`BuildServiceProvider()`时：

```csharp
// services collection仍然包含SAME的Singleton定义
// 但.BuildServiceProvider()创建的是**新的DI容器实例**
```

#### 关键问题：Singleton生命周期跨越了ServiceProvider销毁

当Dispose()被调用时（IntegrationTestBase.cs line 79）：

```csharp
public virtual void Dispose()
{
    _micakeApp?.ShutDown();          // Line 77
    _connection?.Close();             // Line 78
    _connection?.Dispose();           // Line 79
    (ServiceProvider as IDisposable)?.Dispose();  // Line 80 - 只dispose第二个!
}
```

**第一个ServiceProvider从未被Dispose，但它的内部Singleton IServiceScopeFactory仍然活跃在DI容器的某个地方！**

### 3. 具体的链式调用流程

```
xUnit test execution order (IMPORTANT - all tests' class ctors are called during test discovery):

1. Test1 (Audit_CreationTime):
   - IntegrationTestBase.__ctor__()
     - BuildServiceProvider() #1 → ServiceProvider1 (stored in _first)
       - LazyEFSaveChangesLifetime REGISTERED as Singleton
       - LazyEFSaveChangesLifetime.__ctor__(IServiceScopeFactory1) ← captures factory
     - AddDbContext()
     - BuildServiceProvider() #2 → ServiceProvider2 (stored in ServiceProvider)
       
2. Test1 runs → commits to DB
   - commit triggers MiCakeEFCoreInterceptor.SavingChangesAsync()
   - LazyEFSaveChangesLifetime.BeforeSaveChangesAsync() called
   - ExecuteWithScopeAsync() calls:
     _serviceScopeFactory.CreateScope()  ← IServiceScopeFactory1 from ServiceProvider1
       ✅ Still works because ServiceProvider1 not disposed yet

3. Test1 finishes → Dispose() called
   - (ServiceProvider as IDisposable)?.Dispose()  ← Only disposes ServiceProvider2!
   - ServiceProvider1 is NEVER disposed
   - BUT LazyEFSaveChangesLifetime instance is destroyed
   - BUT LazyEFSaveChangesLifetime reference might be retained somewhere

4. Test2 (Repository_FindAsync) constructor called:
   - New IntegrationTestBase.__ctor__() 
   - BuildServiceProvider() #3 → ServiceProvider3
   - AddDbContext()
   - BuildServiceProvider() #4 → ServiceProvider4
   
5. Test2 runs → commits to DB
   - MiCakeEFCoreInterceptor (SAME instance from global cache?) calls:
   - LazyEFSaveChangesLifetime.BeforeSaveChangesAsync()
   - ExecuteWithScopeAsync() uses:
     _serviceScopeFactory from Test1
       ↓
     Tries to create scope on disposed ServiceProvider1
     ❌ ObjectDisposedException!
```

### 4. 为什么单个运行时能通过？

```
Single Test Execution:

Test runs → ServiceProvider built → Used → Disposed (lifecycle clear)
           ↓
           No other tests follow, so disposed object is never accessed again
```

### 5. 关键问题点总结

| 位置 | 问题 | 影响 |
|------|------|------|
| IntegrationTestBase line 39 | 构建第一个ServiceProvider后未Dispose | LazyEFSaveChangesLifetime Singleton含有对ServiceProvider1的间接引用 |
| IntegrationTestBase line 53 | 重新构建第二个ServiceProvider | 第一个被遗弃但从未清理 |
| IntegrationTestBase line 80 | 只Dispose ServiceProvider（最后一个） | 前面构建的providers无法清理 |
| LazyEFSaveChangesLifetime line 21 | 存储_serviceScopeFactory as field | 保持对特定ServiceProvider的引用 |
| MiCakeEFCoreModule line 27 | 将LazyEFSaveChangesLifetime注册为Singleton | 实例在DI容器中被缓存，跨越multiple ServiceProviders |

## 详细的Disposed时间线

```
Timeline T0:      IntegrationTestBase.__ctor__ starts
        T1:       services.BuildServiceProvider() → ServiceProvider1
        T2:       LazyEFSaveChangesLifetime(IServiceScopeFactory1) created
        T3:       services.AddDbContext<TestDbContext>()
        T4:       services.BuildServiceProvider() → ServiceProvider2
        T5:       Test.Run() → database operations trigger SaveChangesAsync
        T6:       MiCakeEFCoreInterceptor.SavingChangesAsync() 
        T7:       _saveChangesLifetime.BeforeSaveChangesAsync() 
        T8:       _serviceScopeFactory1.CreateScope()  ✅ ServiceProvider1 still alive
        T9:       IntegrationTestBase.Dispose()
        T10:      (ServiceProvider as IDisposable)?.Dispose()  ← Only disposes ServiceProvider2
        T11:      *** ServiceProvider1 REMAINS IN MEMORY ***
        
        T12:      Next Test.Constructor
        T13:      services.BuildServiceProvider() → ServiceProvider3
        T14:      LazyEFSaveChangesLifetime from CACHE is used
        T15:      Test.Run() → SaveChangesAsync
        T16:      _saveChangesLifetime.BeforeSaveChangesAsync()
        T17:      _serviceScopeFactory1.CreateScope()  ❌ ServiceProvider1 DISPOSED
        T18:      💥 ObjectDisposedException
```

## 为什么是特定的8个tests失败而不是全部或随机?

因为**xUnit按照assembly扫描顺序**加载tests。前面的tests完成Dispose后，后续tests开始时，disposed providers仍在内存中。

某些tests可能：
- 不触发SaveChanges（因此不调用LazyEFSaveChangesLifetime）
- 或者它们的Setup足够幸运地避免了timing问题

## 现象解释

✅ 单个test通过: 没有后续test，disposed object不会被访问
❌ 全部tests一起: disposed object被后续test访问

## 真正的根本原因

这不仅仅是IntegrationTestBase的问题，还涉及MiCake框架设计：

1. **LazyEFSaveChangesLifetime作为Singleton**被注册到DI容器
2. **Singleton含有对特定ServiceProvider instance的间接引用**（通过IServiceScopeFactory）
3. **当ServiceProvider disposed，但Singleton仍然活跃时**（缓存在DI容器中）
4. **后续test使用same cached Singleton**但它指向disposed provider

这是一个**Singleton生命周期跨越ServiceProvider销毁**的问题。
