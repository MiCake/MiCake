namespace MiCake.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Defines a test collection that disables parallel execution.
    /// Tests in this collection will run sequentially.
    /// </summary>
    [CollectionDefinition("Sequential", DisableParallelization = true)]
    public class SequentialTestCollection
    {
    }
}
