<p align="center">
  <a href="http://www.micake.net">
    <img width="180px" src="assets/logo.svg">
  </a>
</p>

<h1 align="center" >MiCake</h1>

<div align="center">

一款基于.Net Core平台的 **“超轻柔”** 领域驱动设计（DDD）组件

[![Nuget Version](https://img.shields.io/nuget/v/MiCake.Core?label=nuget%20version&logo=nuget)](https://www.nuget.org/packages/MiCake.Core/) [![Nuget Downloads](https://img.shields.io/nuget/dt/MiCake.Core?color=green&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/MiCake.Core/) [![Maintainability](https://api.codeclimate.com/v1/badges/a9d8163cb3023fdef30a/maintainability)](https://codeclimate.com/github/uoyoCsharp/MiCake/maintainability) [![Build Status](https://dev.azure.com/MiCakeOrg/MiCake/_apis/build/status/uoyoCsharp.MiCake?branchName=master)](https://dev.azure.com/MiCakeOrg/MiCake/_build/latest?definitionId=3&branchName=master) [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/MiCakeOrg/MiCake/3?color=ff69b4&label=Azure%20Tests&logo=Microsoft-Azure&logoColor=white)](https://dev.azure.com/MiCakeOrg/MiCake/_build/latest?definitionId=3&branchName=master) [![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/MiCakeOrg/MiCake/3?label=Azure%20Coverage&logo=Azure-DevOps)](https://dev.azure.com/MiCakeOrg/MiCake/_build/latest?definitionId=3&branchName=master) [![Board Status](https://dev.azure.com/MiCakeOrg/e359a201-ca49-495f-92ba-11493e88e94e/9a202286-9c70-40fa-8892-9bd476191d74/_apis/work/boardbadge/e5dd9abe-6df7-4f1c-95d0-762074a5f1e2)](https://dev.azure.com/MiCakeOrg/MiCake/_workitems/recentlyupdated/)

</div>

## 🍡 特点

- “快捷” —— 快速将您的项目转换为DDD风格，让您更专注于您的领域代码。
- “规范” —— 实现了DDD战术模式提出的几乎所有部件，便于使用领域驱动思想来规范项目。
- “方便” —— 提供项目常用的基础功能（全局异常处理，数据格式化等），便于快速构建项目。
- “灵活” —— 通过模块进行解耦，您可以根据自身需求使用特定的MiCake模块。
- “轻柔” —— 无感的融入现有的项目，甚至感觉不到它的存在。

## 🍧 简介

`MiCake`（中文名我更喜欢叫它为“米蛋糕”😜）是基于 `.Net Standard` 所开发的领域驱动设计（DDD）工具包。

您只需要通过 `NuGet` 包安装它，并且编写非常少量的代码就能快速使您的项目转变为**DDD**风格。

它提供了DDD战术模式中的大部分部件，比如**聚合根、实体、值对象、领域服务**等等，通过这些部件建立您的“领域对象”，将开发重心放在领域层中，其它大部分的交互逻辑都将由`MiCake`来帮您完成。

**“轻柔”**的**“组件”**？ `MiCake`在设计之初就被定位为“很薄的一层”，它包裹 `.NET` 项目但并不干扰，您仍然可以使用原有的编程习惯进行开发。

当不使用`DDD`风格时，您甚至都感觉不到它的存在。它很轻，轻到可以忽略；它不是一个“框架”，不会约束您的开发风格；它不是`DDD`，它只是让您更好的践行`DDD`。

`MiCake`的核心是提供领域驱动设计（DDD）的功能，但同时还提供了其它的扩展功能便于您更快速的构建出应用程序：比如依赖注入、自动审计、全局异常处理等等功能。

## 🍒 用法

### 所需环境版本

+ .NET Core 3.0及以上版本
+ Visual Studio 2019

在您的`Asp Net Core`项目中通过`NuGet`安装`MiCake.AspNetCore.Start`：

```powershell
Install-Package MiCake.AspNetCore.Start
```

新增一个叫做`MyEntryModule.cs`的文件，该类的作用是告诉`MiCake`该从哪个程序集启动：

```csharp
public class MyEntryModule : MiCakeModule
{
}
```

将您的DbContext继承自`MiCakeDbContext`:

```csharp
public class MyDbContext : MiCakeDbContext
{
    public MyDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //不要删除该行代码
        base.OnModelCreating(modelBuilder);
    }
}
```

在`Startup.cs`中添加`MiCake`服务：

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ………………

   //添加该代码 用于配置MiCake
   services.AddMiCakeWithDefault<MyDbContext, MyEntryModule>()
           .Build();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    …………

    //添加该代码 用于启动MiCake。确保该代码位于UseEndpoints之前
    app.StartMiCake();
    app.UseEndpoints(…………);
}
```

是的，就是如此简单。`AddMiCakeWithDefault`是`MiCake`所提供的基础使用方案，您可以通过查阅文档来获取更丰富的使用方法。

文档中心提供了一篇[《Wiki - 搭建起步程序》](https://github.com/uoyoCsharp/MiCake/wiki/%E8%B5%B7%E6%AD%A5)来介绍如何使用`MiCake`，也许您可以从中获取一些帮助。

## 🍉 文档

点击跳转至：[文档中心](https://github.com/uoyoCsharp/MiCake/wiki)。

## 🍊 示例项目

您可能会对大量的文字教程而感到枯燥，因此我们提供了以下的几个演示项目供您参考：

+ **预约星** (<font color="red">Coming Soon</font>)
+ **旅人帐** (<font color="red">Coming Soon</font>)

`MiCake.Samples`仓库中放置了一些`MiCake`所公开的示例项目和实验性项目，您可以跳转至[该仓库](https://github.com/uoyoCsharp/MiCake.Samples)进行查阅.

## 🍍 当前版本

| Nuget Package              | 版本信息                                                                                                        | 描述                            |
| -------------------------- | --------------------------------------------------------------------------------------------------------------- | ------------------------------- |
| MiCake.Core                | ![Nuget](https://img.shields.io/nuget/v/MiCake.Core?label=MiCake.Core&logo=nuget)                               | MiCake 核心程序集               |
| MiCake.DDD.Domain          | ![Nuget](https://img.shields.io/nuget/v/MiCake.DDD.Domain?label=MiCake.DDD.Domain&logo=nuget)                   | MiCake 对DDD领域层的实现程序集  |
| MiCake.Core.Util           | ![Nuget](https://img.shields.io/nuget/v/MiCake.Core.Util?label=MiCake.Core.Util&logo=nuget)                     | MiCake 提供的工具类程序集       |
| MiCake.EntityFrameworkCore | ![Nuget](https://img.shields.io/nuget/v/MiCake.EntityFrameworkCore?label=MiCake.EntityFrameworkCore&logo=nuget) | MiCake 对EFCore的支持程序集     |
| MiCake.AspNetCore          | ![Nuget](https://img.shields.io/nuget/v/MiCake.AspNetCore?label=MiCake.AspNetCore&logo=nuget)                   | MiCake 对AspNetCore的支持程序集 |
| MiCake.AspNetCore.Start    | ![Nuget](https://img.shields.io/nuget/v/MiCake.AspNetCore.Start?label=MiCake.AspNetCore.Start&logo=nuget)       | MiCake 搭建起步程序所用的程序集 |

更多：请跳转至[NuGet官网](https://www.nuget.org/packages?q=micake),进行查阅。

## 🍠 贡献与帮助

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/uoyoCsharp/MiCake/pulls)

如果您有什么好的想法和观点，欢迎通过 [Pull Request](https://github.com/uoyoCsharp/MiCake/pulls) 进行贡献，或通过 [提交 issues](https://github.com/uoyoCsharp/MiCake/issues/new)  来反馈您在使用过程中所发现的BUG。（ *期待得到您的反馈~* 🌻🌻）

## 🍑 联系

[![博客园](https://img.shields.io/badge/%E5%8D%9A%E5%AE%A2%E5%9B%AD-%E5%8F%A5%E5%B9%BD-blue)](https://www.cnblogs.com/uoyo/)

如果您喜欢关于 `.NET ` 方面的内容，或者对领域驱动很感兴趣，欢迎您关注我的博客：[句幽的博客](https://www.cnblogs.com/uoyo/)。您可以通过博客园内的站内**短消息**来与我沟通有关编程方面的问题。

![QQ:344481481](https://img.shields.io/badge/QQ:344481481-Online-green)

如果您愿意与我沟通一些其它方面（*非编程方向*）的事情，欢迎添加QQ好友。🌻🌻

## 🍄 最后

备战 `.NET 5` 。**冲鸭！！** 🐣