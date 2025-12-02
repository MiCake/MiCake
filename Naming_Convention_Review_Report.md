# MiCake Framework Naming Convention Review Report

**Version**: 1.0  
**Date**: December 2024  
**Branch Reviewed**: `refactor`  
**Reviewer**: Automated Code Review Agent

---

## Executive Summary

This report presents a comprehensive review of the MiCake framework's public API naming conventions, focusing on classes, properties, and method signatures across all framework layers (`MiCake.Core`, `MiCake` (DDD), `MiCake.AspNetCore`, and `MiCake.EntityFrameworkCore`).

The review identifies naming issues that may impact code clarity, developer experience, and maintainability. Each issue is categorized by severity and includes specific recommendations for improvement.

---

## Review Methodology

The review evaluated naming conventions based on:

1. **.NET Naming Guidelines**: Microsoft's official naming conventions
2. **MiCake Development Principles**: As documented in `principles/development_principle.md`
3. **Domain-Driven Design (DDD) Terminology**: Standard DDD naming patterns
4. **Industry Best Practices**: Common patterns in popular .NET frameworks

---

## Issues Found

### 🔴 Critical Issues (Must Fix)

#### 1. Typo in Method Name: `HandleAysnc`

**Location**: `src/framework/MiCake/DDD/Domain/IDomainEventHandler.cs:13`

**Current Name**:
```csharp
Task HandleAysnc(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
```

**Issue**: The method name contains a typo - "Aysnc" should be "Async".

**Impact**: 
- Violates .NET async method naming convention (Async suffix)
- Creates confusion for developers
- Will cause issues with tooling that expects the correct suffix
- Breaking change if renamed later after widespread adoption

**Recommended Fix**:
```csharp
Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
```

---

### 🟡 Important Issues (Should Fix)

#### 2. Inconsistent Options Class Naming

**Locations**:
- `MiCakeAspNetOptions` - Uses "AspNet" (abbreviated)
- `MiCakeAspNetUowOption` - Uses "Option" (singular) instead of "Options" (plural)
- `MiCakeEFCoreOptions` - Uses "EFCore" (abbreviated)
- `MiCakeAuditOptions` - Follows standard pattern

**Issue**: Inconsistent naming patterns for options classes:
- Mix of singular (`Option`) and plural (`Options`) suffixes
- Inconsistent abbreviation styles

**Recommended Fixes**:
- Rename `MiCakeAspNetUowOption` → `MiCakeAspNetUowOptions` (plural for consistency)
- Standardize abbreviations across the framework

---

#### 3. Ambiguous Context Class Names

**Locations**:
- `ModuleConfigServiceContext` in `MiCake.Core/Abstractions/Modularity/`
- `ModuleInitializationContext` in `MiCake.Core/Abstractions/Modularity/`
- `ModuleShutdownContext` in `MiCake.Core/Abstractions/Modularity/`

**Issue**: The name `ModuleConfigServiceContext` is less clear than it could be. "ConfigService" could be misread as "Configuration Service" rather than "Configure Services".

**Recommended Fix**:
- Consider `ModuleServiceConfigurationContext` for improved clarity
- Alternative: `ModuleConfigureServicesContext` to match the method name `ConfigureServices`

---

#### 4. Property Naming Inconsistency: `EntryType`

**Location**: `src/framework/MiCake.Core/Abstractions/IMiCakeEnvironment.cs:12`

**Current**:
```csharp
public Type EntryType { get; set; }
```

**Issue**: The property name `EntryType` is ambiguous. It could refer to:
- Entry point module type
- Entry assembly type
- Entry class type

**Recommended Fix**:
```csharp
public Type EntryModuleType { get; set; }
```

---

#### 5. Inconsistent "Rely On" vs "Depends On" Terminology

**Locations**:
- `RelyOnAttribute` class
- `RelyOnTypes` property
- `GetRelyOnTypes()` method
- `RelyOnModules` property in `MiCakeModuleDescriptor`

**Issue**: The framework uses "RelyOn" terminology while the .NET ecosystem typically uses "DependsOn" (e.g., ASP.NET Core's `DependsOn` in ABP framework, NuGet dependencies). 

**Industry Standard**: "Dependency" and "DependsOn" are more widely recognized terms.

**Recommendation**: 
While this is a stylistic choice, consider adding `[DependsOn]` as an alias attribute for developers familiar with other frameworks, or document the "RelyOn" convention clearly.

---

#### 6. Abbreviated Class Names in EF Core Layer

**Locations**:
- `EFRepository` - Could be `EntityFrameworkRepository` for clarity
- `EFReadOnlyRepository` - Could be `EntityFrameworkReadOnlyRepository`
- `EFRepositoryBase` - Could be `EntityFrameworkRepositoryBase`
- `EFRepositoryDependencies` - Could be `EntityFrameworkRepositoryDependencies`

**Issue**: While "EF" is a common abbreviation, full names improve discoverability and IDE autocomplete experience.

**Recommendation**: 
Consider using full names for public-facing classes while keeping abbreviations for internal implementation. At minimum, ensure consistency - if using abbreviations, use them consistently throughout.

---

#### 7. Unclear Property Name: `BuildTimeData`

**Location**: `src/framework/MiCake.Core/Abstractions/MiCakeApplicationOptions.cs:38`

**Current**:
```csharp
public DataDepositPool BuildTimeData { get; set; } = new DataDepositPool();
```

**Issue**: "BuildTimeData" and "DataDepositPool" are not immediately clear about their purpose.

**Recommendation**:
```csharp
/// <summary>
/// Temporary data storage for cross-module communication during the build phase.
/// Data stored here is only available during application configuration.
/// </summary>
public BuildPhaseDataStore BuildPhaseData { get; set; } = new BuildPhaseDataStore();
```

---

#### 8. Interface Method Naming: `CommonFilterQueryAsync`

**Location**: `src/framework/MiCake/DDD/Infrastructure/Paging/IRepositoryHasPagingQuery.cs`

**Current**:
```csharp
Task<IEnumerable<TAggregateRoot>> CommonFilterQueryAsync(FilterGroup filterGroup, ...);
Task<PagingResponse<TAggregateRoot>> CommonFilterPagingQueryAsync(PagingRequest pagingRequest, ...);
```

**Issue**: The prefix "Common" is vague and doesn't clearly describe what makes these queries "common".

**Recommended Fix**:
```csharp
Task<IEnumerable<TAggregateRoot>> FilterQueryAsync(FilterGroup filterGroup, ...);
Task<PagingResponse<TAggregateRoot>> FilterPagingQueryAsync(PagingRequest pagingRequest, ...);
```

Or if "dynamic" filtering is the intent:
```csharp
Task<IEnumerable<TAggregateRoot>> DynamicFilterQueryAsync(FilterGroup filterGroup, ...);
```

---

### 🟢 Minor Issues (Consider Fixing)

#### 9. Comment Typos in XML Documentation

**Locations**:
- `IEFSaveChangesLifetime.cs:16` - "entites" should be "entities"
- `MiCakeAuditOptions.cs:12` - "Defalut" should be "Default"
- `MiCakeAuditOptions.cs:18` - "Defalut" should be "Default"
- `IRepository.cs:7` - Comment grammar could be improved
- `IReadOnlyRepository.cs:9` - "A Repository only has get method" - could be clearer

**Recommendation**: Fix spelling errors in documentation to maintain professionalism.

---

#### 10. Inconsistent Naming for Lifetime/Lifecycle Interfaces

**Locations**:
- `IRepositoryLifetime` - Uses "Lifetime"
- `IRepositoryPreSaveChanges` - Uses "Pre" prefix style
- `IRepositoryPostSaveChanges` - Uses "Post" prefix style
- `IEFSaveChangesLifetime` - Uses "Lifetime"
- `IUnitOfWorkLifecycleHook` - Uses "LifecycleHook"

**Issue**: Mixed terminology between "Lifetime" and "Lifecycle" and "Hook".

**Recommendation**: Standardize on one term:
- "Lifetime" for service lifetime patterns (matching .NET DI concepts)
- "Lifecycle" for component lifecycle events
- "Hook" for extensibility points

---

#### 11. Boolean Property Naming

**Locations**:
- `IsAutoRegisterServices` in `IMiCakeModule.cs`
- `IsFrameworkLevel` in `IMiCakeModule.cs`
- `IsAutoUowEnabled` in `MiCakeAspNetOptions.cs`

**Issue**: While these follow the `Is` prefix convention, some could be more descriptive:
- `IsAutoRegisterServices` → `AutoRegisterServices` or `EnableAutoServiceRegistration`
- `IsAutoUowEnabled` → `EnableAutoUnitOfWork` or `AutoCreateUnitOfWork`

**Recommendation**: Consider whether the `Is` prefix adds clarity or is redundant.

---

#### 12. Delegate Naming

**Location**: `src/framework/MiCake.Core/Abstractions/DependencyInjection/FindAutoServiceTypesDelegate.cs`

**Current**: `FindAutoServiceTypesDelegate`

**Issue**: Delegate names typically use the pattern `{Action}Handler` or `{Action}Callback` or simply describe the function signature.

**Alternatives**:
- `AutoServiceTypeFinder`
- `ServiceTypeDiscoveryHandler`
- `FindServiceTypesFunc`

---

#### 13. Extension Method Class Naming Inconsistency

**Locations**:
- `MiCakeServicesExtension` (singular)
- `MiCakeAspNetServicesExtension` (singular)
- `AutoRegisterRepositoriesExtension` (singular)
- `MiCakeBuilderAspNetCoreExtension` (singular)
- `MiCakeBuilderEFCoreExtension` (singular)
- `CollectionExtensions` (plural)
- `ListExtensions` (plural)

**Issue**: Mixed use of singular "Extension" and plural "Extensions" suffixes.

**Recommendation**: Standardize on plural "Extensions" suffix (matching .NET BCL convention like `StringExtensions`).

---

#### 14. Enum Value Naming

**Location**: `src/framework/MiCake.Core/Abstractions/DependencyInjection/MiCakeServiceLifeTime.cs`

**Current**: File is named `MiCakeServiceLifeTime.cs` but enum is `MiCakeServiceLifetime`

**Issue**: File name has space in "LifeTime" while the enum uses "Lifetime" (no space).

**Recommendation**: Rename file to match enum: `MiCakeServiceLifetime.cs`

---

#### 15. Data Wrapper Options Class Name

**Location**: `src/framework/MiCake.AspNetCore/Responses/ResponseWrapperOptions.cs:80`

**Current**: `DataWrapperDefaultCode`

**Issue**: The class name suggests "Code" (singular) but it contains multiple codes for different scenarios (Success, ProblemDetails, Error).

**Recommended Fix**: `DataWrapperDefaultCodes` or `ResponseStatusCodes`

---

#### 16. Parameter Naming in Extension Methods

**Location**: `src/framework/MiCake.AspNetCore/Modules/MiCakeBuilderAspNetCoreExtension.cs:27`

**Current**:
```csharp
public static IMiCakeBuilder UseAspNetCore(
    this IMiCakeBuilder builder,
    Action<MiCakeAspNetOptions>? optionsBulder)
```

**Issue**: `optionsBulder` is misspelled (should be `optionsBuilder`)

**Recommended Fix**: `optionsBuilder`

---

## Summary by Category

| Category | Count | Description |
|----------|-------|-------------|
| 🔴 Critical | 1 | Must be fixed - typo in public API method name |
| 🟡 Important | 8 | Should be fixed - impacts usability and consistency |
| 🟢 Minor | 8 | Consider fixing - documentation and style improvements |

---

## Recommendations Summary

### Immediate Actions (Critical)

1. **Fix `HandleAysnc` typo** → `HandleAsync` in `IDomainEventHandler.cs`

### Short-term Actions (Important)

2. Standardize options class naming (use plural "Options" suffix)
3. Rename `EntryType` → `EntryModuleType` for clarity
4. Fix `optionsBulder` typo → `optionsBuilder`
5. Clarify `CommonFilterQueryAsync` method names
6. Improve `BuildTimeData` property naming

### Long-term Actions (Minor)

7. Fix all documentation typos
8. Standardize extension class naming (use "Extensions" plural)
9. Review and standardize lifecycle/lifetime terminology
10. Consider full names for EF Core repository classes

---

## Best Practices Reference

### Naming Guidelines Used

1. **PascalCase** for public types, methods, and properties
2. **Async suffix** for async methods
3. **Options suffix** (plural) for configuration classes
4. **Clear, descriptive names** over abbreviations for public APIs
5. **Consistent terminology** across related concepts
6. **No typos** in public API names (breaking changes to fix later)

### References

- [Microsoft .NET Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines)
- [MiCake Development Principles](principles/development_principle.md)
- [Domain-Driven Design Reference](https://domainlanguage.com/ddd/reference/)

---

## Conclusion

The MiCake framework demonstrates generally good naming conventions aligned with .NET standards and DDD principles. The most critical issue is the typo in `HandleAysnc` which should be addressed immediately as it affects the core domain event handling API.

The framework would benefit from:
1. A consistency pass to standardize similar naming patterns
2. Documentation typo fixes
3. More descriptive names for some ambiguous concepts

Overall, the naming quality is professional and follows industry standards with the exceptions noted above.
