using MiCake.Cord.Storage.Internal;
using Microsoft.EntityFrameworkCore;

namespace MiCake.EntityFrameworkCore.StorageInterpretor
{
    /// <summary>
    /// This is an internal API  not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// </summary>
    internal interface IConfigModelBuilderStrategy
    {
        /// <summary>
        /// Configure the regulation of <see cref="StoreEntityType"/> to <see cref="ModelBuilder"/> according to a rule
        /// </summary>
        ModelBuilder Config(ModelBuilder modelBuilder, StoreEntityType storeEntity, Type efModelType);
    }
}
