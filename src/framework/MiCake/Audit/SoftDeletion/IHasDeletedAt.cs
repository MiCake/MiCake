using System;

namespace MiCake.Audit.SoftDeletion
{
    /// <summary>
    /// Define a class has deletion time.
    /// <para>
    /// This interface inherits from <see cref="IHasDeletedAt{T}"/> with <see cref="DateTime"/> type.
    /// Consider using <see cref="IHasDeletedAt{T}"/> directly with <see cref="DateTimeOffset"/> for better timezone support.
    /// </para>
    /// </summary>
    public interface IHasDeletedAt : IHasDeletedAt<DateTime>
    {
    }
}
