using MiCake.DDD.Uow;
using System;
using System.Data;

namespace MiCake.AspNetCore.Uow
{
    /// <summary>
    /// Attribute to control Unit of Work behavior at Controller or Action level.
    /// Applying this attribute enables Unit of Work for the controller or action.
    /// Use <see cref="DisableUnitOfWorkAttribute"/> to explicitly disable UoW.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UnitOfWorkAttribute : Attribute
    {
        /// <summary>
        /// Transaction isolation level for this operation.
        /// Default is ReadCommitted.
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.ReadCommitted;

        /// <summary>
        /// Transaction initialization mode (Lazy or Immediate).
        /// Default is Lazy (transactions start when first resource is accessed).
        /// </summary>
        public TransactionInitializationMode InitializationMode { get; set; } = TransactionInitializationMode.Lazy;

        /// <summary>
        /// Creates a new UnitOfWorkAttribute with default settings
        /// </summary>
        public UnitOfWorkAttribute()
        {
        }

        /// <summary>
        /// Creates UnitOfWorkOptions based on this attribute's settings
        /// </summary>
        internal UnitOfWorkOptions CreateOptions()
        {
            return new UnitOfWorkOptions
            {
                IsolationLevel = IsolationLevel,
                InitializationMode = InitializationMode,
                IsReadOnly = false
            };
        }

        /// <summary>
        /// Indicates whether this attribute enables UoW (true for base class, false for DisableUnitOfWorkAttribute)
        /// </summary>
        internal virtual bool IsUowEnabled => true;
    }

    /// <summary>
    /// Disables automatic Unit of Work management for the decorated controller or action.
    /// Use this to explicitly opt-out of UoW when it's enabled globally.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DisableUnitOfWorkAttribute : UnitOfWorkAttribute
    {
        /// <summary>
        /// Creates a new DisableUnitOfWorkAttribute
        /// </summary>
        public DisableUnitOfWorkAttribute()
        {
        }

        /// <summary>
        /// Indicates this attribute disables UoW
        /// </summary>
        internal override bool IsUowEnabled => false;
    }
}
