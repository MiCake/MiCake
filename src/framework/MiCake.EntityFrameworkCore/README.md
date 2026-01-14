# MiCake.EntityFrameworkCore

Entity Framework Core integration for the MiCake DDD toolkit.

## Overview

`MiCake.EntityFrameworkCore` provides EF Core support for MiCake:

- **MiCakeDbContext** - Enhanced DbContext with DDD support
- **Repository Implementation** - Auto-generated repositories
- **Unit of Work** - Transaction management with EF Core
- **Store Conventions** - Automatic entity configuration
- **Domain Event Dispatch** - Events triggered on SaveChanges

## Installation

```bash
dotnet add package MiCake.EntityFrameworkCore
```

## Quick Start

```csharp
// Inherit from MiCakeDbContext
public class AppDbContext : MiCakeDbContext
{
    public DbSet<Order> Orders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Required!
        // Your configurations...
    }
}
```

## Key Features

| Feature | Description |
|---------|-------------|
| `MiCakeDbContext` | Base DbContext with DDD integration |
| `IRepository<T, TKey>` | Auto-implemented repository |
| `IEFSaveChangesLifetime` | SaveChanges lifecycle hooks |
| `BypassUnitOfWorkCheck` | Access DbContext without UoW (read-only) |

## Documentation

📚 [Full Documentation](https://micake.github.io)

## License

MIT License - see [LICENSE](https://github.com/MiCake/MiCake/blob/master/LICENSE)
