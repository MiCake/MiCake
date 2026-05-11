<p align="center">
  <a href="https://micake.github.io">
    <img width="180px" src="assets/readme_logo.png">
  </a>
</p>

<h1 align="center">MiCake</h1>

<div align="center">

A lightweight Domain-Driven Design (DDD) toolkit for .NET

[![Nuget Version](https://img.shields.io/nuget/v/MiCake?label=nuget%20version&logo=nuget)](https://www.nuget.org/packages/MiCake/) [![Nuget Downloads](https://img.shields.io/nuget/dt/MiCake?color=green&label=nuget%20downloads&logo=nuget)](https://www.nuget.org/packages/MiCake/) [![Build Status](https://github.com/MiCake/MiCake/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/MiCake/MiCake/actions/workflows/build-and-release.yml) [![codecov](https://codecov.io/gh/MiCake/MiCake/branch/master/graph/badge.svg)](https://codecov.io/gh/MiCake/MiCake)

[![License](https://img.shields.io/github/license/MiCake/MiCake)](https://github.com/MiCake/MiCake/blob/master/LICENSE) [![.NET Version](https://img.shields.io/badge/.NET-10.0+-512BD4?logo=dotnet)](https://dotnet.microsoft.com/) [![Last Commit](https://img.shields.io/github/last-commit/MiCake/MiCake)](https://github.com/MiCake/MiCake/commits/master) [![GitHub Issues](https://img.shields.io/github/issues/MiCake/MiCake)](https://github.com/MiCake/MiCake/issues) [![GitHub Pull Requests](https://img.shields.io/github/issues-pr/MiCake/MiCake)](https://github.com/MiCake/MiCake/pulls)

**English** | [简体中文](README.md)

</div>

---

## What is MiCake?

MiCake is a lightweight Domain-Driven Design (DDD) toolkit built on .NET. It is designed to help developers quickly transform existing projects into a DDD style while keeping the code clean and flexible.

MiCake is not a traditional "framework", but rather a convenient "toolset". It provides the various components and tools needed to practice DDD, without forcing you to change your development habits.

## Getting Started

Eager to try MiCake? We provide mature templates and sample projects. With a single `dotnet new` command, you can scaffold an out-of-the-box `ASP.NET Core` project powered by MiCake.

For the full getting-started guide and documentation, please visit: 📚 **[Full Online Documentation - Getting Started](https://micake.github.io)**

## Core Features

### 🚀 Fast
Quickly transform your project into a DDD-style architecture, focusing on writing domain code rather than framework configuration. With simple configuration and minimal code, you can upgrade an existing project to a DDD architecture.

### 📐 Standard
Implements the core building blocks of DDD tactical patterns:

- **Entity** - Domain objects with a unique identity
- **Value Object** - Immutable objects compared by their property values
- **Aggregate Root** - The root entity of an aggregate, serving as the entry point for external access
- **Repository** - Provides persistence operations for aggregate roots
- **Domain Event** - Captures important events that occur within the domain
- **Domain Service** - Encapsulates domain logic that does not naturally belong to entities or value objects
- **Unit of Work** - Manages business transactions and data consistency

### 🎯 Convenient
Provides commonly used infrastructure features for everyday project development:

- Global exception handling
- Unified response format
- Automatic auditing
- Soft delete support
- Dependency injection enhancements
- API request logging
- A rich toolset (caching, converters, queries, circuit breakers, etc.)

### 🪶 Gentle
"Gentle" is the core design philosophy of MiCake:

- **Non-intrusive** - Integrates seamlessly into existing projects without changing your coding style
- **Loosely coupled** - Framework code is cleanly separated from business code
- **Optional** - DDD is not mandatory; it can be adopted gradually
- **Almost invisible** - When DDD features are not used, you barely notice it is there

## Use Cases

- ✅ Building new projects with a clean, DDD-based architecture
- ✅ Refactoring existing projects by gradually introducing DDD to improve maintainability
- ✅ Small-to-medium enterprise applications that need a solid architecture without a heavyweight framework
- ✅ Learning and practicing DDD with a lightweight reference implementation

## Requirements

- .NET 10.0 or later
- Visual Studio 2022+ / Visual Studio Code / Rider

## NuGet Packages

| Package                    | Version                                                                        | Description                    |
| -------------------------- | ------------------------------------------------------------------------------ | ------------------------------ |
| MiCake                     | ![Nuget](https://img.shields.io/nuget/v/MiCake?logo=nuget)                     | Core DDD components            |
| MiCake.EntityFrameworkCore | ![Nuget](https://img.shields.io/nuget/v/MiCake.EntityFrameworkCore?logo=nuget) | EF Core integration            |
| MiCake.AspNetCore          | ![Nuget](https://img.shields.io/nuget/v/MiCake.AspNetCore?logo=nuget)          | ASP.NET Core integration       |

For more packages, visit [NuGet.org](https://www.nuget.org/packages?q=micake).

## Contributing

[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/MiCake/MiCake/pulls)

We welcome community contributions!

- 💡 [Open an issue](https://github.com/MiCake/MiCake/issues/new) - Report a bug or suggest a feature
- 🔧 [Submit a PR](https://github.com/MiCake/MiCake/pulls) - Improve the code or documentation
- 💬 [Join the discussion](https://github.com/MiCake/MiCake/discussions) - Share experiences and best practices

## Community & Support

- 📖 [Official Documentation](https://micake.github.io)
- 💻 [GitHub Repository](https://github.com/MiCake/MiCake)
- 📝 [Author's Blog](https://www.cnblogs.com/uoyo/)
- 📦 [NuGet Packages](https://www.nuget.org/packages?q=micake)

---

<div align="center">

**MiCake** - Making DDD simpler

</div>
