# MiCake Development Principles

Version: 1.0
Created By: MiCake Team

## 1. Architecture Principles

- **Keep the four-layer architecture**: Maintain the distinct layers (Core, DDD, AspNetCore, EntityFrameworkCore). Each layer has a focused responsibility.
  - Rationale: Clear separation of concerns improves testability and maintenance.
  - Example: `MiCake.Core` contains only DI and utility helpers; `MiCake.EntityFrameworkCore` contains EF-specific repositories.

- **Dependency direction must be inward**:
  - Rule: Outer layers can depend on inner layers, but inner layers must not depend on outer ones.
  - Example: `MiCake.AspNetCore` may use `MiCake` (DDD), but `MiCake.Core` must not reference AspNetCore.

- **Minimize top-level namespaces**: Avoid creating new root namespaces; follow `MiCake.{Layer}.{Feature}`.
  - Example: `MiCake.EntityFrameworkCore.Repository`.

---

## 2. Module System Principles

- **Modules must be explicit**: Modules should inherit from `MiCakeModule` and implement lifecycle hooks.
  - Rationale: Consistent lifecycle handling (pre-configure, configure, initialize, shutdown).
  - Example: Override `ConfigureServices(ModuleConfigServiceContext)` to auto-register repositories.

- **Declare dependencies via attributes**: Use `RelyOn` (or similar) rather than runtime checks.
  - Example: `[RelyOn(typeof(MiCakeDomainModule), typeof(MiCakeEFCoreModule))]`.

- **Prefer default behavior in base modules**: Provide sensible defaults in `MiCakeModule` to reduce boilerplate.

---

## 3. Dependency Injection (DI) Principles

- **Explicit dependencies**: Avoid `IServiceProvider` in constructors; prefer constructor injection.
  - Rationale: Easier to reason about dependencies and better for testing.
  - Example (bad): `public MyService(IServiceProvider sp) { ... }` (avoid)
  - Example (good): `public MyService(IRepository repo) { ... }`

- **Use marker interfaces or attributes for auto-registration**:
  - Marker interfaces: `ITransientService`, `IScopedService`, `ISingletonService`.
  - Attribute: `InjectService` to register multiple interfaces.

- **Use a dependency wrapper for many parameters**: When a public class has ≥2 injected dependencies, wrap them into a single dependencies object.
  - Example: `RepositoryDependencies<TDbContext>` containing `DbContextAccessor`, `UnitOfWorkManager`, `Logger`.

---

## 4. Resource Management & Dispose Pattern

- **Implement the standard Dispose pattern** when a class holds resources that must be deterministically released.
  - Pattern: `Dispose()`, `Dispose(bool disposing)`, `GC.SuppressFinalize(this)`, finalizer only when unmanaged resources exist.
  - Example: `EFCoreDbContextWrapper` should implement `Dispose(bool)` and use `try/catch` around resource disposal.

- **Make Dispose idempotent**: `Dispose()` may be called multiple times safely.
  - Rationale: .NET consumers may call `Dispose()` repeatedly; throwing on second call is anti-pattern.

- **Check disposed state in public methods**: Throw `ObjectDisposedException` for operations after disposal.
  - Example: `ObjectDisposedException.ThrowIf(_disposed, this);`

---

## 5. Naming & Style

- **Use PascalCase for public types and members; camelCase for locals/params; _camelCase for private fields**.
  - Example: `public class OrderService`, `private readonly ILogger _logger`.

- **Async suffix for async methods**: `SaveChangesAsync`, `GetAsync`.

- **Use XML comments for public APIs**: Provide `<summary>`, `<param>`, and `<returns>`.

---

## 6. Asynchronous Programming

- **Library code uses `ConfigureAwait(false)`** for all awaits.
  - Rationale: Avoid capturing synchronization context in library code.
  - Example: `await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);`

- **Avoid blocking async**: Do not use `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.

---

## 7. Error Handling

- **Validate public inputs**: Use `ArgumentNullException.ThrowIfNull()` and `ArgumentException.ThrowIfNullOrWhiteSpace()`.
  - Example: `ArgumentNullException.ThrowIfNull(dbContext);`

- **Log errors with context**: Include IDs and types in logs to help troubleshooting.

---

## 8. Performance & Reflection

- **Prefer compiled activators over `Activator.CreateInstance`** for performance-sensitive reflection use.
  - Example: implement a `CompiledActivator` cache using expression trees.

- **Cache expensive computed data** with `ConcurrentDictionary` Or `BoundedLruCache` to improve performance.

- **Avoid O(n) scans on high-frequency paths**. Use indexing or pre-computed maps instead.

---

## 9. Testing Principles
- **Unit tests use AAA pattern** (Arrange/Act/Assert).

- **Test naming**: `{Method}_{Scenario}_{ExpectedResult}`.

- **Prefer focused tests**: Test single responsibility per test.

- **Use integration tests for infrastructure**: EF Core, UoW behavior, and repository integration tests should live in integration tests.

---

## 10. Documentation & Code Review

- **Public APIs must have XML docs**.

- **Code review checklist**: Ensure architecture compliance, logging, naming, performance, and dispose patterns before merging.

---

## 11. Checklist for PRs (Quick)

- [ ] Follows dependency rules (no inward dependency violations)
- [ ] Public APIs documented with XML comments
- [ ] No blocking async usage
- [ ] Dispose pattern implemented where needed
- [ ] Unit tests added/updated
- [ ] Performance-sensitive paths reviewed for caching and indexing
