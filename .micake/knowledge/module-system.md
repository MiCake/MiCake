# MiCake Module System

## Overview

MiCake uses a module system for organizing framework and application code. Modules have explicit dependencies and defined lifecycles.

## Module Definition

```csharp
public class MyModule : MiCakeModule
{
    public override Task PreConfigServices(ModuleConfigServiceContext context)
    {
        // Before service registration
        return Task.CompletedTask;
    }

    public override Task ConfigServices(ModuleConfigServiceContext context)
    {
        // Register services
        context.Services.AddScoped<IMyService, MyService>();
        return Task.CompletedTask;
    }

    public override Task PostConfigServices(ModuleConfigServiceContext context)
    {
        // After service registration
        return Task.CompletedTask;
    }

    public override Task PreInitialization(ModuleLoadContext context)
    {
        // Before app initialization
        return Task.CompletedTask;
    }

    public override Task Initialization(ModuleLoadContext context)
    {
        // App initialization
        return Task.CompletedTask;
    }

    public override Task PostInitialization(ModuleLoadContext context)
    {
        // After initialization
        return Task.CompletedTask;
    }

    public override Task Shutdown(ModuleLoadContext context)
    {
        // Cleanup on shutdown
        return Task.CompletedTask;
    }
}
```

## Lifecycle Order

```
1. PreConfigServices   → All modules, dependency order
2. ConfigServices      → All modules, dependency order
3. PostConfigServices  → All modules, dependency order
   --- Application builds ---
4. PreInitialization   → All modules, dependency order
5. Initialization      → All modules, dependency order
6. PostInitialization  → All modules, dependency order
   --- Application runs ---
7. Shutdown            → All modules, reverse order
```

## Module Dependencies

Use `[RelyOn]` attribute to declare dependencies:

```csharp
[RelyOn(typeof(MiCakeCoreModule))]
[RelyOn(typeof(MiCakeDDDModule))]
public class MyModule : MiCakeModule
{
}
```

Rules:
- Dependencies are resolved automatically
- Circular dependencies are not allowed
- Missing dependencies cause startup failure

## Framework Modules

| Module | Layer | Purpose |
|--------|-------|---------|
| MiCakeRootModule | Core | Root module, always loaded |
| MiCakeEssentialModule | DDD | Essential DDD services |
| MiCakeAspNetCoreModule | AspNetCore | Web integration |
| MiCakeEFCoreModule | EntityFrameworkCore | EF Core support |

## Common Patterns

### Auto-Register Repositories

```csharp
public override Task ConfigServices(ModuleConfigServiceContext context)
{
    context.AutoRegisterRepositories(typeof(MyModule).Assembly);
    return Task.CompletedTask;
}
```

### Configure Options

```csharp
public override Task ConfigServices(ModuleConfigServiceContext context)
{
    context.Services.Configure<MiCakeApplicationOptions>(options =>
    {
        options.DomainLayerAssemblies = new[] { typeof(MyModule).Assembly };
    });
    return Task.CompletedTask;
}
```

### Register Services

```csharp
public override Task ConfigServices(ModuleConfigServiceContext context)
{
    context.Services.AddScoped<IMyService, MyService>();
    context.Services.AddSingleton<ICache, MemoryCache>();
    return Task.CompletedTask;
}
```

## Best Practices

1. **Single responsibility**: One module, one purpose
2. **Explicit dependencies**: Always use [RelyOn]
3. **Minimal surface**: Export only necessary types
4. **Sensible defaults**: Override only needed hooks
5. **Fail fast**: Validate configuration in PreConfigServices
