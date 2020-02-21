using System;

namespace MiCake.Audit
{
    public class AuditContext : IAuditContext
    {
        private IAuditObjectSetter _objectSetter;
        public IAuditObjectSetter ObjectSetter => _objectSetter;

        private readonly IServiceProvider _serviceProvider;

        public AuditContext(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _objectSetter = new DefaultAuditObjectSetter(serviceProvider);
        }
    }
}
