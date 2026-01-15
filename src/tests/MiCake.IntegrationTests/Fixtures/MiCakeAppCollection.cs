[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MiCake.IntegrationTests.Fixtures
{
    [CollectionDefinition("MiCakeIntegrationTests")]
    public class MiCakeAppCollection : ICollectionFixture<MiCakeAppFixture>
    {
        // just a container for the collection
    }
}
