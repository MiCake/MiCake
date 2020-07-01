using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MiCake.DDD.Extensions.Metadata
{
    internal class DomainObjectFactory
    {
        private IDomainObjectModelProvider[] _modelProviders;

        public DomainObjectFactory(IEnumerable<IDomainObjectModelProvider> modelProviders)
        {
            _modelProviders = modelProviders.OrderBy(p => p.Order).ToArray();
        }

        public DomainObjectModel CreateDomainObjectModel(Assembly[] domainLayerAsm)
        {
            var context = new DomainObjectModelContext(domainLayerAsm);

            for (var i = 0; i < _modelProviders.Length; i++)
            {
                _modelProviders[i].OnProvidersExecuting(context);
            }

            for (var i = _modelProviders.Length - 1; i >= 0; i--)
            {
                _modelProviders[i].OnProvidersExecuted(context);
            }

            return context.Result;
        }
    }
}
