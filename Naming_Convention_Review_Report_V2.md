# MiCake Framework Naming Convention Review Report V2

**Version**: 2.0  
**Date**: December 2024  
**Branch Reviewed**: `refactor`  
**Reviewer**: Automated Code Review Agent  
**Status**: Follow-up Review (Post-V1 Analysis)

---

## Executive Summary

This report is a follow-up review of the MiCake framework's public API naming conventions following the initial review (V1). This review verifies whether previously identified issues have been addressed and identifies any remaining or new naming concerns.

**Review Result**: The issues identified in V1 have **not been fixed yet**. All previously reported issues remain in the current codebase.

---

## Review Scope

Reviewed all public APIs across:
- `MiCake.Core` - Core framework and DI abstractions
- `MiCake` (DDD) - Domain-Driven Design patterns
- `MiCake.AspNetCore` - ASP.NET Core integration
- `MiCake.EntityFrameworkCore` - Entity Framework Core integration

---

## Issues Status Summary

| Category | V1 Count | Fixed | Remaining | New |
|----------|----------|-------|-----------|-----|
| 🔴 Critical | 1 | 0 | 1 | 0 |
| 🟡 Important | 8 | 0 | 8 | 0 |
| 🟢 Minor | 8 | 0 | 8 | 0 |
| **Total** | **17** | **0** | **17** | **0** |

---

## Remaining Issues (From V1)

### 🔴 Critical Issues (Must Fix)

#### 1. [STILL OPEN] Typo in Method Name: `HandleAysnc`

**Location**: `src/framework/MiCake/DDD/Domain/IDomainEventHandler.cs:13`

**Current Code**:
```csharp
Task HandleAysnc(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
```

**Issue**: The method name contains a typo - "Aysnc" should be "Async".

**Impact**: 
- Violates .NET async method naming convention (Async suffix)
- Creates confusion for developers
- Will cause issues with tooling that expects the correct suffix
- Breaking change if renamed later after widespread adoption

**Required Fix**:
```csharp
Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
```

**Priority**: **CRITICAL** - This must be fixed before any public release.

---

### 🟡 Important Issues (Should Fix)

#### 2. [STILL OPEN] Parameter Typo: `optionsBulder`

**Location**: `src/framework/MiCake.AspNetCore/Modules/MiCakeBuilderAspNetCoreExtension.cs`
- Line 24 (XML comment)
- Line 28 (parameter declaration)
- Line 33 (usage)

**Current Code**:
```csharp
/// <param name="optionsBulder">The config for MiCake AspNetCore extension</param>
public static IMiCakeBuilder UseAspNetCore(
    this IMiCakeBuilder builder,
    Action<MiCakeAspNetOptions>? optionsBulder)
{
    optionsBulder?.Invoke(options);
}
```

**Required Fix**:
```csharp
/// <param name="optionsBuilder">The config for MiCake AspNetCore extension</param>
public static IMiCakeBuilder UseAspNetCore(
    this IMiCakeBuilder builder,
    Action<MiCakeAspNetOptions>? optionsBuilder)
{
    optionsBuilder?.Invoke(options);
}
```

---

#### 3. [STILL OPEN] Inconsistent Options Class Naming

**Locations**:
- `MiCakeAspNetOptions` - Uses "Options" (correct)
- `MiCakeAspNetUowOption` - Uses "Option" (singular) ❌
- `MiCakeEFCoreOptions` - Uses "Options" (correct)
- `MiCakeAuditOptions` - Uses "Options" (correct)

**Issue**: `MiCakeAspNetUowOption` uses singular "Option" instead of plural "Options".

**Required Fix**: Rename `MiCakeAspNetUowOption` → `MiCakeAspNetUowOptions`

---

#### 4. [STILL OPEN] Ambiguous Property Name: `EntryType`

**Location**: `src/framework/MiCake.Core/Abstractions/IMiCakeEnvironment.cs:13`

**Current Code**:
```csharp
public Type EntryType { get; set; }
```

**Recommendation**: Rename to `EntryModuleType` for clarity.

---

#### 5. [STILL OPEN] Unclear Property Name: `BuildTimeData`

**Location**: `src/framework/MiCake.Core/Abstractions/MiCakeApplicationOptions.cs:38`

**Current Code**:
```csharp
public DataDepositPool BuildTimeData { get; set; } = new DataDepositPool();
```

**Recommendation**: Consider `BuildPhaseData` or `BuildPhaseDataStore` for improved clarity.

---

#### 6. [STILL OPEN] Ambiguous Context Class Name

**Location**: `src/framework/MiCake.Core/Abstractions/Modularity/ModuleConfigServiceContext.cs`

**Issue**: "ConfigService" could be misread as "Configuration Service" rather than "Configure Services".

**Recommendation**: Consider `ModuleServiceConfigurationContext` or `ModuleConfigureServicesContext`.

---

#### 7. [STILL OPEN] Vague Method Prefix: `CommonFilterQueryAsync`

**Location**: `src/framework/MiCake/DDD/Infrastructure/Paging/IRepositoryHasPagingQuery.cs`

**Current Methods**:
```csharp
Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(FilterGroup filterGroup, ...);
Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest pagingRequest, ...);
```

**Issue**: The prefix "Common" is vague and doesn't clearly describe what makes these queries "common".

**Recommendation**:
```csharp
Task<IEnumerable<TAggregateRoot>> DynamicFilterQueryAsync(FilterGroup filterGroup, ...);
Task<PagingResponse<TAggregateRoot>> DynamicFilterPagingQueryAsync(PagingRequest pagingRequest, ...);
```

---

#### 8. [STILL OPEN] "RelyOn" vs Industry Standard "DependsOn"

**Location**: `src/framework/MiCake.Core/Abstractions/Modularity/RelyOnAttribute.cs`

**Issue**: The framework uses "RelyOn" terminology while the .NET ecosystem typically uses "DependsOn".

**Recommendation**: Consider adding `[DependsOn]` as an alias attribute, or document the "RelyOn" convention clearly.

---

#### 9. [STILL OPEN] Abbreviated EF Core Class Names

**Locations**:
- `EFRepository`
- `EFReadOnlyRepository`  
- `EFRepositoryBase`
- `EFRepositoryDependencies`

**Recommendation**: Consider using full names (`EntityFrameworkRepository`, etc.) for better discoverability.

---

### 🟢 Minor Issues (Consider Fixing)

#### 10. [STILL OPEN] Documentation Typo: "entites"

**Location**: `src/framework/MiCake.EntityFrameworkCore/IEFSaveChangesLifetime.cs`
- Line 16: `<param name="entityEntries">current change tracking entites</param>`
- Line 24: `<param name="entityEntries">current change tracking entites</param>`

**Required Fix**: Change "entites" → "entities"

---

#### 11. [STILL OPEN] Documentation Typo: "Defalut"

**Location**: `src/framework/MiCake/Audit/MiCakeAuditOptions.cs`
- Line 11: `Defalut value is false.`
- Line 18: `Defalut value is true.`

**Required Fix**: Change "Defalut" → "Default"

---

#### 12. [STILL OPEN] Inconsistent Lifetime/Lifecycle Terminology

**Locations**:
- `IRepositoryLifetime` - Uses "Lifetime"
- `IEFSaveChangesLifetime` - Uses "Lifetime"
- `IUnitOfWorkLifecycleHook` - Uses "LifecycleHook"

**Recommendation**: Standardize on one term:
- "Lifetime" for service lifetime patterns
- "Lifecycle" for component lifecycle events

---

#### 13. [STILL OPEN] Extension Class Naming Inconsistency

**Pattern Inconsistency**:
| Singular "Extension" | Plural "Extensions" |
|---------------------|---------------------|
| `MiCakeServicesExtension` | `CollectionExtensions` |
| `MiCakeBuilderAspNetCoreExtension` | `ListExtensions` |
| `AutoRegisterRepositoriesExtension` | `StringExtensions` |

**Recommendation**: Standardize on plural "Extensions" suffix (matching .NET BCL convention).

---

#### 14. [STILL OPEN] File Name vs Enum Name Mismatch

**Location**: `src/framework/MiCake.Core/Abstractions/DependencyInjection/MiCakeServiceLifeTime.cs`

**Issue**: 
- File name: `MiCakeServiceLifeTime.cs` (with space)
- Enum name: `MiCakeServiceLifetime` (no space)

**Required Fix**: Rename file to `MiCakeServiceLifetime.cs`

---

#### 15. [STILL OPEN] Class Name: `DataWrapperDefaultCode`

**Location**: `src/framework/MiCake.AspNetCore/Responses/ResponseWrapperOptions.cs:80`

**Issue**: The class name uses singular "Code" but contains multiple codes (Success, ProblemDetails, Error).

**Recommendation**: Rename to `DataWrapperDefaultCodes` or `ResponseStatusCodes`

---

#### 16. [STILL OPEN] Delegate Naming Pattern

**Location**: `src/framework/MiCake.Core/Abstractions/DependencyInjection/FindAutoServiceTypesDelegate.cs`

**Current**: `FindAutoServiceTypesDelegate`

**Recommendation**: Consider `AutoServiceTypeFinder` or `ServiceTypeDiscoveryHandler`

---

#### 17. [STILL OPEN] Delegate Naming: CustomerRepositorySelector

**Location**: `src/framework/MiCake/DDD/Infrastructure/Register/AutoRegisterRepositoriesExtension.cs:17`

**Current**: `CustomerRepositorySelector`

**Issue**: "Customer" appears to be a typo for "Custom"

**Required Fix**: Rename to `CustomRepositorySelector`

---

## New Observations (V2)

### Documentation Quality Notes

#### 1. Comment Grammar in IRepository.cs

**Location**: `src/framework/MiCake/DDD/Domain/IRepository.cs:7`

**Current**:
```csharp
/// <summary>
/// Defined a DDD repository interface.Please use <see cref="IRepository{TAggregateRoot, TKey}"/>.
/// </summary>
```

**Recommendation**: Add space after period:
```csharp
/// <summary>
/// Defines a DDD repository interface. Please use <see cref="IRepository{TAggregateRoot, TKey}"/>.
/// </summary>
```

---

#### 2. Comment Clarity in IReadOnlyRepository.cs

**Location**: `src/framework/MiCake/DDD/Domain/IReadOnlyRepository.cs:9`

**Current**:
```csharp
/// <summary>
/// A Repository only has get method
/// </summary>
```

**Recommendation**:
```csharp
/// <summary>
/// A read-only repository that provides query operations without modification capabilities.
/// </summary>
```

---

#### 3. Typo in IRepositoryHasPagingQuery.cs Comments

**Location**: `src/framework/MiCake/DDD/Infrastructure/Paging/IRepositoryHasPagingQuery.cs`

**Lines 19 and 27**: "Paing" should be "Paging"

**Current**:
```csharp
/// <summary>
/// Paing query data from repository by <see cref="PagingRequest"/>
/// </summary>
```

**Required Fix**:
```csharp
/// <summary>
/// Paging query data from repository by <see cref="PagingRequest"/>
/// </summary>
```

---

## Priority Action Items

### Immediate Actions (Before Release)

| Priority | Issue | Location | Fix |
|----------|-------|----------|-----|
| 🔴 P0 | `HandleAysnc` typo | IDomainEventHandler.cs:13 | → `HandleAsync` |
| 🟡 P1 | `optionsBulder` typo | MiCakeBuilderAspNetCoreExtension.cs | → `optionsBuilder` |
| 🟡 P1 | `CustomerRepositorySelector` typo | AutoRegisterRepositoriesExtension.cs:17 | → `CustomRepositorySelector` |
| 🟢 P2 | `entites` typo | IEFSaveChangesLifetime.cs | → `entities` |
| 🟢 P2 | `Defalut` typo | MiCakeAuditOptions.cs | → `Default` |
| 🟢 P2 | `Paing` typo | IRepositoryHasPagingQuery.cs | → `Paging` |

### Short-term Actions

| Priority | Issue | Current | Recommended |
|----------|-------|---------|-------------|
| 🟡 P1 | Options naming | `MiCakeAspNetUowOption` | `MiCakeAspNetUowOptions` |
| 🟡 P1 | Property clarity | `EntryType` | `EntryModuleType` |
| 🟡 P1 | File name mismatch | `MiCakeServiceLifeTime.cs` | `MiCakeServiceLifetime.cs` |

### Long-term Considerations

| Issue | Recommendation |
|-------|----------------|
| Extension class suffix | Standardize on plural "Extensions" |
| Lifecycle terminology | Choose "Lifetime" or "Lifecycle" consistently |
| EF Core class naming | Consider full names for discoverability |
| Method naming | Replace "Common" prefix with more descriptive names |

---

## Conclusion

**Overall Assessment**: The MiCake framework has **not yet addressed** the naming issues identified in the V1 review. All 17 previously identified issues remain in the current codebase.

**Critical Finding**: The `HandleAysnc` typo in `IDomainEventHandler.cs` is a critical issue that must be fixed before any public release, as it will become a breaking change once the API is in use.

**Recommendation**: Address the typos and spelling errors as the highest priority since they:
1. Are quick to fix
2. Have no functional impact
3. Prevent potential breaking changes later
4. Improve code professionalism and developer experience

---

## References

- [Microsoft .NET Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [MiCake Development Principles](principles/development_principle.md)
- [Previous Report: Naming_Convention_Review_Report.md](Naming_Convention_Review_Report.md)
