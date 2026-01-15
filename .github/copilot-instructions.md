# MiCake Framework - AI Coding Agent Instructions

## Architecture Overview

MiCake is a Domain-Driven Design (DDD) toolkit for .NET Core that provides a modular framework for building DDD applications. The framework is designed to be "lightweight" and non-intrusive.

### Core Structure
- **Framework Core**: `src/framework/` contains all framework packages
- **Module System**: Built around `MiCakeModule` base class with lifecycle hooks
- **Sample Application**: `samples/BaseMiCakeApplication/` demonstrates integration patterns
- **Test Structure**: `src/tests/` with comprehensive unit tests

## Essential Patterns

### Module System
Every MiCake application requires an entry module inheriting from `MiCakeModule`:

```csharp
public class MyEntryModule : MiCakeModule
{
    public override Task ConfigServices(ModuleConfigServiceContext context)
    {
        // Auto-register repositories
        context.AutoRegisterRepositories(typeof(MyEntryModule).Assembly);
        return base.ConfigServices(context);
    }
}
```

**Module Lifecycle Order**: PreConfigServices → ConfigServices → PostConfigServices → PreInitialization → Initialization → PostInitialization

### DDD Domain Objects

**Entities** inherit from `Entity<TKey>` (or `Entity` for int keys):
```csharp
public class MyEntity : Entity<int>
{
    // Domain events are automatically handled
    public void DoSomething()
    {
        AddDomainEvent(new SomethingHappenedEvent(Id));
    }
}
```

**Aggregate Roots** inherit from `AggregateRoot<TKey>`:
```csharp
public class MyAggregate : AggregateRoot<int>
{
    // Repository operations target aggregate roots
}
```

**Value Objects** inherit from `ValueObject` or use `RecordValueObject`:
```csharp
public class Money : ValueObject
{
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

### Repository Pattern
- **Auto-generated**: Repositories are created via `IRepositoryFactory<TAggregateRoot, TKey>`
- **Provider Pattern**: Actual data access via `IRepositoryProvider<TAggregateRoot, TKey>`
- **Domain Events**: Automatically dispatched on `SaveChangesAsync()`

```csharp
// In module ConfigServices
context.AutoRegisterRepositories(typeof(MyModule).Assembly);

// Usage (injected automatically)
public class MyService
{
    private readonly IRepository<MyAggregate, int> _repository;
    
    public async Task DoWork()
    {
        var aggregate = await _repository.FindAsync(id);
        aggregate.DoSomething(); // Adds domain events
        await _repository.SaveChangesAsync(); // Events auto-dispatched
    }
}
```

## Build & Test Workflows

### Build Process
- **Primary**: `build.cmd` → `default.ps1` PowerShell script
- **Solution**: `MiCake.All.sln` contains all projects
- **Structure**: Framework projects in `src/framework/`, tests in `src/tests/`

### Testing
- **Command**: `build.cmd` runs full test suite with coverage
- **Coverage**: Uses Coverlet with Cobertura format
- **Settings**: Test configuration in `src/tests/runsettings.xml`
- **Individual**: `dotnet test <project>` for specific test projects

### Build Configuration
- **Directory.Build.props**: Shared MSBuild properties across solution
- **Target Framework**: .NET Core 5.0+
- **Test Results**: Generated in `TestResults/` directory

## Framework-Specific Conventions

### Assembly Scanning
MiCake auto-discovers domain objects from assemblies specified in `MiCakeApplicationOptions.DomainLayerAssemblies`. Set this in module configuration:

```csharp
public override Task ConfigServices(ModuleConfigServiceContext context)
{
    // Framework will scan this assembly for entities, aggregates, value objects
    context.Services.Configure<MiCakeApplicationOptions>(options => 
        options.DomainLayerAssemblies = new[] { typeof(MyModule).Assembly });
}
```

### Dependency Injection Auto-Registration
- Implement `ITransientService`, `ISingletonService`, or `IScopedService` for auto-registration
- Repository auto-registration via `context.AutoRegisterRepositories(assembly)`
- Module-level service configuration in `ConfigServices` method

### DbContext Integration
Inherit from `MiCakeDbContext` for EF Core integration:

```csharp
public class MyDbContext : MiCakeDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // REQUIRED - configures DDD entities
        // Your additional configuration
    }
}
```

### Exception Handling
Custom exception handlers implement `IMiCakeExceptionHandler` and are registered in module configuration:

```csharp
config.Handlers.Add(new MyExceptionHandler());
```

## Key Integration Points

### Module Dependencies
Use `DependsOn` attribute for module dependencies (framework auto-resolves dependency order).

### Data Wrapping
MiCake provides response wrapping features - configure via `MiCakeAspNetOptions.DataWrapperOptions`.

### Domain Event Dispatching
Events from entities are automatically collected and dispatched during repository `SaveChangesAsync()` via `DomainEventsRepositoryLifetime`.

### Audit Support
Built-in audit support via `UseAudit()` extension - tracks creation/modification times automatically.

## Common Pitfalls

- **Missing base.OnModelCreating()**: Required in DbContext for proper DDD entity configuration
- **Module registration order**: Framework handles dependency resolution, but entry module must be specified
- **StartMiCake() placement**: Must be called before `UseEndpoints()` in pipeline
- **Repository scope**: Always work with aggregate roots, not individual entities via repositories

## Development Principle
+ The doc `principles\development_principle.md` lists the development principles for MiCake framework.
