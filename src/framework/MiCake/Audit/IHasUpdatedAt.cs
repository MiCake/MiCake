using System;

namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has modification time.
    /// <para>
    /// This interface inherits from <see cref="IHasUpdatedAt{T}"/> with <see cref="DateTime"/> type.
    /// Consider using <see cref="IHasUpdatedAt{T}"/> directly with <see cref="DateTimeOffset"/> for better timezone support.
    /// </para>
    /// </summary>
    public interface IHasUpdatedAt : IHasUpdatedAt<DateTime>
    {
    }
}
