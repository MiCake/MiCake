using MiCake.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

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
