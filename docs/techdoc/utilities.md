# 工具集

MiCake 在 `MiCake.Core.Util` 中提供了一些与领域无关的通用工具类，方便在框架内部和业务代码中复用。

## 缓存 - BoundedLruCache

`BoundedLruCache<TKey, TValue>` 是一个带容量上限的 LRU（最近最少使用）缓存。

典型用法：

```csharp
using MiCake.Core.Util;

var cache = new BoundedLruCache<string, object>(capacity: 100);

cache.Set("user:1", new { Name = "Tom" });

if (cache.TryGetValue("user:1", out var value))
{
    // 命中缓存
}
```

当缓存达到容量上限时，最久未被访问的条目会被自动移除。

## 转换器 / 查询 / 弹性 / 存储

`MiCake.Core.Util` 还包含若干辅助类型，用于：

- 常见类型转换
- 查询构造与表达式处理
- 弹性策略（如简单重试等）
- 轻量级的内存存储与集合扩展

这些工具大多是无状态的静态方法或小型辅助类，可根据需要在业务代码中直接引用。
