# MiCake.AspNetCore

ASP.NET Core integration for the MiCake DDD toolkit.

## Overview

`MiCake.AspNetCore` provides seamless integration with ASP.NET Core:

- **Unit of Work Filter** - Automatic transaction management per request
- **Exception Handling** - Global exception handling with custom responses
- **Response Wrapper** - Unified API response format
- **API Logging** - Request/response logging with sensitive data masking
- **Data Wrapper** - Automatic response wrapping

## Installation

```bash
dotnet add package MiCake.AspNetCore
```

## Quick Start

```csharp
// In Program.cs
builder.Services
    .AddMiCake<MyDbContext, AppModule>()
    .Build();

var app = builder.Build();
app.StartMiCake();
```

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
