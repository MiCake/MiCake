using Xunit;

namespace MiCake.EntityFrameworkCore.Tests.Extensions
{
    /// <summary>
    /// Tests that set, clear, or depend on the process-wide convention engine share this collection,
    /// so they never run concurrently with each other or with other test collections.
    /// </summary>
    [CollectionDefinition("MiCakeStaticFactory", DisableParallelization = true)]
    public class MiCakeConventionStateCollection { }
}
