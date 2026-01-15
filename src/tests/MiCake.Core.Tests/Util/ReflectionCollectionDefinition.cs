using Xunit;

namespace MiCake.Core.Tests.Util
{
    // Create a non-parallel test collection for reflection related tests
    [CollectionDefinition("ReflectionTests", DisableParallelization = true)]
    public class ReflectionTestsCollection
    {
        // Intentionally left blank: xUnit uses the attribute to define a named collection
    }
}