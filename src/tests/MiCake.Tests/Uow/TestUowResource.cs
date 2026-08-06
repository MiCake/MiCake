using MiCake.DDD.Uow;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.Tests.Uow
{
    /// <summary>
    /// Hand-written fake <see cref="IUnitOfWorkResource"/> for unit tests.
    /// Records every lifecycle call and supports per-call failure injection.
    /// </summary>
    internal sealed class TestUowResource : IUnitOfWorkResource
    {
        public UnitOfWorkResourceId Id { get; } = new(Guid.NewGuid());
        public string ResourceType => "TestResource";
        public bool HasActiveTransaction { get; private set; }
        public bool SupportsSavepoints { get; set; } = true;

        /// <summary>Display name used in call logs to distinguish resources.</summary>
        public string Name { get; set; } = "Resource";

        /// <summary>Optional shared log to assert global call ordering across resources.</summary>
        public List<string>? SharedCallLog { get; set; }

        /// <summary>Per-resource call log.</summary>
        public List<string> CallLog { get; } = [];

        /// <summary>
        /// When true, async lifecycle methods honor the cancellation token by throwing.
        /// Models a real provider that refuses work on a cancelled token.
        /// </summary>
        public bool ThrowIfCanceled { get; set; }

        public UnitOfWorkResourceContext? PrepareContext { get; private set; }
        public int PrepareCount { get; private set; }
        public int EnsureTransactionCount { get; private set; }
        public int FlushCount { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int AsyncDisposeCount { get; private set; }
        public int FlushResult { get; set; } = 1;

        public List<string> Savepoints { get; } = [];
        public List<string> RolledBackTo { get; } = [];
        public List<string> Released { get; } = [];

        public Exception? PrepareException { get; set; }
        public Exception? EnsureTransactionException { get; set; }
        public Exception? FlushException { get; set; }
        public Exception? CommitException { get; set; }
        public Exception? RollbackException { get; set; }
        public Exception? CreateSavepointException { get; set; }
        public Exception? DisposeException { get; set; }

        private void Record(string operation)
        {
            CallLog.Add($"{Name}.{operation}");
            SharedCallLog?.Add($"{Name}.{operation}");
        }

        public void Prepare(UnitOfWorkResourceContext context)
        {
            Record("Prepare");
            PrepareCount++;
            if (PrepareException != null)
                throw PrepareException;
            PrepareContext = context;
        }

        public void EnsureTransaction()
        {
            Record("EnsureTransaction");
            EnsureTransactionCount++;
            if (EnsureTransactionException != null)
                throw EnsureTransactionException;
            HasActiveTransaction = true;
        }

        public ValueTask EnsureTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (ThrowIfCanceled) cancellationToken.ThrowIfCancellationRequested();
            EnsureTransaction();
            return ValueTask.CompletedTask;
        }

        public ValueTask<int> FlushAsync(CancellationToken cancellationToken = default)
        {
            Record("Flush");
            FlushCount++;
            if (ThrowIfCanceled) cancellationToken.ThrowIfCancellationRequested();
            if (FlushException != null)
                throw FlushException;
            return ValueTask.FromResult(FlushResult);
        }

        public ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            Record("Commit");
            CommitCount++;
            if (ThrowIfCanceled) cancellationToken.ThrowIfCancellationRequested();
            if (CommitException != null)
                throw CommitException;
            HasActiveTransaction = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            Record("Rollback");
            RollbackCount++;
            if (ThrowIfCanceled) cancellationToken.ThrowIfCancellationRequested();
            if (RollbackException != null)
                throw RollbackException;
            HasActiveTransaction = false;
            return ValueTask.CompletedTask;
        }

        public ValueTask CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            Record("CreateSavepoint");
            if (!SupportsSavepoints)
                throw new NotSupportedException("Savepoints are not supported");
            if (ThrowIfCanceled) cancellationToken.ThrowIfCancellationRequested();
            if (CreateSavepointException != null)
                throw CreateSavepointException;
            Savepoints.Add(name);
            return ValueTask.CompletedTask;
        }

        public ValueTask RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!SupportsSavepoints)
                throw new NotSupportedException("Savepoints are not supported");
            RolledBackTo.Add(name);
            return ValueTask.CompletedTask;
        }

        public ValueTask ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!SupportsSavepoints)
                throw new NotSupportedException("Savepoints are not supported");
            Released.Add(name);
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            Record("Dispose");
            DisposeCount++;
            HasActiveTransaction = false;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposeCount++;
            Dispose();
            if (DisposeException != null)
                throw DisposeException;
            return ValueTask.CompletedTask;
        }
    }
}
