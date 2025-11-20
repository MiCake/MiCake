
## 优先级说明（使用术语）
- Critical（紧急）= 必须立即修复（安全或关键性能问题）
- High（高优先级）= 重要问题，应优先修复
- Medium（中优先级）= 需要修复但可以计划到下个版本
- Low（低优先级）= 建议优化或长期改进

---

## 按优先级排序的待优化项（主表）
> 列标题：优先级 | 分类 | 问题标题 | 位置/文件 | 简短描述 | 推荐修复（概述）

| 优先级 | 分类 | 问题标题 | 位置 / 文件 | 简要描述 | 推荐修复（概述） |
|---|---|---|---|---|---|
| Critical | Security | 错误响应暴露堆栈跟踪 | `MiCake.AspNetCore/DataWrapper/ErrorResponse.cs` | 错误响应包含 `StackTrace` 字段，可能暴露敏感信息 | 增加 `DataWrapperOptions` 配置：默认不返回堆栈，允许在 Dev 环境显示，确保生产环境禁用。 |
| Critical | Security / Performance | 过滤扩展（FilterExtensions）类型混淆 & 注入风险 | `MiCake.Core/Util/LinqFilter/Extensions/FilterExtensions.cs` | 未验证属性/值，类型转换异常可能导致服务中断或注入风险 | 添加：属性白名单、输入验证、健壮类型转换逻辑、异常捕获并返回友好错误；编写单元测试覆盖异常路径。 |
| Critical | Performance | N+1 查询风险（EFRepository） | `MiCake.EntityFrameworkCore/Repository/EFRepository.cs` | 仓储未提供 Include 或投影支持，易导致 N+1 查询 | 在仓储接口添加 includeFunc / projection 支持，提供最佳实践与文档，并增加测试案例。 |
| High | Security | 反射创建对象（CompiledActivator）危险 | `MiCake.Core/Util/Reflection/CompiledActivator.cs` | 无类型白名单或禁止类型检查，若 type 值来自不可信源存在风险 | 引入类型白名单/黑名单、命名空间限制、禁止危险类型（Process、File 等），并记录错误。 |
| High | Security/Performance | CompiledActivator 缓存无限制（内存泄漏风险） | `MiCake.Core/Util/Reflection/CompiledActivator.cs` | `_factoryCache` 无上限地增长，长期运行会耗尽内存 | 将缓存转为有限大小（LRU）并实现驱逐策略或使用 ConcurrentLRU。添加监控/metric。 |
| High | Performance | BoundedLruCache 并发竞态（GetOrAdd） | `MiCake.Core/Util/Cache/BoundedLruCache.cs` | valueFactory 在未锁内调用，可能被并发多次创建并产生副作用 | 使用双重检查锁或在锁内构造 value（或使用 Concurrent collections），完善异常/Dispose 检查。 |
| High | Performance | LRU 更新锁竞争（BoundedLruCache） | `MiCake.Core/Util/Cache/BoundedLruCache.cs` | 每次命中都会锁 LRU，导致高并发下热点争用 | 改为分段锁或周期性异步 LRU 更新（减少频繁锁），或使用 lock-free 结构。 |
| High | Security/Robustness | UnitOfWorkFilter 异常回滚的上下文缺失（与方法过长） | `MiCake.AspNetCore/Uow/UnitOfWorkFilter.cs` | 回滚异常被遮盖或丢失上下文；方法超长，难维护 | 把长方法拆分（提取 ShouldApplyUow、ExecuteWithUow 等），在 rollback 失败时使用 AggregateException 并保持日志上下文。 |
| High | Security | HttpPaginationProvider 缺乏 HttpClient 安全校验 | `MiCake.Core/Util/Paging/Providers/HttpPaginationProvider.cs` | 允许注入无安全配置的 HttpClient（例如无限 timeout/禁用证书） | 验证 HttpClient：确保 timeout、默认 headers、证书验证、禁止危险 handler（如无验证的 TLS）。 |
| Medium | Usability / Maintainability | FilterExtensions 复杂难测（表达式构建） | `MiCake.Core/Util/LinqFilter/Extensions/FilterExtensions.cs` | 代码复杂：长方法、多分支、类型转换、可测试性差 | 抽离表达式构建器（FilterExpressionBuilder），添加单元测试与行为测试，增加输入验证。 |
| Medium | Usability / Testability | CompiledActivator 单元测试困难 & 责任单一 | `MiCake.Core/Util/Reflection/CompiledActivator.cs` | 为方便测试，避免静态方法/全球状态 | 抽象为接口 `IActivator` 并提供默认实现，避免静态难测行为；拆分参数化与非参数化激活器；注入缓存策略。 |
| Medium | Performance | 域事件集合总是分配 List（内存开销） | `MiCake/DDD/Domain/Entity.cs` | 每个实体总是 allocate List，即使不使用 domain events | 改为延迟创建（null -> new when needed），只在有事件时才 allocate。 |
| Medium | Usability | 异步命名规范（Async）与 CancellationToken 不一致 | 多处 | Async 方法缺 `Async` 后缀或缺 `CancellationToken` 参数 | 全面修正命名策略、确保异步方法返回 Task/Task<T> 并支持 CancellationToken。 |
| Medium | Usability | 配置模式不一致（选项 / builder） | 多处（`MiCakeAspNetOptions` 与 `MiCakeApplicationOptions`） | 不同模块使用不同配置方式，开发者体验不一致 | 标准化 Options 模式 + `IValidateOptions` + 提供 Fluent Builder/扩展方法与示例。 |
| Medium | Usability/Extensibility | Sealed 类与扩展点不足 | `BoundedLruCache` 等 | 不必要 seal 的类降低扩展性 | 除非有明确原因，否则避免 sealing，或提供扩展点（protected virtual）或接口替代。 |
| Medium | Documentation | 缺少 Getting Started、使用示例及性能指南 | docs（缺失文件） | 文档不完整，缺少上手和性能建议 | 新增 docs 目录：Getting Started、Architectural Overview、Best Practices、Performance Guidelines、Examples。 |
| Medium | Maintainability | 代码组织不一致（Util 目录过宽） | `MiCake.Core/Util/*` | Util 变成 catch-all，难维护 | 重构目录：Cashing/Resilience/Querying/Extensions 等；将 Abstractions 与 Implementations 分离。 |
| Low | Maintainability | 异常处理重复 -> 抽取通用 executor | 多处（UnitOfWorkFilter、HttpPaginationProvider 等） | 相似的 try/catch/log 重复代码 | 抽取共用 `ExceptionHandler.ExecuteWithLoggingAsync` 工具并在关键处采用。 |
| Low | Security/Logging | 日志与异常返回信息太冗长或不一致 | 多处 | 记录过多内部信息到日志或未分类 | 改进日志分类并 sanitize 用户可见错误，区分用户/开发者日志。 |
| Low | Usability | 未启用 Nullable Reference Types | 多项目 csproj | 代码未全面启用 NRT | 打开 `<Nullable>enable</Nullable>`，并逐步修正警告（计划性迁移） |
| Low | Performance | BuildCacheKey 字符串拼接低效 | `CompiledActivator` | 使用字符串拼接/FullName 而非 tuple key | 改用 `(Type, Type[])` tuple 作为 key 并实现类型数组比较器以提高效率。 |
| Low | Maintainability | 复杂表达式中的 Boxing / Unboxing | 多处（FilterExtensions 等） | 中小型性能问题 | 可重构用泛型/避免 object boxing 的重写，微优化。 |

---

## 建议的修复优先级动作计划（短期/中期/长期）
- 立即（短期：1-2 周）✅
  - 问题：错误响应暴露堆栈跟踪（Critical）
  - 问题：FilterExtensions 注入 & 类型转换（Critical）
  - 问题：EFRepository N+1 查询潜在问题（Critical）
  - 问题：CompiledActivator 非法类型与缓存问题（High）
  - 问题：BoundedLruCache 并发/锁竞争（High）

- 近期（中期：1-2 月）⚠️
  - UnitOfWorkFilter 的回滚上下文、重构与日志改进（High）
  - HttpPaginationProvider HttpClient 安全验证（High）
  - CompiledActivator 抽象/接口本地化（Medium）
  - FilterExtensions 重构以便可测试（Medium）
  - 文档补齐（Getting Started / Performance Guide / Examples）（Medium）
  - 启用 NRT 并修复关键警告（Low -> Medium）

- 未来（长期：季度内）🔧
  - 重新组织 utilities/目录（Medium）
  - 引入通用异常处理执行器/日志改进（Low）
  - 改善扩展点（Repository Interceptors 等）（Medium）
  - Modern C# 特性逐步迁移（Low）

---

## 额外建议（行动项与投入估计）
- 对于 Critical 类问题（安全/性能）：建议立即建立小型任务分支（hotfix 或 prioritized sprint）并配备 1-2 名开发者与 1 名安全/架构 reviewer。估计修复时间：每项 1-3 天（含测试），部分复杂项（FilterExtensions、EFRepository N+1）需更多时间（1-2 周）。
- 对于 High / Medium 项目：创建 backlog 项目并分配 PR/issue。优先保证安全相关的 High 早于 UX 改善类问题。
- 建议增加 CI 相关测试：
  - 单元测试覆盖 FilterExtensions 的各种输入（正常、类型不匹配、注入测试）
  - 性能/集成测试覆盖 EFRepository 包含情形
  - 确保将 Important security checks 作为静态分析或安全网关

---

如果你愿意，我可以接下来：
1. 将上表转换为 GitHub Issues 的优先级模板，生成建议 PR 描述或任务分配清单（按优先级与文件位置自动拆分任务）。  
2. 为 Critical 与 High 问题生成具体的代码改动建议/补丁（例如 `ErrorResponse` 和 `FilterExtensions` 的示例实现）。  
3. 或者仅导出 CSV/Markdown 表供你导入到任务工具或分配任务。

请选择下一步（例如：1、2 或 3 / 或其他需要），我将按照你的选择继续执行。 💡