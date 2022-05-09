using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiCake.Core.Tests
{
    public class MiCakeApplication_Tests
    {
        public IServiceCollection Services { get; set; } = new ServiceCollection();

        public MiCakeApplication_Tests()
        {
            Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        }
    }
}
