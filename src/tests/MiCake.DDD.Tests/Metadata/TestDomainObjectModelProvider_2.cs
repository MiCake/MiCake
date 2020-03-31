using MiCake.DDD.Extensions.Metadata;
using MiCake.DDD.Tests.Fakes.Entities;
using System;
using System.Linq;

namespace MiCake.DDD.Tests.Metadata
{
    public class TestDomainObjectModelProvider_2 : IDomainObjectModelProvider
    {
        public int Order => 0;

        private TestModelProviderRecorder _recorder;

        public TestDomainObjectModelProvider_2(TestModelProviderRecorder testModelProviderRecorder)
        {
            _recorder = testModelProviderRecorder;
        }

        public void OnProvidersExecuted(DomainObjectModelContext context)
        {
            var domainModel = context.Result;
            var entityADesc = domainModel.Entities.FirstOrDefault(s => s.Type.Equals(typeof(EntityA))) ??
                                throw new ArgumentNullException("not find entityA");

            _recorder.ModelProviderInfo.Add($"{nameof(TestDomainObjectModelProvider_2) + nameof(OnProvidersExecuted)}");
            _recorder.Additionals.Add(entityADesc);
        }

        public void OnProvidersExecuting(DomainObjectModelContext context)
        {
            _recorder.ModelProviderInfo.Add($"{nameof(TestDomainObjectModelProvider_2) + nameof(OnProvidersExecuting)}");
        }
    }
}
