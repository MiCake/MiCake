# MiCake 分页加载器使用指南

## 概述

MiCake 分页加载器是一个通用的数据分页获取组件，用于处理大量数据的分页加载场景。它提供了统一的抽象接口，支持多种数据源（HTTP API、数据库、文件等），并内置了错误处理、重试机制和性能优化功能。

## 核心特性

- 🔄 **通用抽象**: 支持任意数据源的分页加载
- 🌐 **HTTP 支持**: 内置 HTTP API 分页加载实现
- ⚡ **高性能**: 支持并发加载和智能缓存
- 🛡️ **容错机制**: 内置错误处理和重试逻辑
- 📊 **进度监控**: 详细的加载进度和状态信息
- ⚙️ **灵活配置**: 丰富的配置选项适应不同场景
- 🔧 **扩展性**: 易于扩展支持新的数据源类型

## 基本概念

### 核心组件

- **PaginationDataProviderBase**: 分页数据提供者基类
- **HttpPaginationProvider**: HTTP API 专用分页提供者
- **PaginationConfig**: 分页配置选项
- **PaginationRequest/Response**: 请求和响应模型

### 分页流程

1. 创建初始请求
2. 循环调用数据源获取分页数据
3. 合并所有页面数据
4. 处理错误和限制条件
5. 返回完整数据集

## 快速开始

### 1. HTTP API 分页加载

首先，创建一个具体的 HTTP 分页提供者：

```csharp
public class ProductApiProvider : HttpPaginationProvider<Product>
{
    private readonly ILogger<ProductApiProvider> _logger;
    
    public ProductApiProvider(ILogger<ProductApiProvider> logger) : base(logger)
    {
    }
    
    protected override HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.example.com/");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer your-token");
        return client;
    }
    
    protected override string BuildRequestUrl(HttpPaginationRequest baseRequest, int offset, int limit)
    {
        var parameters = new Dictionary<string, string>
        {
            ["offset"] = offset.ToString(),
            ["limit"] = limit.ToString()
        };
        
        // 添加其他查询参数
        if (baseRequest.QueryParameters != null)
        {
            foreach (var kvp in baseRequest.QueryParameters)
            {
                parameters[kvp.Key] = kvp.Value;
            }
        }
        
        return BuildUrl(baseRequest.BaseUrl, parameters);
    }
    
    protected override PaginationResponse<Product> ParseResponse(string content, HttpStatusCode statusCode)
    {
        if (statusCode != HttpStatusCode.OK)
        {
            return new PaginationResponse<Product>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = $"HTTP {statusCode}"
            };
        }
        
        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<Product>>(content);
            
            return new PaginationResponse<Product>
            {
                Data = apiResponse.Data,
                HasMore = apiResponse.HasMore,
                NextOffset = apiResponse.NextOffset
            };
        }
        catch (Exception ex)
        {
            return new PaginationResponse<Product>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = $"解析响应失败: {ex.Message}"
            };
        }
    }
}
```

### 2. 配置依赖注入

在 MiCake 模块中注册服务：

```csharp
public class DataModule : MiCakeModule
{
    public override Task ConfigServices(ModuleConfigServiceContext context)
    {
        // 注册分页提供者
        context.Services.AddScoped<ProductApiProvider>();
        
        // 配置默认分页选项
        context.Services.Configure<PaginationConfig>(options =>
        {
            options.MaxItemsPerRequest = 100;
            options.MaxTotalItems = 10000;
            options.MaxRequests = 50;
            options.DelayBetweenRequests = 100;
        });
        
        return base.ConfigServices(context);
    }
}
```

### 3. 使用分页加载器

```csharp
public class ProductService
{
    private readonly ProductApiProvider _paginationProvider;
    
    public ProductService(ProductApiProvider paginationProvider)
    {
        _paginationProvider = paginationProvider;
    }
    
    public async Task<List<Product>?> GetAllProductsAsync(string category = null)
    {
        var request = new HttpPaginationRequest
        {
            BaseUrl = "/products",
            Method = HttpMethod.Get,
            QueryParameters = category != null ? new Dictionary<string, string>
            {
                ["category"] = category
            } : null
        };
        
        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 50,
            MaxTotalItems = 1000,
            DelayBetweenRequests = 200 // 避免过于频繁的请求
        };
        
        return await _paginationProvider.LoadPaginatedDataAsync(
            request, 
            config, 
            $"products_{category ?? "all"}");
    }
}
```

## 高级功能

### 1. 自定义数据源分页提供者

创建数据库分页提供者：

```csharp
public class DatabasePaginationProvider : PaginationDataProviderBase<DatabaseQuery, User>
{
    private readonly IDbContext _dbContext;
    
    public DatabasePaginationProvider(IDbContext dbContext, ILogger<DatabasePaginationProvider> logger) 
        : base(logger)
    {
        _dbContext = dbContext;
    }
    
    protected override async Task<PaginationResponse<User>> FetchPageAsync(
        PaginationRequest<DatabaseQuery> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = _dbContext.Users.AsQueryable();
            
            // 应用过滤条件
            if (!string.IsNullOrEmpty(request.Request.Filter))
            {
                query = query.Where(u => u.Name.Contains(request.Request.Filter));
            }
            
            // 应用排序
            if (!string.IsNullOrEmpty(request.Request.OrderBy))
            {
                query = request.Request.OrderBy switch
                {
                    "name" => query.OrderBy(u => u.Name),
                    "created" => query.OrderBy(u => u.CreatedAt),
                    _ => query.OrderBy(u => u.Id)
                };
            }
            
            // 分页查询
            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query
                .Skip(request.Offset)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);
            
            var hasMore = request.Offset + users.Count < totalCount;
            
            return new PaginationResponse<User>
            {
                Data = users,
                HasMore = hasMore,
                NextOffset = hasMore ? request.Offset + users.Count : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库分页查询失败");
            
            return new PaginationResponse<User>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = $"数据库查询失败: {ex.Message}"
            };
        }
    }
    
    public async Task<List<User>?> GetUsersAsync(DatabaseQuery query, PaginationConfig? config = null)
    {
        return await LoadPaginatedDataAsync(query, config, $"users_{query.Filter}");
    }
}

public class DatabaseQuery
{
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
}
```

### 2. 文件分页读取

创建大文件分页读取器：

```csharp
public class CsvFilePaginationProvider : PaginationDataProviderBase<CsvFileRequest, CsvRecord>
{
    public CsvFilePaginationProvider(ILogger<CsvFilePaginationProvider> logger) : base(logger)
    {
    }
    
    protected override async Task<PaginationResponse<CsvRecord>> FetchPageAsync(
        PaginationRequest<CsvFileRequest> request,
        CancellationToken cancellationToken)
    {
        try
        {
            var records = new List<CsvRecord>();
            var currentLine = 0;
            var recordCount = 0;
            
            using var reader = new StreamReader(request.Request.FilePath);
            
            // 跳过已读取的行
            while (currentLine < request.Offset && !reader.EndOfStream)
            {
                await reader.ReadLineAsync();
                currentLine++;
            }
            
            // 读取当前页的数据
            while (recordCount < request.Limit && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;
                
                var record = ParseCsvLine(line);
                if (record != null)
                {
                    records.Add(record);
                    recordCount++;
                }
                currentLine++;
            }
            
            var hasMore = !reader.EndOfStream;
            
            return new PaginationResponse<CsvRecord>
            {
                Data = records,
                HasMore = hasMore,
                NextOffset = hasMore ? currentLine : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV文件读取失败: {FilePath}", request.Request.FilePath);
            
            return new PaginationResponse<CsvRecord>
            {
                Data = null,
                HasMore = false,
                ErrorMessage = $"文件读取失败: {ex.Message}"
            };
        }
    }
    
    private CsvRecord? ParseCsvLine(string line)
    {
        var fields = line.Split(',');
        if (fields.Length < 3) return null;
        
        return new CsvRecord
        {
            Id = fields[0].Trim(),
            Name = fields[1].Trim(),
            Value = fields[2].Trim()
        };
    }
    
    public async Task<List<CsvRecord>?> ReadCsvFileAsync(string filePath, PaginationConfig? config = null)
    {
        var request = new CsvFileRequest { FilePath = filePath };
        return await LoadPaginatedDataAsync(request, config, Path.GetFileName(filePath));
    }
}

public class CsvFileRequest
{
    public string FilePath { get; set; } = string.Empty;
}

public class CsvRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
```

### 3. 进度监控和统计

创建带进度监控的分页加载器：

```csharp
public class ProgressAwarePaginationProvider<TRequest, TData> : PaginationDataProviderBase<TRequest, TData>
{
    private readonly PaginationDataProviderBase<TRequest, TData> _innerProvider;
    private readonly IProgress<PaginationProgress>? _progress;
    
    public ProgressAwarePaginationProvider(
        PaginationDataProviderBase<TRequest, TData> innerProvider,
        ILogger logger,
        IProgress<PaginationProgress>? progress = null) : base(logger)
    {
        _innerProvider = innerProvider;
        _progress = progress;
    }
    
    protected override async Task<PaginationResponse<TData>> FetchPageAsync(
        PaginationRequest<TRequest> request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            var response = await _innerProvider.FetchPageAsync(request, cancellationToken);
            
            var duration = DateTime.UtcNow - startTime;
            
            // 报告进度
            _progress?.Report(new PaginationProgress
            {
                CurrentOffset = request.Offset,
                PageSize = request.Limit,
                ItemsInCurrentPage = response.Data?.Count ?? 0,
                HasMore = response.HasMore,
                IsSuccess = response.IsSuccess,
                Duration = duration,
                Identifier = request.Identifier
            });
            
            return response;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            _progress?.Report(new PaginationProgress
            {
                CurrentOffset = request.Offset,
                PageSize = request.Limit,
                ItemsInCurrentPage = 0,
                HasMore = false,
                IsSuccess = false,
                Duration = duration,
                ErrorMessage = ex.Message,
                Identifier = request.Identifier
            });
            
            throw;
        }
    }
}

public class PaginationProgress
{
    public int CurrentOffset { get; set; }
    public int PageSize { get; set; }
    public int ItemsInCurrentPage { get; set; }
    public bool HasMore { get; set; }
    public bool IsSuccess { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Identifier { get; set; }
}
```

## 实际应用场景

### 1. 电商产品同步

```csharp
public class ProductSyncService
{
    private readonly ProductApiProvider _productProvider;
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductSyncService> _logger;
    
    public ProductSyncService(
        ProductApiProvider productProvider,
        IProductRepository repository,
        ILogger<ProductSyncService> logger)
    {
        _productProvider = productProvider;
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<SyncResult> SyncAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var request = new HttpPaginationRequest
        {
            BaseUrl = "/products",
            QueryParameters = new Dictionary<string, string>
            {
                ["include_inactive"] = "false",
                ["updated_since"] = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
            }
        };
        
        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 100,
            MaxTotalItems = 0, // 不限制总数
            MaxRequests = 1000,
            DelayBetweenRequests = 500 // 避免触发API限流
        };
        
        var progress = new Progress<PaginationProgress>(p =>
        {
            _logger.LogInformation(
                "同步进度: 偏移量 {Offset}, 当前页 {Count} 项, 耗时 {Duration}ms",
                p.CurrentOffset, p.ItemsInCurrentPage, p.Duration.TotalMilliseconds);
        });
        
        var progressProvider = new ProgressAwarePaginationProvider<HttpPaginationRequest, Product>(
            _productProvider, _logger, progress);
        
        try
        {
            var products = await progressProvider.LoadPaginatedDataAsync(
                request, config, "product_sync", cancellationToken);
            
            if (products == null)
            {
                return new SyncResult { Success = false, Message = "未获取到任何产品数据" };
            }
            
            // 批量更新数据库
            var batchSize = 50;
            var batches = products.Chunk(batchSize);
            var updatedCount = 0;
            
            foreach (var batch in batches)
            {
                await _repository.BulkUpsertAsync(batch, cancellationToken);
                updatedCount += batch.Length;
                
                _logger.LogDebug("已处理 {Count}/{Total} 产品", updatedCount, products.Count);
            }
            
            return new SyncResult
            {
                Success = true,
                TotalProducts = products.Count,
                UpdatedProducts = updatedCount,
                Message = $"成功同步了 {updatedCount} 个产品"
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("产品同步被取消");
            return new SyncResult { Success = false, Message = "同步操作被取消" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "产品同步失败");
            return new SyncResult { Success = false, Message = $"同步失败: {ex.Message}" };
        }
    }
}

public class SyncResult
{
    public bool Success { get; set; }
    public int TotalProducts { get; set; }
    public int UpdatedProducts { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

### 2. 日志分析系统

```csharp
public class LogAnalysisService
{
    private readonly LogFilePaginationProvider _logProvider;
    private readonly ILogger<LogAnalysisService> _logger;
    
    public async Task<AnalysisResult> AnalyzeLogsAsync(
        string logFilePath,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken = default)
    {
        var request = new LogFileRequest
        {
            FilePath = logFilePath,
            StartTime = startTime,
            EndTime = endTime,
            LogLevel = LogLevel.Error // 只分析错误日志
        };
        
        var config = new PaginationConfig
        {
            MaxItemsPerRequest = 1000,
            MaxTotalItems = 50000, // 限制最大处理量
            DelayBetweenRequests = 10 // 快速处理本地文件
        };
        
        var logs = await _logProvider.LoadPaginatedDataAsync(
            request, config, $"analysis_{Path.GetFileName(logFilePath)}", cancellationToken);
        
        if (logs == null || logs.Count == 0)
        {
            return new AnalysisResult { Message = "未找到符合条件的日志" };
        }
        
        // 分析日志
        var errorsByType = logs
            .GroupBy(log => log.ErrorType)
            .ToDictionary(g => g.Key, g => g.Count());
        
        var errorsByHour = logs
            .GroupBy(log => log.Timestamp.Hour)
            .ToDictionary(g => g.Key, g => g.Count());
        
        return new AnalysisResult
        {
            TotalErrors = logs.Count,
            ErrorsByType = errorsByType,
            ErrorsByHour = errorsByHour,
            TimeRange = $"{startTime:yyyy-MM-dd HH:mm} - {endTime:yyyy-MM-dd HH:mm}",
            Message = $"分析完成，共处理 {logs.Count} 条错误日志"
        };
    }
}
```

### 3. 数据迁移工具

```csharp
public class DataMigrationService
{
    private readonly DatabasePaginationProvider _sourceProvider;
    private readonly ITargetDatabase _targetDatabase;
    private readonly ILogger<DataMigrationService> _logger;
    
    public async Task<MigrationResult> MigrateUsersAsync(
        MigrationOptions options,
        CancellationToken cancellationToken = default)
    {
        var query = new DatabaseQuery
        {
            Filter = options.UserFilter,
            OrderBy = "id"
        };
        
        var config = new PaginationConfig
        {
            MaxItemsPerRequest = options.BatchSize,
            MaxTotalItems = options.MaxUsers,
            DelayBetweenRequests = options.DelayMs
        };
        
        var migratedCount = 0;
        var errorCount = 0;
        
        try
        {
            await foreach (var userBatch in GetUserBatchesAsync(query, config, cancellationToken))
            {
                try
                {
                    await _targetDatabase.BulkInsertUsersAsync(userBatch, cancellationToken);
                    migratedCount += userBatch.Count;
                    
                    _logger.LogInformation("已迁移 {Count} 用户，总计 {Total}", 
                        userBatch.Count, migratedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批次迁移失败，跳过 {Count} 用户", userBatch.Count);
                    errorCount += userBatch.Count;
                    
                    if (!options.ContinueOnError)
                    {
                        throw;
                    }
                }
                
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();
            }
            
            return new MigrationResult
            {
                Success = true,
                MigratedCount = migratedCount,
                ErrorCount = errorCount,
                Message = $"迁移完成: 成功 {migratedCount}, 失败 {errorCount}"
            };
        }
        catch (OperationCanceledException)
        {
            return new MigrationResult
            {
                Success = false,
                MigratedCount = migratedCount,
                ErrorCount = errorCount,
                Message = "迁移被取消"
            };
        }
    }
    
    private async IAsyncEnumerable<List<User>> GetUserBatchesAsync(
        DatabaseQuery query,
        PaginationConfig config,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var offset = 0;
        bool hasMore = true;
        
        while (hasMore && !cancellationToken.IsCancellationRequested)
        {
            var request = new PaginationRequest<DatabaseQuery>
            {
                Request = query,
                Offset = offset,
                Limit = config.MaxItemsPerRequest
            };
            
            var response = await _sourceProvider.FetchPageAsync(request, cancellationToken);
            
            if (!response.IsSuccess || response.Data == null || response.Data.Count == 0)
            {
                break;
            }
            
            yield return response.Data;
            
            hasMore = response.HasMore;
            offset = response.NextOffset ?? (offset + response.Data.Count);
            
            if (config.DelayBetweenRequests > 0)
            {
                await Task.Delay(config.DelayBetweenRequests, cancellationToken);
            }
        }
    }
}

public class MigrationOptions
{
    public string? UserFilter { get; set; }
    public int BatchSize { get; set; } = 100;
    public int MaxUsers { get; set; } = 0;
    public int DelayMs { get; set; } = 100;
    public bool ContinueOnError { get; set; } = true;
}

public class MigrationResult
{
    public bool Success { get; set; }
    public int MigratedCount { get; set; }
    public int ErrorCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

## 最佳实践

### 1. 性能优化

```csharp
public class OptimizedHttpPaginationProvider<TData> : HttpPaginationProvider<TData>
{
    private readonly IMemoryCache _cache;
    private readonly SemaphoreSlim _semaphore;
    
    public OptimizedHttpPaginationProvider(
        IMemoryCache cache,
        ILogger logger) : base(logger)
    {
        _cache = cache;
        _semaphore = new SemaphoreSlim(5, 5); // 限制并发请求数
    }
    
    protected override async Task<PaginationResponse<TData>> FetchPageAsync(
        PaginationRequest<HttpPaginationRequest> request,
        CancellationToken cancellationToken)
    {
        // 生成缓存键
        var cacheKey = GenerateCacheKey(request);
        
        // 尝试从缓存获取
        if (_cache.TryGetValue(cacheKey, out PaginationResponse<TData> cachedResponse))
        {
            _logger.LogDebug("从缓存返回数据: {CacheKey}", cacheKey);
            return cachedResponse;
        }
        
        // 限制并发请求
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            var response = await base.FetchPageAsync(request, cancellationToken);
            
            // 缓存成功的响应
            if (response.IsSuccess && response.Data != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    SlidingExpiration = TimeSpan.FromMinutes(1),
                    Priority = CacheItemPriority.Normal
                };
                
                _cache.Set(cacheKey, response, cacheOptions);
            }
            
            return response;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private string GenerateCacheKey(PaginationRequest<HttpPaginationRequest> request)
    {
        var keyParts = new List<string>
        {
            request.Request.BaseUrl,
            request.Offset.ToString(),
            request.Limit.ToString()
        };
        
        if (request.Request.QueryParameters != null)
        {
            foreach (var kvp in request.Request.QueryParameters.OrderBy(x => x.Key))
            {
                keyParts.Add($"{kvp.Key}={kvp.Value}");
            }
        }
        
        return string.Join("|", keyParts);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _semaphore?.Dispose();
        }
        base.Dispose(disposing);
    }
}
```

### 2. 错误处理和重试

```csharp
public class ResilientPaginationProvider<TRequest, TData> : PaginationDataProviderBase<TRequest, TData>
{
    private readonly PaginationDataProviderBase<TRequest, TData> _innerProvider;
    private readonly RetryPolicy _retryPolicy;
    
    public ResilientPaginationProvider(
        PaginationDataProviderBase<TRequest, TData> innerProvider,
        RetryPolicy retryPolicy,
        ILogger logger) : base(logger)
    {
        _innerProvider = innerProvider;
        _retryPolicy = retryPolicy;
    }
    
    protected override async Task<PaginationResponse<TData>> FetchPageAsync(
        PaginationRequest<TRequest> request,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;
        
        while (attempt < _retryPolicy.MaxAttempts)
        {
            try
            {
                var response = await _innerProvider.FetchPageAsync(request, cancellationToken);
                
                // 检查是否是可重试的错误
                if (!response.IsSuccess && IsRetryableError(response.ErrorMessage))
                {
                    attempt++;
                    if (attempt < _retryPolicy.MaxAttempts)
                    {
                        var delay = _retryPolicy.GetDelay(attempt);
                        _logger.LogWarning(
                            "请求失败，{Delay}ms 后重试 (第 {Attempt}/{MaxAttempts} 次): {Error}",
                            delay.TotalMilliseconds, attempt + 1, _retryPolicy.MaxAttempts, response.ErrorMessage);
                        
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                }
                
                return response;
            }
            catch (Exception ex) when (IsRetryableException(ex))
            {
                lastException = ex;
                attempt++;
                
                if (attempt < _retryPolicy.MaxAttempts)
                {
                    var delay = _retryPolicy.GetDelay(attempt);
                    _logger.LogWarning(ex,
                        "请求异常，{Delay}ms 后重试 (第 {Attempt}/{MaxAttempts} 次)",
                        delay.TotalMilliseconds, attempt + 1, _retryPolicy.MaxAttempts);
                    
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
        
        // 所有重试都失败了
        var errorMessage = lastException?.Message ?? "所有重试都失败了";
        _logger.LogError(lastException, "请求最终失败: {Error}", errorMessage);
        
        return new PaginationResponse<TData>
        {
            Data = null,
            HasMore = false,
            ErrorMessage = errorMessage
        };
    }
    
    private bool IsRetryableError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return false;
        
        var retryableErrors = new[]
        {
            "timeout", "network", "connection", "502", "503", "504"
        };
        
        return retryableErrors.Any(error => 
            errorMessage.Contains(error, StringComparison.OrdinalIgnoreCase));
    }
    
    private bool IsRetryableException(Exception ex)
    {
        return ex is HttpRequestException or TaskCanceledException or SocketException;
    }
}

public class RetryPolicy
{
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
    
    public TimeSpan GetDelay(int attempt)
    {
        return Strategy switch
        {
            RetryStrategy.FixedDelay => BaseDelay,
            RetryStrategy.LinearBackoff => TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * attempt),
            RetryStrategy.ExponentialBackoff => TimeSpan.FromMilliseconds(BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)),
            _ => BaseDelay
        };
    }
}

public enum RetryStrategy
{
    FixedDelay,
    LinearBackoff,
    ExponentialBackoff
}
```

### 3. 监控和日志

```csharp
public class MonitoredPaginationProvider<TRequest, TData> : PaginationDataProviderBase<TRequest, TData>
{
    private readonly PaginationDataProviderBase<TRequest, TData> _innerProvider;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;
    
    public MonitoredPaginationProvider(
        PaginationDataProviderBase<TRequest, TData> innerProvider,
        IMetrics metrics,
        ILogger logger) : base(logger)
    {
        _innerProvider = innerProvider;
        _metrics = metrics;
        _logger = logger;
    }
    
    protected override async Task<PaginationResponse<TData>> FetchPageAsync(
        PaginationRequest<TRequest> request,
        CancellationToken cancellationToken)
    {
        using var activity = StartActivity(request);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await _innerProvider.FetchPageAsync(request, cancellationToken);
            
            stopwatch.Stop();
            
            // 记录指标
            RecordMetrics(request, response, stopwatch.Elapsed, null);
            
            // 记录日志
            if (response.IsSuccess)
            {
                _logger.LogDebug(
                    "分页请求成功: {Identifier}, 偏移量={Offset}, 数量={Count}, 耗时={Duration}ms",
                    request.Identifier, request.Offset, response.Data?.Count ?? 0, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "分页请求失败: {Identifier}, 偏移量={Offset}, 错误={Error}, 耗时={Duration}ms",
                    request.Identifier, request.Offset, response.ErrorMessage, stopwatch.ElapsedMilliseconds);
            }
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordMetrics(request, null, stopwatch.Elapsed, ex);
            
            _logger.LogError(ex,
                "分页请求异常: {Identifier}, 偏移量={Offset}, 耗时={Duration}ms",
                request.Identifier, request.Offset, stopwatch.ElapsedMilliseconds);
            
            throw;
        }
    }
    
    private Activity? StartActivity(PaginationRequest<TRequest> request)
    {
        var activity = Activity.Current?.Source.StartActivity("pagination_fetch");
        activity?.SetTag("identifier", request.Identifier ?? "unknown");
        activity?.SetTag("offset", request.Offset.ToString());
        activity?.SetTag("limit", request.Limit.ToString());
        return activity;
    }
    
    private void RecordMetrics(
        PaginationRequest<TRequest> request,
        PaginationResponse<TData>? response,
        TimeSpan duration,
        Exception? exception)
    {
        var tags = new Dictionary<string, string>
        {
            ["identifier"] = request.Identifier ?? "unknown",
            ["success"] = (response?.IsSuccess ?? false).ToString().ToLower()
        };
        
        if (exception != null)
        {
            tags["exception_type"] = exception.GetType().Name;
        }
        
        // 记录请求计数
        _metrics.IncrementCounter("pagination_requests_total", tags);
        
        // 记录响应时间
        _metrics.RecordValue("pagination_request_duration_ms", duration.TotalMilliseconds, tags);
        
        // 记录数据量
        if (response?.Data != null)
        {
            _metrics.RecordValue("pagination_items_fetched", response.Data.Count, tags);
        }
    }
}
```

## 注意事项

### 1. 资源管理

- 及时释放 `HttpPaginationProvider` 资源，特别是 `HttpClient`
- 避免创建过多的分页提供者实例
- 合理设置超时时间和重试策略

### 2. 性能考虑

- 根据数据源特性调整批次大小
- 使用缓存减少重复请求
- 限制并发请求数量避免过载
- 监控内存使用，避免加载过大的数据集

### 3. 错误处理

- 实现适当的重试逻辑
- 区分可重试和不可重试的错误
- 提供降级策略和默认值
- 记录详细的错误日志

### 4. 测试建议

```csharp
[Fact]
public async Task PaginationProvider_ShouldHandleMultiplePages()
{
    // 模拟多页数据
    var mockProvider = new Mock<PaginationDataProviderBase<string, TestData>>();
    
    // 第一页
    mockProvider.SetupSequence(p => p.FetchPageAsync(It.IsAny<PaginationRequest<string>>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new PaginationResponse<TestData>
        {
            Data = new List<TestData> { new() { Id = 1 }, new() { Id = 2 } },
            HasMore = true,
            NextOffset = 2
        })
        // 第二页
        .ReturnsAsync(new PaginationResponse<TestData>
        {
            Data = new List<TestData> { new() { Id = 3 } },
            HasMore = false
        });
    
    var config = new PaginationConfig { MaxItemsPerRequest = 2 };
    var result = await mockProvider.Object.LoadPaginatedDataAsync("test", config);
    
    Assert.NotNull(result);
    Assert.Equal(3, result.Count);
    Assert.Equal(1, result[0].Id);
    Assert.Equal(3, result[2].Id);
}
```

## 总结

MiCake 分页加载器提供了强大而灵活的数据分页处理能力，支持多种数据源和丰富的配置选项。通过合理的使用和配置，可以有效处理大规模数据的加载和处理需求。记住要根据实际场景选择合适的配置参数，并做好错误处理和性能监控。