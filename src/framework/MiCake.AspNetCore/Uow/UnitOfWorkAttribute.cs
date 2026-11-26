using System;
using System.Data;

namespace MiCake.AspNetCore.Uow
{
    /// <summary>
    /// Attribute to control Unit of Work behavior at Controller or Action level.
    /// Applying this attribute enables Unit of Work for the controller or action.
    /// Use <see cref="DisableUnitOfWorkAttribute"/> to explicitly disable UoW.
    /// </summary>
    /// <remarks>
    /// <see cref="IsReadOnly"/> marks the operation as read-only. Read-only units of work
    /// reject resource flush and write activation, so every attempted write fails before
    /// a command executes. Explicit read-only metadata overrides action-name inference.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class UnitOfWorkAttribute : Attribute
    {
        /// <summary>
        /// Whether this operation is read-only.
        /// When true, the unit of work is marked read-only and rejects every write path.
        /// When false, the unit of work commits on successful action execution.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Transaction isolation level for this operation.
        /// When null, the unit of work default (ReadCommitted) is used.
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }
    }

    /// <summary>
    /// Disables automatic Unit of Work management for the decorated controller or action.
    /// Use this to explicitly opt-out of UoW when it's enabled globally.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class DisableUnitOfWorkAttribute : Attribute
    {
    }
}
