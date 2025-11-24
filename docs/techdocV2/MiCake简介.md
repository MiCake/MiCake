# MiCake 简介

## 什么是 MiCake？

**MiCake**（米蛋糕）是一个基于 .NET Standard 开发的轻量级领域驱动设计（DDD）工具包。它旨在帮助开发者快速将现有项目转换为 DDD 风格，同时保持代码的简洁性和灵活性。

## 核心特点

### 🚀 快捷
快速将您的项目转换为DDD风格，让您更专注于领域代码的编写，而不是框架的配置。

### 📐 规范
实现了DDD战术模式提出的几乎所有部件：
- 实体（Entity）
- 值对象（Value Object）
- 聚合根（Aggregate Root）
- 仓储（Repository）
- 领域事件（Domain Event）
- 领域服务（Domain Service）
- 工作单元（Unit of Work）

### 🎯 方便
提供项目常用的基础功能：
- 全局异常处理
- 统一数据返回格式
- 自动审计功能
- 软删除支持
- 依赖注入增强

### 🔄 灵活
采用模块化设计，您可以根据自身需求选择特定的 MiCake 模块进行使用，不需要全部引入。

### 🪶 轻柔
"轻柔"是 MiCake 的核心设计理念。它无感地融入现有项目，不会强制改变您的编程习惯。当您不使用 DDD 风格时，甚至感觉不到它的存在。

## 设计理念

MiCake 在设计之初就被定位为**"很薄的一层"**：

- **非侵入式**：它包裹 .NET 项目但并不干扰，您仍然可以使用原有的编程习惯
- **不是框架**：MiCake 不会约束您的开发风格，它只是提供工具
- **不是 DDD**：它只是让您更好地践行 DDD，而不是强制使用 DDD

## 技术架构

MiCake 采用模块化架构，主要包含以下核心包：

- **MiCake.Core**：核心功能，包括模块系统、依赖注入、异常处理等
- **MiCake**：DDD 核心组件，包括实体、值对象、聚合根、仓储等
- **MiCake.AspNetCore**：ASP.NET Core 集成，包括统一返回、数据包装等
- **MiCake.EntityFrameworkCore**：Entity Framework Core 集成，提供仓储实现

## 适用场景

MiCake 适合以下场景：

- 希望在 .NET 项目中实践领域驱动设计
- 需要快速搭建具有 DDD 架构的新项目
- 希望将现有项目逐步重构为 DDD 风格
- 需要一个轻量级的 DDD 工具包，而不是重量级框架

## 环境要求

- **.NET Core 5.0** 及以上版本
- **Visual Studio 2019** 或更高版本（推荐）
- 支持跨平台开发（Windows、Linux、macOS）

## 开源协议

MiCake 使用 MIT 开源协议，您可以自由地在商业和非商业项目中使用。

## 社区与支持

- **GitHub**：[https://github.com/MiCake/MiCake](https://github.com/MiCake/MiCake)
- **官方网站**：[http://www.micake.net](http://www.micake.net)
- **NuGet**：搜索 "MiCake" 查看所有可用包

## 下一步

阅读[快速开始](./快速开始.md)，了解如何在您的项目中安装和使用 MiCake。
