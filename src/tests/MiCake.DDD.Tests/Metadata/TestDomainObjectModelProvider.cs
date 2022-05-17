using MiCake.Cord.Tests.Fakes.Entities;
using MiCake.DDD.Connector.Metadata;
using System;
using System.Linq;

namespace MiCake.Cord.Tests.Metadata
{
    internal class TestDomainObjectModelProvider : IDomainObjectModelProvider
    {
        public int Order => -1;

        private readonly TestModelProviderRecorder _recorder;

        public TestDomainObjectModelProvider(TestModelProviderRecorder testModelProviderRecorder)
        {
            _recorder = testModelProviderRecorder;
        }

        public void OnProvidersExecuted(DomainObjectModelContext context)
        {
            var domainModel = context.Result;
            var entityADesc = domainModel.Entities.FirstOrDefault(s => s.Type.Equals(typeof(GenericEntityA))) ??
                                throw new ArgumentNullException("not find entityA");

            _recorder.ModelProviderInfo.Add($"{nameof(TestDomainObjectModelProvider) + nameof(OnProvidersExecuted)}");
            _recorder.Additionals.Add(entityADesc);
        }

        public void OnProvidersExecuting(DomainObjectModelContext context)
        {
            _recorder.ModelProviderInfo.Add($"{nameof(TestDomainObjectModelProvider) + nameof(OnProvidersExecuting)}");
        }
    }
}
