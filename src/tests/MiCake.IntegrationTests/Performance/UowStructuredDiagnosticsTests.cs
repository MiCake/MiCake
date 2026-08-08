using MiCake.DDD.Uow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiCake.IntegrationTests.Performance
{
    /// <summary>
    /// Verifies that unit-of-work transaction diagnostics correlate the UoW, resource, and
    /// DbContext identity while excluding entity property values, SQL parameters, and
    /// connection strings. Uses a unique marker as the entity value so any leak fails the
    /// assertion, and the fixture's database path so any connection-string leak fails it.
    /// </summary>
    [Collection("PerformanceBaseline")]
    public class UowStructuredDiagnosticsTests
    {
        [Fact]
        public async Task TransactionDiagnostics_CorrelateIdentityAndExcludeSensitiveData()
        {
            var entries = new List<LogEntry>();
            using var fixture = new Uow.SqliteUnitOfWorkFixture();
            using var provider = fixture.BuildProvider(configure: s =>
                s.AddLogging(b =>
                {
                    // The default LoggerFilterOptions minimum level (Information) would
                    // drop the MiCake Debug-level transaction diagnostics; capture Trace+.
                    b.SetMinimumLevel(LogLevel.Trace);
                    b.AddProvider(new CapturingLoggerProvider(entries));
                }));

            await using (var initScope = provider.CreateAsyncScope())
            {
                await initScope.ServiceProvider
                    .GetRequiredService<Uow.UowAcceptanceDbContext>()
                    .Database.EnsureCreatedAsync();
            }

            var marker = $"diag-marker-{Guid.NewGuid():N}";
            await using (var scope = provider.CreateAsyncScope())
            {
                var manager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                await using var uow = await manager.BeginAsync();
                var context = Uow.SqliteUnitOfWorkFixture.GetPrimaryContext(scope.ServiceProvider);
                context.Aggregates.Add(new Uow.UowAcceptanceAggregate(marker));
                await uow.CommitAsync();
            }

            // Correlation fields: unit-of-work id, resource, and DbContext type in MiCake
            // diagnostics (EF Core connection/command logs are outside the framework contract).
            // NOTE: the framework emits no stable EventId for these diagnostics, so the
            // assertions rely on message wording (case-insensitive, combined keywords); if
            // stable event IDs are introduced later, migrate these assertions to them.
            var miCakeMessages = entries
                .Where(e => e.Category.StartsWith("MiCake", StringComparison.Ordinal))
                .Select(e => e.Message)
                .ToList();

            Assert.NotEmpty(miCakeMessages);
            Assert.Contains(
                miCakeMessages,
                m => m.Contains("UoW", StringComparison.OrdinalIgnoreCase)
                    && m.Contains("resource", StringComparison.OrdinalIgnoreCase)
                    && m.Contains(nameof(Uow.UowAcceptanceDbContext), StringComparison.OrdinalIgnoreCase));

            // Sensitive exclusion: the entity value marker, the database file path, and any
            // connection-string fragment must never appear in MiCake diagnostics.
            Assert.DoesNotContain(miCakeMessages, m => m.Contains(marker, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(miCakeMessages, m => m.Contains("micake-uow-acceptance", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(miCakeMessages, m => m.Contains("Data Source", StringComparison.OrdinalIgnoreCase));
        }

        private sealed record LogEntry(string Category, string Message);

        private sealed class CapturingLoggerProvider : ILoggerProvider
        {
            private readonly object _gate = new();
            private readonly List<LogEntry> _entries;

            public CapturingLoggerProvider(List<LogEntry> entries)
            {
                _entries = entries;
            }

            public ILogger CreateLogger(string categoryName) => new CapturingLogger(_gate, _entries, categoryName);

            public void Dispose()
            {
            }

            private sealed class CapturingLogger : ILogger
            {
                private readonly object _gate;
                private readonly List<LogEntry> _entries;
                private readonly string _categoryName;

                public CapturingLogger(object gate, List<LogEntry> entries, string categoryName)
                {
                    _gate = gate;
                    _entries = entries;
                    _categoryName = categoryName;
                }

                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(
                    LogLevel logLevel,
                    EventId eventId,
                    TState state,
                    Exception? exception,
                    Func<TState, Exception?, string> formatter)
                {
                    lock (_gate)
                    {
                        _entries.Add(new LogEntry(_categoryName, formatter(state, exception)));
                    }
                }
            }
        }
    }
}
