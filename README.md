<p align="center">
  <a href="https://micake.github.io">
    <img width="180px" src="assets/readme_logo.png">
  </a>
</p>

<h1 align="center">MiCake</h1>

<div align="center">

基于 .NET 的轻量级领域驱动设计（DDD）工具包

[![Nuget Version](https://img.shields.io/nuget/v/MiCake?label=nuget%20version&logo=nuget)](https://www.nuget.org/packages/MiCake/) [![Nuget Downloads](https://img.shields.io/nuget/dt/MiCake?color=green&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/MiCake/) [![Build Status](https://github.com/MiCake/MiCake/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/MiCake/MiCake/actions/workflows/build-and-release.yml) [![codecov](https://codecov.io/gh/MiCake/MiCake/branch/master/graph/badge.svg)](https://codecov.io/gh/MiCake/MiCake) [![License](https://img.shields.io/github/license/MiCake/MiCake)](https://github.com/MiCake/MiCake/blob/master/LICENSE)

</div>

---

## 什么是 MiCake？

MiCake 是一个基于 .NET 开发的轻量级领域驱动设计（DDD）工具包。它旨在帮助开发者快速将现有项目转换为 DDD 风格，同时保持代码的简洁性和灵活性。

MiCake 不是一个传统意义上的"框架"，而是一套便利的"工具集"。它提供了实践 DDD 所需的各种组件和工具，但不会强制改变你的开发习惯。

## 快速开始

迫不及待想要使用 MiCake？我们提供了成熟的模板和示例项目，你只需要使用`dotnet new`命令就能创建一个开箱即用，由 MiCake 驱动的`ASP.NET Core`项目。

详细的入门指南和使用文档，请访问： 📚 **[在线完整文档 - 快速开始](https://micake.github.io)** 

## 核心特性

### 🚀 快捷
快速将项目转换为 DDD 风格，专注于领域代码的编写而非框架配置。通过简单的配置和少量代码，即可将现有项目升级为 DDD 架构。

### 📐 规范
实现了 DDD 战术模式的核心部件：

- **实体（Entity）** - 具有唯一标识的领域对象
- **值对象（Value Object）** - 通过属性值比较的不可变对象
- **聚合根（Aggregate Root）** - 聚合的根实体，作为外部访问的入口
- **仓储（Repository）** - 提供聚合根的持久化操作
- **领域事件（Domain Event）** - 捕获领域中发生的重要事件
- **领域服务（Domain Service）** - 封装不属于实体或值对象的领域逻辑
- **工作单元（Unit of Work）** - 管理业务事务和数据一致性

### 🎯 方便
提供项目开发常用的基础功能：

- 全局异常处理
- 统一数据返回格式
- 自动审计功能
- 软删除支持
- 依赖注入增强
- API 请求日志记录
- 丰富的工具集（缓存、转换器、查询、熔断器等）

### 🪶 轻柔
"轻柔"是 MiCake 的核心设计理念：

- **非侵入式** - 无感融入现有项目，不改变编程习惯
- **低耦合** - 框架代码与业务代码清晰分离
- **可选使用** - 不强制使用 DDD，可逐步引入
- **几乎无感** - 不使用 DDD 特性时，甚至感觉不到它的存在

## 适用场景

- ✅ 新项目开发，希望使用 DDD 方法构建清晰架构
- ✅ 现有项目重构，逐步引入 DDD 改善可维护性
- ✅ 中小型企业应用，需要良好架构但不想引入重量级框架
- ✅ 学习和实践 DDD，需要轻量级的参考实现

## 环境要求

- .NET 10.0 及以上版本
- Visual Studio 2022+ / Visual Studio Code / Rider

## NuGet 包

| 包名                       | 版本                                                                           | 说明                  |
| -------------------------- | ------------------------------------------------------------------------------ | --------------------- |
| MiCake                     | ![Nuget](https://img.shields.io/nuget/v/MiCake?logo=nuget)                     | DDD 核心组件          |
| MiCake.EntityFrameworkCore | ![Nuget](https://img.shields.io/nuget/v/MiCake.EntityFrameworkCore?logo=nuget) | EF Core 集成支持      |
| MiCake.AspNetCore          | ![Nuget](https://img.shields.io/nuget/v/MiCake.AspNetCore?logo=nuget)          | ASP.NET Core 集成支持 |

更多包信息请访问 [NuGet.org](https://www.nuget.org/packages?q=micake)

## 贡献

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/MiCake/MiCake/pulls)

我们欢迎社区贡献！

- 💡 [提交问题](https://github.com/MiCake/MiCake/issues/new) - 报告 Bug 或功能建议
- 🔧 [提交 PR](https://github.com/MiCake/MiCake/pulls) - 改进代码或文档
- 💬 [参与讨论](https://github.com/MiCake/MiCake/discussions) - 分享经验和最佳实践

## 社区与支持

- 📖 [官方文档](https://micake.github.io)
- 💻 [GitHub 仓库](https://github.com/MiCake/MiCake)
- 📝 [作者博客](https://www.cnblogs.com/uoyo/)
- 📦 [NuGet 包](https://www.nuget.org/packages?q=micake)

---

<div align="center">

**MiCake** - 让 DDD 更简单

</div>
