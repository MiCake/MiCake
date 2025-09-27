# MiCake 熔断器使用指南

## 概述

MiCake 熔断器是一个基于断路器模式（Circuit Breaker Pattern）的容错组件，用于提高系统的稳定性和可用性。它通过监控服务调用的成功率，在服务出现故障时自动切换到备用服务或快速失败，避免级联故障。

## 核心特性

- 🔄 **自动故障切换**: 当主服务失败时，自动切换到备用服务
- 📊 **实时监控**: 监控服务健康状态和调用成功率
- 🎯 **多种选择策略**: 支持优先级、轮询、最少负载、并行竞争等策略
- ⚡ **高性能**: 低延迟的状态检查和切换机制
- 🛡️ **线程安全**: 支持高并发环境下的安全使用
- 📈 **可配置**: 丰富的配置选项满足不同场景需求

## 基本概念

### 熔断器状态

熔断器有三种状态：

- **关闭 (Closed)**: 正常状态，所有请求正常通过
- **打开 (Open)**: 熔断状态，请求被阻断，直接返回失败
- **半开 (Half-Open)**: 试探状态，允许少量请求通过以测试服务是否恢复

### 选择策略

- **PriorityOrder**: 按优先级顺序选择服务
- **RoundRobin**: 轮询选择服务
- **LeastLoad**: 选择当前负载最低的服务
- **ParallelRace**: 并行调用所有服务，返回最快的成功响应

## 快速开始

### 1. 定义服务提供者

首先，实现 `ICircuitBreakerProvider<TRequest, TResponse>` 接口：

```csharp
public class ApiProvider : ICircuitBreakerProvider<ApiRequest, ApiResponse>
{
    private readonly HttpClient _httpClient;
    
    public string ProviderName => "MainAPI";
    
    public ApiProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<ApiResponse?> ExecuteAsync(ApiRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/data", request, cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: cancellationToken);
        }
        
        throw new HttpRequestException($"API request failed: {response.StatusCode}");
    }
    
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

### 2. 配置依赖注入

在 MiCake 模块中注册服务：

```csharp
public class MyApiModule : MiCakeModule
{
    public override Task ConfigServices(ModuleConfigServiceContext context)
    {
        // 注册服务提供者
        context.Services.AddSingleton<ICircuitBreakerProvider<ApiRequest, ApiResponse>, ApiProvider>();
        context.Services.AddSingleton<ICircuitBreakerProvider<ApiRequest, ApiResponse>, BackupApiProvider>();
        
        // 注册熔断器
        context.Services.AddGenericCircuitBreaker();
        
        // 配置熔断器
        context.Services.AddSingleton<CircuitBreakerConfig>(provider =>
        {
            return new CircuitBreakerConfigBuilder()
                .WithFailureThreshold(3)
                .WithSuccessThreshold(2)
                .WithOpenStateTimeout(TimeSpan.FromMinutes(1))
                .WithMaxConcurrentOperations(50)
                .WithSelectionStrategy(ProviderSelectionStrategy.PriorityOrder)
                .Build();
        });
        
        return base.ConfigServices(context);
    }
}
```

### 3. 使用熔断器

在服务中注入并使用熔断器：

```csharp
public class DataService
{
    private readonly GenericCircuitBreaker<ApiRequest, ApiResponse> _circuitBreaker;
    
    public DataService(GenericCircuitBreaker<ApiRequest, ApiResponse> circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
        
        // 配置服务优先级
        _circuitBreaker.WithPrimaryProvider("MainAPI")
                      .WithFallbackProvider("BackupAPI", 100);
    }
    
    public async Task<ApiResponse?> GetDataAsync(string query)
    {
        var request = new ApiRequest { Query = query };
        return await _circuitBreaker.ExecuteAsync(request);
    }
}
```

## 高级配置

### 1. 自定义配置

```csharp
var config = new CircuitBreakerConfigBuilder()
    .WithFailureThreshold(5)           // 5次失败后熔断
    .WithSuccessThreshold(3)           // 3次成功后恢复
    .WithOpenStateTimeout(TimeSpan.FromMinutes(2))  // 2分钟后尝试恢复
    .WithMaxConcurrentOperations(100)  // 最大并发数
    .WithProviderOrder("Primary", "Secondary", "Tertiary")  // 设置优先级
    .WithSelectionStrategy(ProviderSelectionStrategy.LeastLoad)  // 使用最少负载策略
    .Build();
```

### 2. 多种选择策略

#### 优先级策略
```csharp
_circuitBreaker.SetProviderPriorities(new Dictionary<string, int>
{
    ["HighPerformanceAPI"] = 1,    // 最高优先级
    ["StandardAPI"] = 10,          // 中等优先级
    ["BackupAPI"] = 100            // 最低优先级
});
```

#### 并行竞争策略
```csharp
_circuitBreaker.SetSelectionStrategy(ProviderSelectionStrategy.ParallelRace);

// 并行调用所有可用服务，返回最快的成功响应
var result = await _circuitBreaker.ExecuteAsync(request);
```

#### 轮询策略
```csharp
_circuitBreaker.SetSelectionStrategy(ProviderSelectionStrategy.RoundRobin);

// 按轮询方式选择服务
var result = await _circuitBreaker.ExecuteAsync(request);
```

### 3. 监控和诊断

```csharp
public class CircuitBreakerMonitorService
{
    private readonly GenericCircuitBreaker<ApiRequest, ApiResponse> _circuitBreaker;
    
    public CircuitBreakerMonitorService(GenericCircuitBreaker<ApiRequest, ApiResponse> circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync()
    {
        // 获取所有服务状态
        var status = _circuitBreaker.GetProvidersStatus();
        
        var healthyProviders = status.Count(s => s.Value.State == CircuitState.Closed);
        var totalProviders = status.Count;
        
        if (healthyProviders == 0)
        {
            return new HealthCheckResult("所有服务都不可用", isHealthy: false);
        }
        
        // 刷新服务状态
        await _circuitBreaker.RefreshProviderStatusAsync();
        
        return new HealthCheckResult($"{healthyProviders}/{totalProviders} 服务可用", isHealthy: true);
    }
    
    public Dictionary<string, ServiceHealthInfo> GetDetailedStatus()
    {
        var status = _circuitBreaker.GetProvidersStatus();
        var priorities = _circuitBreaker.GetProviderPriorities();
        
        return status.ToDictionary(
            kvp => kvp.Key,
            kvp => new ServiceHealthInfo
            {
                State = kvp.Value.State,
                FailureCount = kvp.Value.Failures,
                SuccessCount = kvp.Value.Successes,
                ConcurrentOperations = kvp.Value.Concurrent,
                Priority = priorities.GetValueOrDefault(kvp.Key, 0)
            });
    }
}
```

## 实际应用场景

### 1. 微服务架构中的服务调用

```csharp
public class OrderService
{
    private readonly GenericCircuitBreaker<PaymentRequest, PaymentResponse> _paymentCircuitBreaker;
    private readonly GenericCircuitBreaker<InventoryRequest, InventoryResponse> _inventoryCircuitBreaker;
    
    public async Task<OrderResult> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            // 检查库存
            var inventoryResult = await _inventoryCircuitBreaker.ExecuteAsync(
                new InventoryRequest { ProductId = request.ProductId, Quantity = request.Quantity });
                
            if (inventoryResult?.Available != true)
            {
                return OrderResult.Failed("库存不足");
            }
            
            // 处理支付
            var paymentResult = await _paymentCircuitBreaker.ExecuteAsync(
                new PaymentRequest { Amount = request.Amount, PaymentMethod = request.PaymentMethod });
                
            if (paymentResult?.Success != true)
            {
                return OrderResult.Failed("支付失败");
            }
            
            return OrderResult.Success(paymentResult.OrderId);
        }
        catch (Exception ex)
        {
            // 熔断器会自动处理服务不可用的情况
            return OrderResult.Failed($"订单处理失败: {ex.Message}");
        }
    }
}
```

### 2. 数据库主从切换

```csharp
public class UserRepository
{
    private readonly GenericCircuitBreaker<string, User> _circuitBreaker;
    
    public UserRepository(GenericCircuitBreaker<string, User> circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
        
        // 设置主从数据库优先级
        _circuitBreaker.WithPrimaryProvider("MasterDB")     // 主数据库
                      .WithFallbackProvider("SlaveDB1", 10) // 从数据库1
                      .WithFallbackProvider("SlaveDB2", 20); // 从数据库2
    }
    
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        return await _circuitBreaker.ExecuteAsync(userId);
    }
}
```

### 3. 外部API调用容错

```csharp
public class WeatherService
{
    private readonly GenericCircuitBreaker<WeatherRequest, WeatherData> _circuitBreaker;
    
    public WeatherService(GenericCircuitBreaker<WeatherRequest, WeatherData> circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
        
        // 配置多个天气服务提供商
        _circuitBreaker.WithProviderOrder("OpenWeatherMap", "AccuWeather", "WeatherAPI");
        _circuitBreaker.SetSelectionStrategy(ProviderSelectionStrategy.ParallelRace);
    }
    
    public async Task<WeatherData?> GetWeatherAsync(string city)
    {
        var request = new WeatherRequest { City = city };
        
        // 并行调用所有天气服务，返回最快的响应
        return await _circuitBreaker.ExecuteAsync(request);
    }
}
```

## 最佳实践

### 1. 配置建议

- **失败阈值**: 根据服务的正常错误率设置，通常建议3-5次
- **成功阈值**: 建议设置为2-3次，确保服务真正恢复
- **超时时间**: 根据服务恢复时间设置，建议1-5分钟
- **并发限制**: 根据服务容量设置，避免过载

### 2. 服务提供者实现

```csharp
public class RobustApiProvider : ICircuitBreakerProvider<ApiRequest, ApiResponse>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RobustApiProvider> _logger;
    
    public string ProviderName => "RobustAPI";
    
    public async Task<ApiResponse?> ExecuteAsync(ApiRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 添加超时控制
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            
            var response = await _httpClient.PostAsJsonAsync("/api/data", request, timeoutCts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse>(cancellationToken: timeoutCts.Token);
                _logger.LogDebug("API call succeeded for provider {ProviderName}", ProviderName);
                return result;
            }
            
            _logger.LogWarning("API call failed with status {StatusCode} for provider {ProviderName}", 
                response.StatusCode, ProviderName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API call threw exception for provider {ProviderName}", ProviderName);
            throw;
        }
    }
    
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
            
            var response = await _httpClient.GetAsync("/health", timeoutCts.Token);
            var isAvailable = response.IsSuccessStatusCode;
            
            _logger.LogDebug("Health check for provider {ProviderName}: {IsAvailable}", 
                ProviderName, isAvailable);
                
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for provider {ProviderName}", ProviderName);
            return false;
        }
    }
}
```

### 3. 错误处理

```csharp
public class SafeDataService
{
    private readonly GenericCircuitBreaker<DataRequest, DataResponse> _circuitBreaker;
    private readonly ILogger<SafeDataService> _logger;
    
    public async Task<DataResponse> GetDataSafelyAsync(DataRequest request)
    {
        try
        {
            var result = await _circuitBreaker.ExecuteAsync(request);
            
            if (result != null)
            {
                return result;
            }
            
            // 所有服务都不可用时的降级处理
            _logger.LogWarning("所有服务不可用，使用缓存数据");
            return GetCachedDataOrDefault(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取数据时发生错误");
            
            // 异常情况下的兜底策略
            return new DataResponse 
            { 
                Success = false, 
                ErrorMessage = "服务暂时不可用，请稍后再试" 
            };
        }
    }
    
    private DataResponse GetCachedDataOrDefault(DataRequest request)
    {
        // 实现缓存查找或返回默认值的逻辑
        return new DataResponse { Success = true, Data = GetDefaultData() };
    }
}
```

## 注意事项

### 1. 性能考虑

- 熔断器本身的开销很小，但不要创建过多的实例
- 并行竞争策略会增加资源消耗，适合对延迟敏感的场景
- 定期监控服务状态，避免不必要的健康检查

### 2. 线程安全

- `GenericCircuitBreaker` 是线程安全的，可以在多线程环境中使用
- 服务提供者的实现需要确保线程安全
- 避免在服务提供者中使用共享的可变状态

### 3. 测试建议

```csharp
[Fact]
public async Task CircuitBreaker_ShouldFailoverCorrectly()
{
    // 模拟主服务失败
    var mockPrimaryProvider = new Mock<ICircuitBreakerProvider<string, string>>();
    mockPrimaryProvider.Setup(p => p.ProviderName).Returns("Primary");
    mockPrimaryProvider.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Primary service failed"));
    
    // 模拟备用服务成功
    var mockBackupProvider = new Mock<ICircuitBreakerProvider<string, string>>();
    mockBackupProvider.Setup(p => p.ProviderName).Returns("Backup");
    mockBackupProvider.Setup(p => p.ExecuteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync("backup-response");
    
    var providers = new[] { mockPrimaryProvider.Object, mockBackupProvider.Object };
    var circuitBreaker = new GenericCircuitBreaker<string, string>(providers, Mock.Of<ILogger<GenericCircuitBreaker<string, string>>>());
    
    // 执行测试
    var result = await circuitBreaker.ExecuteAsync("test-request");
    
    // 验证结果
    Assert.Equal("backup-response", result);
}
```

## 总结

MiCake 熔断器提供了强大而灵活的容错机制，帮助构建更加稳定可靠的应用程序。通过合理的配置和使用，可以显著提高系统的可用性和用户体验。记住要根据实际业务场景选择合适的策略，并做好监控和测试。