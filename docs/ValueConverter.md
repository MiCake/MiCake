# Convert 模块重构说明

## 概述

本次重构使用**策略模式**和**注册表模式**对原有的类型转换工具进行了全面改进，提供了更好的可扩展性、灵活性和可维护性。

## 新的架构设计

### 核心组件

1. **IValueConverter<TSource, TDestination>** - 转换器接口
   - 定义了所有转换器必须实现的契约
   - `CanConvert(value)`: 检查是否可以转换
   - `Convert(value)`: 执行转换操作

2. **IConverterRegistry** - 转换器注册表接口
   - `Register<TSource, TDestination>(factory)`: 注册转换器工厂
   - `Register<TSource, TDestination>(converter)`: 注册转换器实例
   - `GetConverters<TSource, TDestination>()`: 获取所有已注册的转换器
   - `HasConverter<TSource, TDestination>()`: 检查转换器是否存在
   - `Clear<TSource, TDestination>()`: 清除特定转换器
   - `ClearAll()`: 清除所有转换器

3. **DefaultConverterRegistry** - 默认注册表实现
   - 管理转换器的生命周期
   - 支持线程安全的注册和检索
   - 内置支持 Guid 和 Version 类型的转换

4. **ValueConverter** - 公共 API 入口点
   - 静态方法提供便捷的转换接口
   - `Convert<TSource, TDestination>(source)`: 转换值
   - `RegisterConverter<TSource, TDestination>(factory/instance)`: 注册转换器
   - `HasConverter<TSource, TDestination>()`: 检查转换器
   - `SetRegistry(registry)`: 设置自定义注册表
   - `ResetRegistry()`: 重置为默认注册表

### 内置转换器

1. **SystemValueConverter<TSource, TDestination>** - 系统转换器
   - 使用 `System.Convert.ChangeType` 进行通用类型转换
   - 支持大多数基础类型 (int, string, DateTime, decimal, bool 等)
   - 作为后备转换器，在没有专门转换器时使用

2. **GuidValueConverter<TSource>** - Guid 转换器
   - 支持从 string 和 Guid 类型转换
   - 使用 `Guid.TryParse` 进行安全转换

3. **VersionValueConverter<TSource>** - Version 转换器
   - 支持从 string 和 Version 类型转换
   - 使用 `Version.TryParse` 进行安全转换

## 主要改进

### 1. 开闭原则 (Open/Closed Principle)
- 新的转换器可以通过注册来添加，无需修改现有代码
- 用户可以创建自定义转换器并通过 API 注册

### 2. 单一职责原则 (Single Responsibility Principle)
- 每个转换器专注于特定的转换任务
- 注册表只负责管理转换器的生命周期
- 公共 API 提供统一的接口

### 3. 依赖反转原则 (Dependency Inversion Principle)
- 通过 `IConverterRegistry` 接口解耦实现
- 用户可以提供自己的注册表实现

### 4. 更好的命名
- `IValueConverter` 替代 `IValueConvert` (更清晰)
- `ValueConverter` 替代 `ConvertHelper` (更直观)
- `SystemValueConverter` 替代 `BaseConvert` (更描述性)

### 5. 完整的文档
- 每个类和方法都有详细的 XML 注释
- 参数、返回值和异常都有说明
- 使用 Intellisense 时可以看到帮助信息

### 6. 扩展性
- `RegisterConverter()` 支持工厂函数，便于依赖注入
- `SetRegistry()` 允许用户提供完全自定义的实现
- 支持多个转换器按顺序尝试

## 使用示例

### 基本转换

```csharp
// 基础类型转换
var intValue = ValueConverter.Convert<string, int>("123");
var stringValue = ValueConverter.Convert<int, string>(123);
var dateValue = ValueConverter.Convert<string, DateTime>("2024-01-01");

// Guid 转换
var guid = ValueConverter.Convert<string, Guid>("550e8400-e29b-41d4-a716-446655440000");

// Version 转换
var version = ValueConverter.Convert<string, Version>("1.2.3");
```

### 自定义转换器

```csharp
// 创建自定义转换器
internal class CustomConverter : IValueConverter<string, MyType>
{
    public bool CanConvert(string value)
    {
        // 检查是否可以转换
        return !string.IsNullOrEmpty(value);
    }

    public MyType? Convert(string value)
    {
        // 执行转换
        return MyType.Parse(value);
    }
}

// 注册转换器
ValueConverter.RegisterConverter(() => new CustomConverter());

// 使用
var result = ValueConverter.Convert<string, MyType>("value");
```

### 自定义注册表

```csharp
// 创建自定义注册表
var customRegistry = new DefaultConverterRegistry();
customRegistry.Register<string, MyType>(() => new CustomConverter());

// 使用自定义注册表
ValueConverter.SetRegistry(customRegistry);

// 重置为默认注册表
ValueConverter.ResetRegistry();
```

## 删除的代码

- `BaseConvert.cs` - 替代为 `SystemValueConverter`
- `IValueConvert.cs` - 替代为 `IValueConverter`
- `ConvertHelper.cs` - 功能集成到 `ValueConverter`
- `SpecialPrimitivesTypeConvert.cs` - 分离为 `GuidValueConverter` 和 `VersionValueConverter`
- `ConvertHelp_Tests.cs` - 替代为 `ValueConverter_Tests.cs`
- `ConvertHelper_Registry_Tests.cs` - 功能集成到 `ValueConverter_Tests.cs`

## 向后兼容性

**破坏性变更:**
- API 已从 `ConvertHelper.ConvertValue<TSource, TDestination>(source)` 更改为 `ValueConverter.Convert<TSource, TDestination>(source)`
- 接口从 `IValueConvert` 更改为 `IValueConverter`

如果需要保持向后兼容，可以添加过渡性别名或包装方法。

## 线程安全性

- `DefaultConverterRegistry` 使用锁进行线程安全的读写操作
- 适合在多线程环境下安全使用

## 测试覆盖

新的单元测试 (`ValueConverter_Tests.cs`) 涵盖:
- 基本类型转换 (string, int, DateTime, double, bool 等)
- Guid 转换 (有效和无效输入)
- Version 转换 (有效和无效输入)
- 转换器注册和检查
- 自定义转换器支持
- 错误处理和边界情况
- 注册表管理和切换
