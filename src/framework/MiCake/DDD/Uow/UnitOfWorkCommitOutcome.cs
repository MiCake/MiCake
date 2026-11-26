using System;
using System.Collections.Generic;

namespace MiCake.DDD.Uow
{
    /// <summary>
    /// Collision-safe identity of a unit of work resource instance.
    /// </summary>
    public readonly record struct UnitOfWorkResourceId(Guid Value);

    /// <summary>
    /// Commit state of a single resource during a unit of work commit.
    /// </summary>
    public enum UnitOfWorkResourceCommitState
    {
        /// <summary>Commit has not been attempted yet.</summary>
        Pending,

        /// <summary>The resource committed successfully and is durable.</summary>
        Committed,

        /// <summary>Committing the resource failed.</summary>
        Failed,

        /// <summary>The resource was rolled back successfully.</summary>
        RolledBack,

        /// <summary>Rolling back the resource failed.</summary>
        RollbackFailed
    }

    /// <summary>
    /// Per-resource outcome recorded during a unit of work commit.
    /// <see cref="CommitState"/> captures the commit attempt result (Pending / Committed / Failed)
    /// and is never overwritten by a later rollback; <see cref="RollbackState"/> records the
    /// compensating rollback result when a rollback attempt occurred (RolledBack / RollbackFailed).
    /// This keeps the identity of a commit-failed resource distinguishable from a resource that was
    /// never committed at all.
    /// </summary>
    public sealed record UnitOfWorkResourceOutcome(
        UnitOfWorkResourceId ResourceId,
        string ResourceType,
        UnitOfWorkResourceCommitState CommitState,
        UnitOfWorkResourceCommitState? RollbackState = null);

    /// <summary>
    /// Complete structured outcome of a unit of work commit attempt.
    /// Contains no sensitive data (no SQL, entity values, or connection strings).
    /// </summary>
    public sealed record UnitOfWorkCommitOutcome(
        Guid UnitOfWorkId,
        IReadOnlyList<UnitOfWorkResourceOutcome> Resources);
}
