using System;

namespace MiCake.Audit
{
    /// <summary>
    /// Define a class has creation time.
    /// <para>
    /// This interface inherits from <see cref="IHasCreatedAt{T}"/> with <see cref="DateTime"/> type.
    /// Consider using <see cref="IHasCreatedAt{T}"/> directly with <see cref="DateTimeOffset"/> for better timezone support.
    /// </para>
    /// </summary>
    public interface IHasCreatedAt : IHasCreatedAt<DateTime>
    {
    }
}
