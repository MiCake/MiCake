# MiCake Framework - 优先级修复清单

**创建日期**: 2025-11-22  
**基于**: 综合深度分析报告  
**状态追踪**: 待修复 → 进行中 → 已完成

---

## 🔴 P0 - 严重 (立即修复 - 24小时内)

### 1. DataDepositPool DoS漏洞
- **文件**: `MiCake.Core/Util/Store/DataDepositPool.cs`
- **风险**: 拒绝服务攻击、内存耗尽
- **预计工作量**: 4-6小时
- **状态**: ⬜ 待修复
- **负责人**: ___________

**修复内容**:
- [ ] 添加键名称长度限制 (MaxKeyLength = 256)
- [ ] 添加对象大小限制 (MaxObjectSizeBytes = 1MB)
- [ ] 实现键名称验证 (防止特殊字符注入)
- [ ] 添加LRU驱逐策略
- [ ] 估算对象大小
- [ ] 编写安全测试用例

**测试清单**:
- [ ] 测试超长键名被拒绝
- [ ] 测试特殊字符键名被拒绝
- [ ] 测试大对象被拒绝
- [ ] 测试容量满时自动驱逐
- [ ] 负载测试 (1000并发写入)

---

### 2. EmitHelper动态类型创建无限制
- **文件**: `MiCake.Core/Util/Reflection/Emit/EmitHelper.cs`
- **风险**: 内存泄漏、代码注入
- **预计工作量**: 4-6小时
- **状态**: ⬜ 待修复
- **负责人**: ___________

**修复内容**:
- [ ] 添加动态类型生成限制 (MaxDynamicTypesPerAssembly = 100)
- [ ] 验证类名格式 (使用CodeGenerator.IsValidLanguageIndependentIdentifier)
- [ ] 验证基类型白名单
- [ ] 防止继承系统关键类型
- [ ] 添加类型生成计数器
- [ ] 编写安全测试用例

**测试清单**:
- [ ] 测试无效类名被拒绝
- [ ] 测试非法基类型被拒绝
- [ ] 测试类型生成限制
- [ ] 测试长时间运行不泄漏内存

---

### 3. ResponseWrapperExecutor每次创建工厂
- **文件**: `MiCake.AspNetCore/Responses/Internals/ResponseWrapperExecutor.cs`
- **风险**: 高并发下性能瓶颈
- **预计工作量**: 2-3小时
- **状态**: ⬜ 待修复
- **负责人**: ___________

**修复内容**:
- [ ] 使用Lazy<T>缓存工厂实例
- [ ] 确保线程安全 (LazyThreadSafetyMode.ExecutionAndPublication)
- [ ] 更新WrapSuccess方法使用缓存
- [ ] 更新WrapError方法使用缓存
- [ ] 性能基准测试

**测试清单**:
- [ ] 单元测试工厂只创建一次
- [ ] 并发测试 (1000请求/秒)
- [ ] 基准测试对比优化前后
- [ ] 验证功能不变

---

## 🟡 P1 - 高优先级 (本周内修复)

### 4. UnitOfWork事件处理器异常被吞咽
- **文件**: `MiCake/DDD/Uow/Internal/UnitOfWork.cs`
- **风险**: 数据一致性、监控盲区
- **预计工作量**: 6-8小时
- **状态**: ⬜ 待修复
- **负责人**: ___________

**修复内容**:
- [ ] 添加EventErrorHandlingStrategy枚举
- [ ] 添加CriticalEventTypes配置
- [ ] 实现事件异常收集机制
- [ ] 更新RaiseEvent方法支持策略
- [ ] 在CommitAsync结束时检查收集的异常
- [ ] 编写测试用例

**测试清单**:
- [ ] 测试LogAndContinue策略
- [ ] 测试FailFast策略
- [ ] 测试Collect策略
- [ ] 测试关键事件失败立即抛出
- [ ] 测试非关键事件失败收集后抛出

---

### 5. EFCoreDbContextWrapper资源泄漏风险
- **文件**: `MiCake.EntityFrameworkCore/Uow/EFCoreDbContextWrapper.cs`
- **风险**: 连接泄漏、内存泄漏
- **预计工作量**: 4-5小时
- **状态**: ⬜ 待修复
- **负责人**: ___________

**修复内容**:
- [ ] 实现完整的Dispose模式
- [ ] 添加Dispose(bool disposing)方法
- [ ] 添加析构函数
- [ ] 调用GC.SuppressFinalize
- [ ] 分离DisposeTransaction和DisposeDbContext逻辑
- [ ] 改进异常日志

**测试清单**:
- [ ] 测试重复Dispose不抛异常
- [ ] 测试transaction正确释放
- [ ] 测试DbContext按需释放
- [ ] 内存泄漏测试 (长时间运行)

---

### 6. TakeOutByType线性遍历性能问题
- **文件**: `MiCake.Core/Util/Store/DataDepositPool.cs`
- **风险**: 大数据集性能差
- **预计工作量**: 5-6小时
- **状态**: ⬜ 待修复
- **负责人**: ___________

**修复内容**:
- [ ] 添加TypeIndex类型索引
- [ ] 修改PoolEntry包含DataType
- [ ] 在Deposit时更新类型索引
- [ ] 优化TakeOutByType使用索引
- [ ] 处理派生类型查询
- [ ] 性能基准测试

**测试清单**:
- [ ] 测试精确类型查找
- [ ] 测试派生类型查找
- [ ] 性能测试 (1000+项目)
- [ ] 基准测试对比优化前后

---

## 🟢 P2 - 中优先级 (本月内修复)

### 7. 类型转换异常处理
- **文件**: `MiCake.Core/Util/Store/DataDepositPool.cs:66-73`
- **风险**: 运行时异常、信息泄漏
- **预计工作量**: 3-4小时
- **状态**: ⬜ 待修复

**修复内容**:
- [ ] 添加TryConvert<T>方法
- [ ] 使用'is'模式匹配
- [ ] 添加IConvertible支持
- [ ] 改进异常消息
- [ ] 编写测试用例

---

### 8. DataDepositPool Dispose模式不符合规范
- **文件**: `MiCake.Core/Util/Store/DataDepositPool.cs:139-147`
- **风险**: 违反.NET规范
- **预计工作量**: 2-3小时
- **状态**: ⬜ 待修复

**修复内容**:
- [ ] 实现标准Dispose模式
- [ ] Dispose方法幂等性
- [ ] 添加GC.SuppressFinalize
- [ ] 添加析构函数
- [ ] 更新其他方法检查_disposed

---

### 9. API命名和文档改进
- **文件**: 多个文件
- **风险**: 易用性差、理解困难
- **预计工作量**: 8-10小时
- **状态**: ⬜ 待修复

**修复内容**:
- [ ] Release方法重命名为Clear
- [ ] 添加Remove方法
- [ ] 添加RemoveWhere方法
- [ ] 添加ContainsKey方法
- [ ] 添加GetStatistics方法
- [ ] 所有公共API添加XML文档和示例

---

## ⚪ P3 - 低优先级 (持续改进)

### 10. 哈希算法优化
- **文件**: `MiCake.Core/Util/Reflection/CompiledActivator.cs:59-70`
- **预计工作量**: 1小时
- **状态**: ⬜ 待修复

**修复内容**:
- [ ] 使用HashCode结构体
- [ ] 替换简单乘法哈希

---

### 11. 字典查找优化
- **文件**: `MiCake.Core/Util/Store/DataDepositPool.cs:119-128`
- **预计工作量**: 1小时
- **状态**: ⬜ 待修复

**修复内容**:
- [ ] 使用TryGetValue替代ContainsKey + []
- [ ] 减少字典查找次数

---

### 12. SOLID原则重构
- **文件**: `MiCake.Core/Util/Store/DataDepositPool.cs`
- **预计工作量**: 10-12小时
- **状态**: ⬜ 待修复

**修复内容**:
- [ ] 提取TypeIndex类
- [ ] 提取CapacityManager类
- [ ] 分离关注点
- [ ] 改进可测试性

---

## 🔍 代码审查检查清单

在提交PR之前,确保:

### 安全性
- [ ] 所有用户输入都经过验证
- [ ] 没有信息泄漏在异常消息中
- [ ] 没有潜在的DoS向量
- [ ] 资源限制已实施

### 性能
- [ ] 关键路径已进行基准测试
- [ ] 没有不必要的分配
- [ ] 缓存适当使用
- [ ] 算法复杂度合理

### 质量
- [ ] 所有公共API有XML文档
- [ ] 代码有适当的注释
- [ ] 遵循命名规范
- [ ] Dispose模式正确实现

### 测试
- [ ] 单元测试覆盖率 > 80%
- [ ] 边界条件已测试
- [ ] 并发场景已测试
- [ ] 性能回归测试已通过

---

## 📊 进度追踪

### 本周目标
- [ ] 完成所有P0项目
- [ ] 完成至少2个P1项目

### 本月目标
- [ ] 完成所有P0和P1项目
- [ ] 完成至少50%的P2项目

### 本季度目标
- [ ] 完成所有P0、P1、P2项目
- [ ] 完成至少50%的P3项目

---

## 📈 度量指标

### 安全性指标
- 关键安全漏洞: **0** (目标)
- 高危安全漏洞: **0** (目标)
- 中危安全漏洞: **< 5** (目标)

### 性能指标
- P95响应时间: **< 100ms** (目标)
- 内存使用增长率: **< 1% per hour** (目标)
- 缓存命中率: **> 80%** (目标)

### 质量指标
- 代码覆盖率: **> 80%** (目标)
- 代码重复率: **< 5%** (目标)
- 圈复杂度: **< 10** (目标，关键方法)

---

**更新日期**: 2025-11-22  
**下次审查**: 2025-11-29
