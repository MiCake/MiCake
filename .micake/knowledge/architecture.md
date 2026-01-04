# MiCake Architecture

## Layer Structure

MiCake uses a four-layer architecture with strict dependency rules.

| Layer | Level | Primary Role | Dependencies |
|-------|-------|--------------|--------------|
| MiCake.EntityFrameworkCore | 4 (Top) | Data access implementation | MiCake.Core, MiCake, MiCake.AspNetCore |
| MiCake.AspNetCore | 3 | Web integration | MiCake.Core, MiCake |
| MiCake (DDD) | 2 | Domain patterns | MiCake.Core |
| MiCake.Core | 1 (Bottom) | Foundation | None |

**Dependency Rule**: Higher layers depend on lower layers. Never the reverse.

## Layer Responsibilities

### MiCake.Core

Foundation utilities and DI abstractions.

Contents:
- Dependency injection helpers
- Module system (MiCakeModule, lifecycle)
- Utility classes
- Marker interfaces (ITransientService, IScopedService, ISingletonService)

Dependencies: None (innermost layer)

### MiCake (DDD)

Domain-Driven Design patterns and abstractions.

Contents:
- Entity, AggregateRoot, ValueObject base classes
- IRepository, IReadOnlyRepository interfaces
- Domain event infrastructure
- Unit of Work pattern
- Audit support

Dependencies: MiCake.Core

### MiCake.AspNetCore

ASP.NET Core integration.

Contents:
- API response wrapping
- Exception handling middleware
- Data wrapper filters
- Web-specific extensions

Dependencies: MiCake.Core, MiCake (DDD)

### MiCake.EntityFrameworkCore

EF Core data access implementation.

Contents:
- MiCakeDbContext base class
- Repository implementations
- Store conventions
- EF-specific Unit of Work

Dependencies: MiCake.Core, MiCake (DDD)

## Dependency Rules

1. **Inward only**: Outer layers depend on inner, never reverse
2. **No cross-dependency**: AspNetCore and EntityFrameworkCore don't depend on each other
3. **Core is isolated**: Core has zero external dependencies
4. **DDD is domain-pure**: No infrastructure concerns

## Namespace Conventions

Pattern: `MiCake.{Layer}.{Feature}`

Examples:
- `MiCake.Core.Modularity`
- `MiCake.DDD.Domain`
- `MiCake.AspNetCore.DataWrapper`
- `MiCake.EntityFrameworkCore.Repository`

Rules:
- Minimize root namespaces
- Group by feature within layer
- Keep namespace depth reasonable (max 4 levels)

## Extension Points

### User Extension

- Inherit from `MiCakeModule` for custom modules
- Inherit from `AggregateRoot<TKey>` for domain objects
- Implement `IRepository<TAggregateRoot, TKey>` for custom repositories
- Inherit from `MiCakeDbContext` for EF Core

### Framework Extension

- Add new lifecycle hooks to module system
- Create new convention engines
- Add new metadata providers
- Extend store conventions

## Design Principles

1. **Lightweight**: Minimal overhead, non-intrusive
2. **Explicit**: No magic, clear contracts
3. **Modular**: Pick what you need
4. **Testable**: Easy to mock and test
5. **Extensible**: Open for extension, closed for modification
