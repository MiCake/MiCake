﻿namespace MiCake.DDD.Connector.Metadata
{
    /// <summary>
    /// Provider a way to get <see cref="DomainMetadata"/>
    /// </summary>
    public interface IDomainMetadataProvider
    {
        DomainMetadata GetDomainMetadata();
    }
}
