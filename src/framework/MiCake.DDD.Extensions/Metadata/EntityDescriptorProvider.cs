using MiCake.DDD.Domain.Helper;
using System;

namespace MiCake.DDD.Extensions.Metadata
{
    public sealed class EntityDescriptorProvider : IObjectDescriptorProvider
    {
        public void Dispose()
        {
        }

        public IObjectDescriptor GetDescriptor(Type type)
        {
            if (!EntityHelper.IsEntity(type))
                return null;

            return new EntityDescriptor(type);
        }
    }
}
