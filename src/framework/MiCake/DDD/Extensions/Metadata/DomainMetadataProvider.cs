﻿using MiCake.Core;
using MiCake.Core.Modularity;
using MiCake.DDD.Domain.Helper;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Reflection;

namespace MiCake.DDD.Extensions.Metadata
{
    internal class DomainMetadataProvider : IDomainMetadataProvider
    {
        private readonly DomainObjectFactory _domainObjectFactory;
        private readonly Assembly[] _domainLayerAsm;

        public DomainMetadataProvider(
            IMiCakeModuleContext moduleContext,
            IOptions<MiCakeApplicationOptions> appOptions,
            DomainObjectFactory domainObjectFactory)
        {
            _domainObjectFactory = domainObjectFactory;

            var exceptModules = moduleContext.MiCakeModules
                                             .Where(s => !s.Instance.IsFrameworkLevel)
                                             .ToMiCakeModuleCollection();

            _domainLayerAsm = appOptions.Value.DomainLayerAssemblies ?? GetDomainLayer(exceptModules);
        }

        public DomainMetadata GetDomainMetadata()
        {
            var domainObject = _domainObjectFactory.CreateDomainObjectModel(_domainLayerAsm);

            return new DomainMetadata(_domainLayerAsm, domainObject);
        }

        private static Assembly[] GetDomainLayer(IMiCakeModuleCollection miCakeModules)
        {
            return miCakeModules.GetAssemblies(false).Where(asm =>
                            asm.GetTypes().AsEnumerable().Any(inModuleType =>
                                            DomainTypeHelper.IsDomainObject(inModuleType))).ToArray();
        }

    }
}
