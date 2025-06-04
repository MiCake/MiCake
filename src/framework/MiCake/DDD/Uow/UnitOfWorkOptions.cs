using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data;

namespace MiCake.DDD.Uow
{
    public class UnitOfWorkOptions
    {
        /// <summary>
        /// Specifies the transaction locking behavior for the connection.
        /// <see cref="IsolationLevel"/>
        /// </summary>
        public IsolationLevel? IsolationLevel { get; set; }

        /// <summary>
        /// Time out config.
        /// </summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Indicate the scope of the unit of work to be created.
        /// <see cref="UnitOfWorkScope"/>
        /// </summary>
        public UnitOfWorkScope Scope { get; set; }

        /// <summary>
        /// Specifies a <see cref="IServiceScope"/> to get the unit of work. 
        /// If the value is not specified, a new scope is created using the default <see cref="IServiceProvider"/>.
        /// <para>
        ///     If you specify this parameter, it means you will control the release of the scope yourself.
        /// </para>
        /// </summary>
        public IServiceScope ServiceScope { get; set; }

        /// <summary>
        /// Specifies events which the <see cref="IUnitOfWork"/> invokes to enable developer control unit of work process.
        /// <see cref="UnitOfWorkEvents"/>
        /// </summary>
        public UnitOfWorkEvents Events { get; set; } = new UnitOfWorkEvents();

        public UnitOfWorkOptions() : this(default)
        {
        }

        public UnitOfWorkOptions(IsolationLevel? isolationLevel) :
            this(isolationLevel, null)
        {
        }

        public UnitOfWorkOptions(IsolationLevel? isolationLevel, TimeSpan? timeOut)
            : this(isolationLevel, timeOut, UnitOfWorkScope.Required)
        {
        }

        public UnitOfWorkOptions(IsolationLevel? isolationLevel, TimeSpan? timeOut, UnitOfWorkScope unitOfWorkLimit)
        {
            IsolationLevel = isolationLevel;
            Timeout = timeOut;
            Scope = unitOfWorkLimit;
        }

        /// <summary>
        /// Clone this options config to a new instance.
        /// </summary>
        public UnitOfWorkOptions Clone()
        {
            return new UnitOfWorkOptions()
            {
                Events = Events,
                IsolationLevel = IsolationLevel,
                Scope = Scope,
                Timeout = Timeout,
            };
        }
    }
}
