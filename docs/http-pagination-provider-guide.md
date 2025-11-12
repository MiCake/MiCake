# HttpPaginationProvider 使用指南

## 概述

`HttpPaginationProvider` 是 MiCake 分页加载器中专门用于 HTTP API 分页数据获取的组件。它继承自 `PaginationDataProviderBase`，提供了 HTTP 请求的特定实现，并内置了强大的**重试机制**和**自愈能力**，确保在网络不稳定或服务暂时不可用的情况下仍能可靠地获取数据。

## 核心特性

- 🔄 **智能重试**: 支持多种重试策略（固定延迟、指数退避、线性退避、自定义策略）
- 🛡️ **自愈机制**: 可扩展的自愈钩子，支持代理切换、连接池重置等高级场景
- ⚙️ **灵活配置**: 丰富的配置选项，可根据不同场景定制行为
- 📊 **详细日志**: 完整的请求/重试/自愈过程日志，便于监控和调试
- 🎯 **精细控制**: 可选择哪些异常需要重试，以及重试的次数和延迟
- 🔧 **易于扩展**: 通过虚拟方法和钩子函数，轻松实现自定义行为

## 快速开始

### 基本使用（无重试）

```csharp
public class ProductApiProvider : HttpPaginationProvider<Product>
{
    public ProductApiProvider(ILogger<ProductApiProvider> logger) : base(logger)
    {
    }
    
    protected override HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
        return client;
    }
    
    protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
    {
        return $"{baseRequest.BaseUrl}?offset={offset}&limit={limit}";
    }
    
    protected override PaginationResponse<Product> ParseResponse(string content, HttpStatusCode statusCode)
    {
        if (statusCode != HttpStatusCode.OK)
        {
            return new PaginationResponse<Product>
            {
                Data = new List<Product>(),
                HasMore = false,
                ErrorMessage = $"HTTP {statusCode}"
            };
        }
        
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<Product>>(content);
        return new PaginationResponse<Product>
        {
            Data = apiResponse.Items,
            HasMore = apiResponse.HasMore,
            NextOffset = apiResponse.NextOffset
        };
    }
    
    public async Task<List<Product>?> GetProductsAsync(string category)
    {
        var request = new HttpPaginationRequest
        {
            BaseUrl = "https://api.example.com/products",
            Method = HttpMethod.Get
        };
        
        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 50,
            MaxPages = 100
        };
        
        return await LoadPaginatedDataAsync(request, config, $"products_{category}");
    }
}
```

## 重试机制

### 启用重试

通过设置 `RetryPolicy` 属性来启用重试功能：

```csharp
var provider = new ProductApiProvider(logger);

// 使用固定延迟重试
provider.RetryPolicy = RetryPolicy.FixedDelay(
    maxAttempts: 3,      // 最多重试3次
    delayMs: 1000        // 每次重试间隔1秒
);

// 使用指数退避重试（推荐）
provider.RetryPolicy = RetryPolicy.ExponentialBackoff(
    maxAttempts: 5,      // 最多重试5次
    initialDelayMs: 1000, // 初始延迟1秒
    multiplier: 2.0      // 每次延迟翻倍
);

// 不重试
provider.RetryPolicy = RetryPolicy.NoRetry();
```

### 重试策略

#### 1. 固定延迟 (Fixed Delay)

每次重试使用相同的延迟时间。

```csharp
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.FixedDelay,
    MaxAttempts = 3,
    InitialDelayMs = 2000  // 每次重试延迟2秒
};
```

**适用场景**: 简单的临时性错误，如短暂的网络抖动。

#### 2. 指数退避 (Exponential Backoff)

延迟时间呈指数增长，避免对服务器造成过大压力。

```csharp
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.ExponentialBackoff,
    MaxAttempts = 5,
    InitialDelayMs = 1000,     // 第1次重试: 1秒
    BackoffMultiplier = 2.0,   // 第2次重试: 2秒
    MaxDelayMs = 30000,        // 第3次重试: 4秒
    EnableJitter = true        // 第4次重试: 8秒
};                              // 第5次重试: 16秒
```

**适用场景**: 服务过载、限流场景，或者需要给服务恢复时间的情况（推荐）。

#### 3. 线性退避 (Linear Backoff)

延迟时间线性增长。

```csharp
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.LinearBackoff,
    MaxAttempts = 4,
    InitialDelayMs = 1000  // 1秒, 2秒, 3秒, 4秒
};
```

**适用场景**: 需要逐渐增加延迟但不希望增长过快的场景。

#### 4. 自定义策略 (Custom)

完全自定义延迟计算逻辑。

```csharp
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.Custom,
    MaxAttempts = 5,
    CustomDelayCalculator = (attemptNumber, previousDelay) =>
    {
        // 自定义逻辑: 斐波那契数列延迟
        if (attemptNumber == 1) return 1000;
        if (attemptNumber == 2) return 1000;
        return previousDelay + CalculateFibonacci(attemptNumber - 2) * 1000;
    }
};
```

**适用场景**: 特殊的业务需求，需要完全自定义的重试延迟逻辑。

### 抖动 (Jitter)

抖动可以避免"惊群效应"（多个客户端同时重试导致服务器再次过载）。

```csharp
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.ExponentialBackoff,
    InitialDelayMs = 1000,
    EnableJitter = true,      // 启用抖动
    JitterFactor = 0.2        // ±20% 的随机波动
};
```

启用抖动后，实际延迟会在计算值的 ±20% 范围内随机波动。

### 自定义重试条件

默认情况下，`HttpRequestException`、`TimeoutException` 等常见的网络异常会触发重试。你可以自定义重试条件：

```csharp
provider.RetryPolicy = new RetryPolicy
{
    MaxAttempts = 3,
    ShouldRetry = (exception, attemptNumber) =>
    {
        // 只重试特定的异常
        if (exception is HttpRequestException httpEx)
        {
            // 只重试服务器错误 (5xx)，不重试客户端错误 (4xx)
            return httpEx.StatusCode >= HttpStatusCode.InternalServerError;
        }
        
        // 超时异常总是重试
        if (exception is TimeoutException)
            return true;
            
        // 前3次尝试都重试，之后不再重试
        return attemptNumber <= 3;
    }
};
```

## 自愈机制

自愈机制允许你在请求失败时执行自定义的恢复操作，例如：
- 切换代理服务器
- 重置连接池
- 更新认证令牌
- 切换 API 端点

### 实现自愈逻辑

通过重写 `AttemptSelfHealingAsync` 方法来实现自愈：

```csharp
public class ResilientProductApiProvider : HttpPaginationProvider<Product>
{
    private readonly List<string> _proxyServers;
    private int _currentProxyIndex = 0;
    
    public ResilientProductApiProvider(
        ILogger<ResilientProductApiProvider> logger,
        List<string> proxyServers) : base(logger)
    {
        _proxyServers = proxyServers;
    }
    
    protected override async Task<SelfHealingResult> AttemptSelfHealingAsync(
        SelfHealingContext context)
    {
        _logger.LogWarning("Request failed on attempt {Attempt}: {Error}", 
            context.AttemptNumber, context.Exception.Message);
        
        // 检查是否是代理相关的错误
        if (IsProxyError(context.Exception))
        {
            // 切换到下一个代理
            _currentProxyIndex = (_currentProxyIndex + 1) % _proxyServers.Count;
            var newProxy = _proxyServers[_currentProxyIndex];
            
            _logger.LogInformation("Switching to proxy: {Proxy}", newProxy);
            
            // 重新创建 HttpClient 使用新代理
            var newHttpClient = CreateHttpClientWithProxy(newProxy);
            SetHttpClient(newHttpClient);
            
            return SelfHealingResult.Success(
                message: $"Switched to proxy {newProxy}",
                state: new { ProxyIndex = _currentProxyIndex }
            );
        }
        
        // 检查是否是认证错误
        if (context.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Attempting to refresh authentication token");
            
            var tokenRefreshed = await RefreshAuthTokenAsync();
            if (tokenRefreshed)
            {
                return SelfHealingResult.Success("Authentication token refreshed");
            }
            else
            {
                return SelfHealingResult.StopRetry("Failed to refresh token, stopping retries");
            }
        }
        
        // 默认行为：继续重试但不进行特殊处理
        return SelfHealingResult.Failed("No healing strategy available", continueRetry: true);
    }
    
    private bool IsProxyError(Exception ex)
    {
        return ex is HttpRequestException httpEx 
            && (httpEx.Message.Contains("proxy") || httpEx.Message.Contains("connection"));
    }
    
    private HttpClient CreateHttpClientWithProxy(string proxyUrl)
    {
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy(proxyUrl),
            UseProxy = true
        };
        
        return new HttpClient(handler);
    }
    
    private async Task<bool> RefreshAuthTokenAsync()
    {
        // 实现令牌刷新逻辑
        // ...
        return await Task.FromResult(true);
    }
    
    // ... 其他必需的方法实现
}
```

### SelfHealingContext

自愈上下文包含失败的详细信息：

```csharp
public class SelfHealingContext
{
    public Exception Exception { get; set; }        // 发生的异常
    public HttpStatusCode? StatusCode { get; set; } // HTTP 状态码（如果有）
    public int AttemptNumber { get; set; }          // 当前重试次数
    public PaginationRequest Request { get; set; }  // 失败的请求
    public object? State { get; set; }              // 用户自定义状态
}
```

### SelfHealingResult

自愈结果指示是否成功以及是否继续重试：

```csharp
// 成功的自愈，继续重试
return SelfHealingResult.Success("Proxy switched successfully");

// 失败的自愈，但继续重试
return SelfHealingResult.Failed("Healing failed", continueRetry: true);

// 停止重试（例如认证无法恢复）
return SelfHealingResult.StopRetry("Unrecoverable authentication error");
```

## 扩展钩子方法

`HttpPaginationProvider` 提供了多个虚拟方法供子类重写，以实现自定义行为：

### OnHttpRequestFailed

请求失败时调用（每次失败都会调用，包括重试）。

```csharp
protected override void OnHttpRequestFailed(
    Exception exception, 
    PaginationRequest<HttpPaginationRequest> request, 
    int attemptNumber = 1)
{
    _logger.LogWarning("Request failed on attempt {Attempt}: {Error}", 
        attemptNumber, exception.Message);
    
    // 记录到监控系统
    _metrics.RecordFailure(request.Identifier, exception);
    
    base.OnHttpRequestFailed(exception, request, attemptNumber);
}
```

### OnBeforeRetry

在重试之前调用，可以用来记录日志或执行准备工作。

```csharp
protected override void OnBeforeRetry(
    Exception exception, 
    PaginationRequest<HttpPaginationRequest> request, 
    int attemptNumber, 
    int delayMs)
{
    _logger.LogInformation(
        "Retrying request {Identifier} (attempt {Attempt}) after {Delay}ms delay",
        request.Identifier, attemptNumber, delayMs);
    
    // 更新重试计数器
    _metrics.IncrementRetryCount(request.Identifier);
}
```

### OnRetryExhausted

所有重试都失败后调用。

```csharp
protected override void OnRetryExhausted(
    Exception exception, 
    PaginationRequest<HttpPaginationRequest> request, 
    int totalAttempts)
{
    _logger.LogError(
        "All {Attempts} retry attempts failed for {Identifier}: {Error}",
        totalAttempts, request.Identifier, exception.Message);
    
    // 发送告警
    _alertService.SendAlert($"Pagination failed for {request.Identifier}");
}
```

### OnHttpResponseError / OnHttpResponseSuccess

处理 HTTP 响应时调用。

```csharp
protected override void OnHttpResponseError(
    HttpResponseMessage response, 
    PaginationRequest<HttpPaginationRequest> request, 
    PaginationResponse<TData> parsedResult)
{
    _logger.LogWarning(
        "HTTP {StatusCode} for {Identifier}: {ReasonPhrase}",
        response.StatusCode, request.Identifier, response.ReasonPhrase);
    
    // 特殊处理限流错误
    if (response.StatusCode == (HttpStatusCode)429)
    {
        var retryAfter = response.Headers.RetryAfter?.Delta;
        _logger.LogInformation("Rate limited, retry after: {RetryAfter}", retryAfter);
    }
}

protected override void OnHttpResponseSuccess(
    HttpResponseMessage response, 
    PaginationRequest<HttpPaginationRequest> request, 
    PaginationResponse<TData> parsedResult)
{
    _metrics.RecordSuccess(request.Identifier, parsedResult.Data?.Count ?? 0);
}
```

## 实际应用场景

### 场景1: 电商产品同步（含重试和自愈）

```csharp
public class ProductSyncProvider : HttpPaginationProvider<Product>
{
    private readonly IMemoryCache _cache;
    private readonly List<string> _apiEndpoints;
    private int _currentEndpointIndex = 0;
    
    public ProductSyncProvider(
        ILogger<ProductSyncProvider> logger,
        IMemoryCache cache,
        List<string> apiEndpoints) : base(logger)
    {
        _cache = cache;
        _apiEndpoints = apiEndpoints;
        
        // 配置重试策略
        RetryPolicy = new RetryPolicy
        {
            Strategy = RetryStrategy.ExponentialBackoff,
            MaxAttempts = 5,
            InitialDelayMs = 2000,
            BackoffMultiplier = 2.0,
            MaxDelayMs = 60000,
            EnableJitter = true,
            
            // 自定义重试条件
            ShouldRetry = (ex, attempt) =>
            {
                // 只重试服务器错误和网络错误
                if (ex is HttpRequestException httpEx)
                {
                    return httpEx.StatusCode == null || 
                           httpEx.StatusCode >= HttpStatusCode.InternalServerError;
                }
                return ex is TimeoutException;
            }
        };
    }
    
    protected override HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "ProductSync/1.0");
        return client;
    }
    
    protected override string BuildRequestUrl(
        HttpPaginationRequest baseRequest, 
        int offset, 
        int limit)
    {
        var endpoint = _apiEndpoints[_currentEndpointIndex];
        return $"{endpoint}/products?offset={offset}&limit={limit}";
    }
    
    protected override PaginationResponse<Product> ParseResponse(
        string content, 
        HttpStatusCode statusCode)
    {
        if (statusCode != HttpStatusCode.OK)
        {
            return new PaginationResponse<Product>
            {
                Data = new List<Product>(),
                HasMore = false,
                ErrorMessage = $"HTTP {statusCode}"
            };
        }
        
        var response = JsonSerializer.Deserialize<ProductApiResponse>(content);
        return new PaginationResponse<Product>
        {
            Data = response.Products,
            HasMore = response.HasMore,
            NextOffset = response.NextOffset
        };
    }
    
    protected override async Task<SelfHealingResult> AttemptSelfHealingAsync(
        SelfHealingContext context)
    {
        // 如果当前端点失败，切换到下一个端点
        if (context.Exception is HttpRequestException)
        {
            var oldEndpoint = _apiEndpoints[_currentEndpointIndex];
            _currentEndpointIndex = (_currentEndpointIndex + 1) % _apiEndpoints.Count;
            var newEndpoint = _apiEndpoints[_currentEndpointIndex];
            
            _logger.LogWarning(
                "Switching API endpoint from {Old} to {New} after failure",
                oldEndpoint, newEndpoint);
            
            // 清除旧端点的缓存
            _cache.Remove($"endpoint_{oldEndpoint}");
            
            return SelfHealingResult.Success($"Switched to endpoint {newEndpoint}");
        }
        
        return await base.AttemptSelfHealingAsync(context);
    }
    
    public async Task<List<Product>> SyncAllProductsAsync()
    {
        var request = new HttpPaginationRequest
        {
            BaseUrl = _apiEndpoints[_currentEndpointIndex],
            Method = HttpMethod.Get
        };
        
        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 100,
            MaxPages = 1000,
            DelayBetweenRequests = 500  // 请求间延迟500ms，避免限流
        };
        
        return await LoadPaginatedDataAsync(request, config, "product_sync") 
               ?? new List<Product>();
    }
}
```

### 场景2: 数据采集器（使用代理池）

```csharp
public class WebScraperProvider : HttpPaginationProvider<ScrapedData>
{
    private readonly ProxyPool _proxyPool;
    private HttpClient _currentClient;
    
    public WebScraperProvider(
        ILogger<WebScraperProvider> logger,
        ProxyPool proxyPool) : base(logger)
    {
        _proxyPool = proxyPool;
        
        // 激进的重试策略
        RetryPolicy = new RetryPolicy
        {
            Strategy = RetryStrategy.ExponentialBackoff,
            MaxAttempts = 10,  // 最多重试10次
            InitialDelayMs = 500,
            BackoffMultiplier = 1.5,
            MaxDelayMs = 15000,
            EnableJitter = true,
            JitterFactor = 0.3  // 更大的抖动
        };
    }
    
    protected override HttpClient CreateHttpClient()
    {
        var proxy = _proxyPool.GetNextProxy();
        return CreateClientWithProxy(proxy);
    }
    
    private HttpClient CreateClientWithProxy(ProxyInfo proxy)
    {
        var handler = new HttpClientHandler
        {
            Proxy = new WebProxy(proxy.Url),
            UseProxy = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        
        var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.Add("User-Agent", proxy.UserAgent);
        
        _logger.LogInformation("Using proxy: {Proxy}", proxy.Url);
        return client;
    }
    
    protected override async Task<SelfHealingResult> AttemptSelfHealingAsync(
        SelfHealingContext context)
    {
        // 检查是否是代理被封禁
        if (IsProxyBlocked(context))
        {
            var oldProxy = _proxyPool.Current;
            
            // 标记当前代理为不可用
            _proxyPool.MarkAsBlocked(oldProxy);
            
            // 获取新代理
            var newProxy = _proxyPool.GetNextProxy();
            
            if (newProxy == null)
            {
                return SelfHealingResult.StopRetry("No available proxies");
            }
            
            _logger.LogWarning(
                "Proxy {Old} blocked, switching to {New}",
                oldProxy.Url, newProxy.Url);
            
            // 切换到新代理
            var newClient = CreateClientWithProxy(newProxy);
            SetHttpClient(newClient);
            
            // 等待一段时间再继续，避免立即被检测到
            await Task.Delay(Random.Shared.Next(1000, 3000));
            
            return SelfHealingResult.Success($"Switched to new proxy {newProxy.Url}");
        }
        
        return await base.AttemptSelfHealingAsync(context);
    }
    
    private bool IsProxyBlocked(SelfHealingContext context)
    {
        // 检测常见的代理被封禁的信号
        if (context.StatusCode == HttpStatusCode.Forbidden ||
            context.StatusCode == HttpStatusCode.TooManyRequests ||
            context.StatusCode == (HttpStatusCode)407) // Proxy Authentication Required
        {
            return true;
        }
        
        if (context.Exception is HttpRequestException httpEx)
        {
            var message = httpEx.Message.ToLower();
            return message.Contains("proxy") || 
                   message.Contains("403") || 
                   message.Contains("blocked");
        }
        
        return false;
    }
    
    protected override string BuildRequestUrl(
        HttpPaginationRequest baseRequest, 
        int offset, 
        int limit)
    {
        return $"{baseRequest.BaseUrl}?page={offset / limit + 1}";
    }
    
    protected override PaginationResponse<ScrapedData> ParseResponse(
        string content, 
        HttpStatusCode statusCode)
    {
        // 解析HTML或JSON
        // ...
    }
}

// 代理池类
public class ProxyPool
{
    private readonly List<ProxyInfo> _proxies;
    private readonly HashSet<string> _blockedProxies = new();
    private int _currentIndex = 0;
    
    public ProxyInfo Current => _proxies[_currentIndex];
    
    public ProxyPool(List<ProxyInfo> proxies)
    {
        _proxies = proxies;
    }
    
    public ProxyInfo? GetNextProxy()
    {
        for (int i = 0; i < _proxies.Count; i++)
        {
            _currentIndex = (_currentIndex + 1) % _proxies.Count;
            var proxy = _proxies[_currentIndex];
            
            if (!_blockedProxies.Contains(proxy.Url))
            {
                return proxy;
            }
        }
        
        return null; // 所有代理都被封禁
    }
    
    public void MarkAsBlocked(ProxyInfo proxy)
    {
        _blockedProxies.Add(proxy.Url);
    }
}

public class ProxyInfo
{
    public string Url { get; set; }
    public string UserAgent { get; set; }
}
```

## 配置最佳实践

### 1. 根据服务类型选择策略

```csharp
// 稳定的内部API - 简单重试
provider.RetryPolicy = RetryPolicy.FixedDelay(3, 1000);

// 公共API（可能限流）- 指数退避
provider.RetryPolicy = RetryPolicy.ExponentialBackoff(5, 2000, 2.0);

// 不稳定的第三方API - 自定义策略
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.Custom,
    MaxAttempts = 10,
    CustomDelayCalculator = (attempt, _) => 
    {
        // 特殊逻辑：前3次快速重试，之后慢速重试
        return attempt <= 3 ? 1000 : 10000;
    }
};
```

### 2. 启用抖动避免惊群效应

```csharp
// 在微服务架构中，多个实例同时重试可能导致服务雪崩
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.ExponentialBackoff,
    MaxAttempts = 5,
    InitialDelayMs = 1000,
    EnableJitter = true,      // 必须启用
    JitterFactor = 0.25       // 25% 的随机化
};
```

### 3. 设置合理的超时和最大延迟

```csharp
provider.RetryPolicy = new RetryPolicy
{
    Strategy = RetryStrategy.ExponentialBackoff,
    MaxAttempts = 5,
    InitialDelayMs = 1000,
    MaxDelayMs = 30000  // 避免无限期等待
};

// 同时设置 HttpClient 超时
var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(30);
```

### 4. 自定义重试条件

```csharp
provider.RetryPolicy = new RetryPolicy
{
    MaxAttempts = 5,
    ShouldRetry = (exception, attemptNumber) =>
    {
        // 客户端错误（4xx）不重试
        if (exception is HttpRequestException httpEx 
            && httpEx.StatusCode.HasValue 
            && (int)httpEx.StatusCode.Value >= 400 
            && (int)httpEx.StatusCode.Value < 500)
        {
            return false;
        }
        
        // 服务器错误（5xx）和网络错误重试
        return true;
    }
};
```

## 注意事项

### 1. 资源管理

```csharp
// 使用完毕后释放资源
using var provider = new ProductApiProvider(logger);
provider.RetryPolicy = RetryPolicy.ExponentialBackoff();

var products = await provider.GetProductsAsync("electronics");
// provider 会在此处自动释放
```

### 2. HttpClient 重用

```csharp
// ❌ 错误：频繁创建新的 HttpClient
protected override HttpClient CreateHttpClient()
{
    return new HttpClient();  // 每次都创建新实例
}

// ✅ 正确：使用 HttpClientFactory 或单例
private static readonly HttpClient SharedClient = new HttpClient();

protected override HttpClient CreateHttpClient()
{
    return SharedClient;
}

// 或者在构造函数中注入
public class MyProvider : HttpPaginationProvider<Data>
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public MyProvider(ILogger logger, IHttpClientFactory factory) : base(logger)
    {
        _httpClientFactory = factory;
    }
    
    protected override HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient("MyApiClient");
    }
}
```

### 3. 避免无限重试

```csharp
// ❌ 危险：无限重试
provider.RetryPolicy = new RetryPolicy
{
    MaxAttempts = -1  // 永不放弃！
};

// ✅ 建议：设置合理的上限
provider.RetryPolicy = new RetryPolicy
{
    MaxAttempts = 10,  // 最多10次
    MaxDelayMs = 60000 // 最长等待60秒
};
```

### 4. 幂等性

确保你的API调用是幂等的，否则重试可能导致重复操作：

```csharp
// 如果API不是幂等的，考虑使用幂等性键
protected override HttpRequestMessage CreateHttpRequest(
    PaginationRequest<HttpPaginationRequest> request)
{
    var httpRequest = base.CreateHttpRequest(request);
    
    // 添加幂等性键
    httpRequest.Headers.Add("Idempotency-Key", 
        $"{request.Identifier}_{request.Offset}_{Guid.NewGuid()}");
    
    return httpRequest;
}
```

### 5. 监控和告警

```csharp
protected override void OnRetryExhausted(
    Exception exception, 
    PaginationRequest<HttpPaginationRequest> request, 
    int totalAttempts)
{
    base.OnRetryExhausted(exception, request, totalAttempts);
    
    // 发送告警
    _telemetry.TrackException(exception, new Dictionary<string, string>
    {
        ["Identifier"] = request.Identifier,
        ["TotalAttempts"] = totalAttempts.ToString(),
        ["Offset"] = request.Offset.ToString()
    });
    
    // 如果失败率过高，可能需要人工介入
    if (CalculateFailureRate() > 0.5)  // 超过50%失败率
    {
        _alertService.SendCriticalAlert("High pagination failure rate detected");
    }
}
```

## 性能考虑

### 1. 批量大小优化

```csharp
var config = new PaginationConfig
{
    MaxItemsPerRequest = 100,  // 根据API和网络情况调整
    MaxPages = 100,            // 限制总页数
    DelayBetweenRequests = 100 // 避免限流
};
```

### 2. 并发控制

```csharp
public class ConcurrentPaginationProvider : HttpPaginationProvider<Data>
{
    private readonly SemaphoreSlim _semaphore = new(5, 5);  // 最多5个并发请求
    
    protected override async Task<PaginationResponse<Data>> FetchPageAsync(
        PaginationRequest<HttpPaginationRequest> request,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await base.FetchPageAsync(request, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 3. 缓存

```csharp
public class CachedPaginationProvider : HttpPaginationProvider<Data>
{
    private readonly IMemoryCache _cache;
    
    protected override async Task<PaginationResponse<Data>> FetchPageAsync(
        PaginationRequest<HttpPaginationRequest> request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"{request.Identifier}_{request.Offset}_{request.Limit}";
        
        if (_cache.TryGetValue<PaginationResponse<Data>>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for {Key}", cacheKey);
            return cached;
        }
        
        var result = await base.FetchPageAsync(request, cancellationToken);
        
        if (result.IsSuccess)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        }
        
        return result;
    }
}
```

## 故障排查

### 启用详细日志

```csharp
// 在 appsettings.json 中配置
{
  "Logging": {
    "LogLevel": {
      "MiCake.Util.Paging.Providers": "Trace"  // 启用详细日志
    }
  }
}
```

### 常见问题

**问题1: 重试次数超出预期**

检查 `ShouldRetry` 条件是否正确：

```csharp
provider.RetryPolicy.ShouldRetry = (ex, attempt) =>
{
    _logger.LogDebug("Evaluating retry for attempt {Attempt}: {Exception}", 
        attempt, ex.GetType().Name);
    return true;  // 总是重试 - 可能导致过多重试
};
```

**问题2: 自愈逻辑未执行**

确保重试策略已启用：

```csharp
// ❌ 自愈不会被调用，因为没有启用重试
provider.RetryPolicy = null;

// ✅ 启用重试后才会调用自愈
provider.RetryPolicy = RetryPolicy.FixedDelay(3, 1000);
```

**问题3: 延迟时间不准确**

检查是否启用了抖动：

```csharp
provider.RetryPolicy.EnableJitter = false;  // 禁用抖动获得精确延迟
```

## 总结

`HttpPaginationProvider` 的重试和自愈机制为分页数据获取提供了强大的容错能力：

1. **重试策略**: 灵活的重试策略适应不同场景
2. **自愈能力**: 可扩展的自愈钩子处理复杂故障
3. **精细控制**: 丰富的配置选项和钩子方法
4. **最佳实践**: 抖动、幂等性、监控等生产级特性

通过合理配置和使用这些功能，你可以构建出高可用、高可靠的数据采集系统。
