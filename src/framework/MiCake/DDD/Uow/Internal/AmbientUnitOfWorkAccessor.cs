using System;
using System.Threading;

namespace MiCake.DDD.Uow.Internal
{
    /// <summary>
    /// Token identifying a specific ambient unit of work frame for token-based compare-and-pop restoration.
    /// </summary>
    internal readonly record struct UnitOfWorkFrameToken(Guid Value)
    {
        public static UnitOfWorkFrameToken New() => new(Guid.NewGuid());
    }

    /// <summary>
    /// Immutable ambient unit of work frame.
    /// Each frame carries the unit of work, the service provider that owns it, and a link to the previous frame.
    /// </summary>
    internal sealed class UnitOfWorkFrame
    {
        public UnitOfWorkFrameToken Token { get; }
        public IUnitOfWork UnitOfWork { get; }
        public IServiceProvider ServiceProvider { get; }
        public UnitOfWorkFrame? Previous { get; }

        public UnitOfWorkFrame(
            UnitOfWorkFrameToken token,
            IUnitOfWork unitOfWork,
            IServiceProvider serviceProvider,
            UnitOfWorkFrame? previous)
        {
            Token = token;
            UnitOfWork = unitOfWork;
            ServiceProvider = serviceProvider;
            Previous = previous;
        }
    }

    /// <summary>
    /// Host-local ambient unit of work frame stack.
    /// A singleton AsyncLocal keeps frames flowing through async execution while remaining isolated per execution context.
    /// Frames are immutable; pushing creates a new head and popping restores the previous head via token-based compare-and-pop.
    /// </summary>
    internal sealed class AmbientUnitOfWorkAccessor
    {
        private readonly AsyncLocal<UnitOfWorkFrame?> _current = new();

        /// <summary>
        /// The current live ambient frame, or null when no unit of work is active in this execution context.
        /// Completed or disposed frames are treated as dead and are self-healed (popped) on read,
        /// because async completion APIs cannot reliably pop the caller's execution context.
        /// </summary>
        public UnitOfWorkFrame? Current
        {
            get
            {
                var current = _current.Value;
                while (current != null && (current.UnitOfWork.IsCompleted || current.UnitOfWork.IsDisposed))
                {
                    _current.Value = current.Previous;
                    current = current.Previous;
                }

                return current;
            }
        }

        /// <summary>
        /// Pushes a new frame as the current head.
        /// </summary>
        public void Push(UnitOfWorkFrame frame)
        {
            _current.Value = frame;
        }

        /// <summary>
        /// Restores the previous frame when the current frame matches <paramref name="token"/>.
        /// Idempotent and tolerant: if the stack already moved on, this is a no-op.
        /// </summary>
        public void Pop(UnitOfWorkFrameToken token)
        {
            var current = _current.Value;
            if (current == null || current.Token != token)
            {
                return;
            }

            _current.Value = current.Previous;
        }
    }
}
