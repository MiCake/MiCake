using System;

namespace MiCake.DDD.Extensions.Metadata
{
    /// /// <summary>
    /// Represents a type that can create instances of <see cref="ILogger"/>.
    /// </summary>
    /// <typeparam name="TDescriptor"></typeparam>
    public interface IObjectDescriptorProvider : IDisposable
    {
        IObjectDescriptor GetDescriptor(Type type);
    }
}
