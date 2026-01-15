# MiCake.Core

Core framework components for the MiCake DDD toolkit.

## Overview

`MiCake.Core` provides the foundational building blocks for the MiCake framework:

- **Modular Architecture** - Plugin-based module system with lifecycle hooks
- **Dependency Injection** - Enhanced DI with auto-registration support
- **Utility Classes** - Common helpers and extensions

## Installation

```bash
dotnet add package MiCake.Core
```

## Key Features

| Feature | Description |
|---------|-------------|
| `MiCakeModule` | Base class for creating modular applications |
| `ITransientService` | Marker interface for transient DI registration |
| `IScopedService` | Marker interface for scoped DI registration |
| `ISingletonService` | Marker interface for singleton DI registration |

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
