<p align="center">
  <a href="http://www.micake.net">
    <img width="180px" src="assets/logo.svg">
  </a>
</p>

<h1 align="center" >MiCake</h1>

<div align="center">

一款基于.Net Core平台的 **“超轻柔”** 领域驱动（DDD）组件

[![Nuget Version](https://img.shields.io/nuget/v/MiCake.Core?label=nuget%20version&logo=nuget)](https://www.nuget.org/packages/MiCake.Core/) [![Nuget Downloads](https://img.shields.io/nuget/dt/MiCake.Core?color=green&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/MiCake.Core/) [![Build Status](https://dev.azure.com/MiCakeOrg/MiCake/_apis/build/status/uoyoCsharp.MiCakeFramework?branchName=master)](https://dev.azure.com/MiCakeOrg/MiCake/_build/latest?definitionId=1&branchName=master) [![Azure DevOps tests](https://img.shields.io/azure-devops/tests/MiCakeOrg/MiCake/2?color=ff69b4&label=Azure%20Tests&logo=Microsoft-Azure&logoColor=white)](https://dev.azure.com/MiCakeOrg/MiCake/_build/latest?definitionId=1&branchName=master) [![Azure DevOps coverage](https://img.shields.io/azure-devops/coverage/MiCakeOrg/MiCake/2?label=Azure%20Coverage&logo=Azure-DevOps)](https://dev.azure.com/MiCakeOrg/MiCake/_build/latest?definitionId=1&branchName=master) [![Board Status](https://dev.azure.com/MiCakeOrg/e359a201-ca49-495f-92ba-11493e88e94e/9a202286-9c70-40fa-8892-9bd476191d74/_apis/work/boardbadge/e5dd9abe-6df7-4f1c-95d0-762074a5f1e2)](https://dev.azure.com/MiCakeOrg/e359a201-ca49-495f-92ba-11493e88e94e/_boards/board/t/9a202286-9c70-40fa-8892-9bd476191d74/Microsoft.RequirementCategory/)

</div>

## 🍡 特点

- “快捷” —— 快速将您的项目转换为DDD风格，让您更专注于您的领域代码。
- “规范” —— 实现了DDD战术模式提出的几乎所有部件，便于使用领域驱动思想来规范项目。
- “方便” —— 提供项目常用的基础功能（全局错误捕获，返回数据包裹等），便于快速构建项目。
- “灵活” —— 通过模块进行解耦，您可以根据自身需求使用特定的MiCake模块。
- “轻柔” —— 无感的融入现有的项目，甚至感觉不到它的存在。

## 🍧 简介

`MiCake`（中文名我更喜欢叫它为“米蛋糕”😜）是基于 `.Net Standard` 所开发的领域驱动设计（DDD）工具包。您只需要通过 `NuGet` 包安装它，并且编写非常少量的代码就能快速使您的项目转变为**DDD**风格。它提供了DDD战术模式中的大部分部件，比如**聚合根、实体、值对象、领域服务**等等，通过这些部件建立您的“领域对象”，将开发重心放在领域层中，其它大部分的交互逻辑都将有`MiCake`来帮您完成。

**“轻柔”**的**“组件”**？ `MiCake`在设计之初就被定位为“很薄的一层”，它包裹 `.NET` 项目但并不干扰，您仍然可以使用原有的编程习惯进行开发。当不使用`DDD`风格时，您甚至都感觉不到它的存在。它很轻，轻到可以忽略；它不是一个“框架”，不会约束您的开发风格；它不是`DDD`，它只是让您更好的践行`DDD`。

`MiCake`的核心是提供领域驱动设计（DDD）的功能，但同时还提供了其它的扩展功能便于您更快速的构建出应用程序：比如依赖注入、自动审计、全局异常处理等等功能。

## 🍒 小试牛刀

通过下面的操作步骤，您将建立一个小小的`MiCake`演示程序。

### 环境条件

+ .NET Core SDK 3.0 +
+ 带有ASP.NET和Web开发的 `Visual Studio 2019` 或者`Visual Studio Code`
+ SqlServer [可选。该选项取决于您接下来EFCore使用何种数据库提供程序]

### 操作步骤

1.新建项目

  + 从 Visual Studio “文件” 菜单中选择“新建” >“项目” 。
  + 选择“ASP.NET Core Web 应用程序” 。
  + 将该项目命名为 MiCakeDemo 。

2.配置

+ 在`Visual Studio`中选择“工具”>“NuGet 包管理器”>“包管理器控制台”
+ 执行以下包安装指令：

```powershell
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package Microsoft.EntityFrameworkCore.Tools
Install-Package MiCake.AspNetCore
```

+ 在项目文件夹中，使用以下代码创建 MyDbContext.cs

```csharp
public class MyDbContext : MiCakeDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlServer("Server=localhost;Database=MiCakeDemo;User=sa;Password={your password};");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => base.OnModelCreating(modelBuilder);
}
```

到目前为止，您会发现这和您建立一个普通的`EFCore`应用没有一点区别。是的，以上操作对于经常使用`EFCore`的朋友将感到非常熟悉。

+ 在项目文件夹中，使用以下代码创建 Book.cs

```csharp
public class Book : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Author { get; private set; }

    public Book()
    {
        Id = Guid.NewGuid();
    }

    public Book(string name, string author) : this()
    {
        Name = name;
        Author = author;
    }

    public void ChangeName(string name)
    {
        CheckValue.NotNullOrEmpty(name, nameof(name));
        Name = name;
    }
}
```

+ 在MyDbContext.cs添加如下代码：

```csharp
public class MyDbContext : MiCakeDbContext
{
   //Add this line
   public DbSet<Book> Books { get; set; }

   …………
}
```

+ 在Startup.cs添加如下代码：

```csharp
 // This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();

   //添加该代码 用于配置EFCore
    services.AddDbContext<BaseAppDbContext>();
   //添加该代码 用于配置MiCake
    services.AddMiCakeWithDefault<BaseAppDbContext>();
}

// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthorization();
    //添加该代码 用于启动MiCake
    app.StartMiCake();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
}
```

## 🍉 文档

## 🍊 示例项目



## 🍍 当前版本

## 🍠 贡献与帮助

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/uoyoCsharp/MiCake/pulls)

如果您有什么好的想法和观点，欢迎通过 [Pull Request](https://github.com/uoyoCsharp/MiCake/pulls) 进行贡献，或通过 [提交 issues](https://github.com/uoyoCsharp/MiCake/issues/new)  来反馈您在使用过程中所发现的BUG。（ *期待得到您的反馈~* 🌻🌻）

## 🍑 联系

[![博客园](https://img.shields.io/badge/%E5%8D%9A%E5%AE%A2%E5%9B%AD-%E5%8F%A5%E5%B9%BD-blue)](https://www.cnblogs.com/uoyo/)

如果您喜欢关于 `.NET ` 方面的内容，或者对领域驱动很感兴趣，欢迎您关注我的博客：[句幽的博客](https://www.cnblogs.com/uoyo/)。您可以通过博客园内的站内**短消息**来与我沟通有关编程方面的问题。

[![QQ](https://img.shields.io/badge/QQ-Online-green)](tencent://AddContact/?fromId=45&fromSubId=1&subcmd=all&uin=344481481)

如果您愿意与我沟通一些其它方面（*非编程方向*）的事情，欢迎点击上面的 QQ 徽章添加好友。🌻🌻

## 🍄 最后

备战 `.NET 5` 。**冲鸭！！** 🐣