# MiCake.Util — Utilities index

This README documents the recommended organization and naming for utility helpers in the MiCake framework (code is under `src/framework/MiCake.Core/Util/`). The goal is to keep a single NuGet package but improve developer discoverability and follow .NET namespace naming best practices.

---

## Final recommended groups (folders & namespaces)
Follow noun-based naming for namespaces (prefer singular domain words) and organize by domain.

- Caching
  - Folder: `Util/Caching`
  - Suggested namespace: `MiCake.Util.Caching` (or `MiCake.Util.Cache` if preferred)
  - Example files: `BoundedLruCache.cs`

- Query
  - Folder: `Util/Query`
  - Suggested namespaces:
    - `MiCake.Util.Query` (top-level)
    - `MiCake.Util.Query.Filtering` (filter helpers)
    - `MiCake.Util.Query.Paging` (paging helpers)
    - `MiCake.Util.Query.Sorting`
  - Example files: `FilterExtensions`, `ExpressionHelpers`, `DynamicQueryGeneratorExtensions`, `PagingRequest`, `PagingResponse`

- Reflection
  - Folder: `Util/Reflection`
  - Namespace: `MiCake.Util.Reflection`
  - Example files: `CompiledActivator.cs`, `ReflectionHelper.cs`, `TypeHelper.cs` and subfolder `Emit/`

- Convert
  - Folder: `Util/Convert`
  - Namespace: `MiCake.Util.Convert`
  - Example files: `ConvertHelper.cs`, `BaseConvert.cs`, converters

- Resilience
  - Folder: `Util/Resilience` (or `Util/CircuitBreaker`)
  - Namespace: `MiCake.Util.Resilience` or `MiCake.Util.CircuitBreaker`
  - Example files: circuit breaker classes

- Collections (Extensions)
  - Folder: `Util/Collections` or `Util/Extensions`
  - Namespace: `MiCake.Util.Collections`
  - Example files: `ListExtensions`, `DictionaryExtensions`, `RangeExtensions`

- Diagnostics
  - Folder: `Util/Diagnostics`
  - Namespace: `MiCake.Util.Diagnostics`
  - Example: `DebugEnvironment.cs`

- Store / Storage
  - Folder: `Util/Store` (or `Util/Storage` depending on preference)
  - Namespace: `MiCake.Util.Store`
  - Example file: `DataDepositPool.cs`

- Expressions
  - Folder: `Util/Expressions`
  - Namespace: `MiCake.Util.Expressions`
  - Example: `ExpressionExtensions.cs`

- CommonType
  - Folder: `Util/CommonType` (or add to `Collections` if small)
  - Namespace: `MiCake.Util.Common` or keep `MiCake.Util`

---

## Why these names?
- Use single-word noun for top-level domain (e.g., `Query`) matching .NET style (see `Microsoft.EntityFrameworkCore.Query`).
- Avoid gerunds like `Querying` in namespaces; prefer nouns for clarity and consistency.
- Use plural nouns only when the folder is a collection of DTOs or you want to emphasize multiple items (e.g., `Queries`).

---

## Migration plan (safe & non-breaking)
1. Create the new folder(s) under `src/framework/MiCake.Core/Util/` and copy existing files.
2. Keep the public namespaces in the moved files unchanged to preserve API. This is the low-risk approach.
3. Update internal code references only if you change namespaces.
4. Run `dotnet build` and `dotnet test` after each chunk of moves.
5. Update docs, README, and add an entry in the repo’s top-level `CHANGELOG`.
6. Optionally, after a minor release, you may change namespaces and add `[Obsolete]` wrappers in old namespaces to gradually migrate.

Sample compatibility shim:
```csharp
namespace MiCake.Util.LinqFilter
{
    [Obsolete("Use MiCake.Util.Query.Filtering.FilterExtensions instead.")]
    public static class FilterExtensionsCompatibility
    {
        public static IQueryable<T> Filter<T>(this IQueryable<T> query, List<Filter> filters)
            => MiCake.Util.Query.Filtering.FilterExtensions.Filter(query, filters);
    }
}
```

---

## Documentation / README enhancements to add
- `src/framework/MiCake.Core/Util/README.md` (this file)
- `src/framework/MiCake.Core/Util/Query/README.md` — examples for filtering/paging
- Quick-start: short code snippets for `Filter` and `Paging` usage
- Compatibility notice: where names changed or were merged from legacy `MiCake.Util` package

---

## Tests & CI checks
- Unit tests live under `src/tests/MiCake.Core.Tests/Util/`.
- After moving files, ensure tests reference the correct namespaces or keep namespaces unchanged to avoid fallout.
- After changing namespaces, add API verification (e.g., dotnet apicompat) if you want to prevent accidental breaking changes.

---

## Naming checklist for contributors
- Add utilities to the appropriate domain folder — prefer `Query` for query features.
- Keep public API namespaces stable when possible.
- Use `Extensions` only when exposing a set of extension methods.
- Keep README updated with mapping notes.

---

## Next steps (optional)
- (A) If you want, I can move `LinqFilter` to `Util/Query/Filtering` as a POC while keeping the public namespace unchanged.
- (B) Create `src/framework/MiCake.Core/Util/Query/README.md` with usage examples for filters and paging.

---

If you prefer a different naming scheme, or would like me to move files as a proof-of-concept (keeping namespaces for compatibility), tell me which option you'd like to implement and I’ll proceed with a safe migration and tests. 
