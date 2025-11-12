using MiCake.DDD.Uow;
using System;
using System.Data;

namespace MiCake.AspNetCore.Uow
{
    /// <summary>
    /// Attribute to control Unit of Work behavior at Controller or Action level.
    /// When applied, overrides the default automatic transaction behavior configured in MiCakeAspNetOptions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UnitOfWorkAttribute : Attribute
    {
        /// <summary>
        /// Whether to enable automatic transaction management for this controller/action.
        /// When null, uses the default configuration from MiCakeAspNetOptions.
        /// </summary>
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// Whether this is a read-only operation (optimization for queries).
        /// Read-only operations will skip transaction commit to improve performance.
        /// Default is false.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;

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
        /// Creates a new UnitOfWorkAttribute with specified enabled state
        /// </summary>
        /// <param name="isEnabled">Whether to enable UoW for this controller/action</param>
        public UnitOfWorkAttribute(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Creates UnitOfWorkOptions based on this attribute's settings
        /// </summary>
        internal UnitOfWorkOptions CreateOptions()
        {
            return new UnitOfWorkOptions
            {
                AutoBeginTransaction = IsEnabled ?? true,
                IsReadOnly = IsReadOnly,
                IsolationLevel = IsolationLevel,
                InitializationMode = InitializationMode
            };
        }
    }

    /// <summary>
    /// Disables automatic Unit of Work management for the decorated controller or action.
    /// Equivalent to [UnitOfWork(IsEnabled = false)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DisableUnitOfWorkAttribute : UnitOfWorkAttribute
    {
        /// <summary>
        /// Creates a new DisableUnitOfWorkAttribute
        /// </summary>
        public DisableUnitOfWorkAttribute() : base(false)
        {
        }
    }
}
