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

## Unit of Work per Request

A `UnitOfWorkFilter` runs last on every controller action. It begins a unit of work before the action, commits it after a successful action, and rolls it back on action failure or request cancellation. The unit of work is always disposed asynchronously, including on commit or rollback failure.

### Operation Modes

| Mode | How to enable | Behavior |
|------|---------------|----------|
| Automatic (default) | `EnableAutoUnitOfWork = true` | Every action gets a writable unit of work that commits on success and rolls back on failure |
| Explicit read-only | `[UnitOfWork(IsReadOnly = true)]` on action or controller | Read-only unit of work; every attempted write fails before a command executes; no commit is performed |
| Explicit writable | `[UnitOfWork(IsReadOnly = false)]` | Writable unit of work regardless of action-name inference |
| Opt-out | `[DisableUnitOfWork]` on action or controller | No unit of work is created for the decorated action(s) |
| Global off | `EnableAutoUnitOfWork = false` | No automatic unit of work unless `[UnitOfWork]` is applied |

```csharp
[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    // Explicit read-only intent: writes fail before SQL execution
    [HttpGet("{id}")]
    [UnitOfWork(IsReadOnly = true)]
    public async Task<ActionResult<Book>> GetBook(Guid id) { ... }

    // Writable: the request unit of work commits tracked changes on success
    [HttpPost]
    public async Task<ActionResult<Guid>> AddBook(AddBookDto dto) { ... }
}
```

### Attribute and Configuration Priority

Per request, the filter resolves the effective policy in this order:

1. `[DisableUnitOfWork]` on the action, controller, or endpoint metadata — skips UoW management entirely. This wins over every other configuration, so a controller-level `[DisableUnitOfWork]` also overrides an action-level `[UnitOfWork]`.
2. `[UnitOfWork]` on the action (highest), controller, or endpoint metadata — enables UoW and applies its `IsReadOnly`/`IsolationLevel`, even when `EnableAutoUnitOfWork` is `false`.
3. Global configuration: `EnableAutoUnitOfWork` (default `true`), then `EnableReadOnlyActionNameInference` when the action name matches a keyword.

### Read-Only Action-Name Inference (Opt-In)

For compatibility with the previous naming convention, set `EnableReadOnlyActionNameInference = true`:

```csharp
options.AspNetConfig = asp =>
{
    asp.UnitOfWork.EnableReadOnlyActionNameInference = true;
    asp.UnitOfWork.ReadOnlyActionKeywords = ["Find", "Get", "Query", "Search"];
};
```

When enabled, actions whose names start with a configured keyword are treated as read-only. Explicit `[UnitOfWork(IsReadOnly = ...)]` metadata always overrides the inference. The inference is **disabled by default**: declare read-only intent explicitly.

### Request Failure Semantics

- Action failure or request cancellation → the unit of work rolls back.
- Commit failure → the unit of work rolls back and the original exception propagates; a rollback failure surfaces as an `AggregateException` carrying both causes.
- The unit of work is disposed asynchronously in all paths, so pooled DbContexts are returned promptly.

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
