using MiCake.DDD.Domain;
using MiCake.DDD.Domain.Helper;
using System;

namespace MiCake.DDD.Extensions.Metadata
{
    /// <summary>
    /// Describes an <see cref="IEntity"/>
    /// </summary>
    public class EntityDescriptor : ObjectDescriptor
    {
        private Type _keyType;

        /// <summary>
        /// The primary key of <see cref="IEntity"/>
        /// </summary>
        public Type PrimaryKey
        {
            get
            {
                if (_keyType == null)
                {
                    _keyType = EntityHelper.FindPrimaryKeyType(Type);
                }
                return _keyType;
            }
        }

        public EntityDescriptor(Type type) : base(type)
        {
        }
    }
}
