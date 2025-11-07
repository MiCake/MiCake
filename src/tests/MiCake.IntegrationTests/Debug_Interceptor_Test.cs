using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit.Abstractions;

namespace MiCake.IntegrationTests
{
    /// <summary>
    /// Debug tests to verify Interceptor registration
    /// </summary>
    public class Debug_Interceptor_Test : Infrastructure.IntegrationTestBase
    {
        private readonly ITestOutputHelper _output;

        public Debug_Interceptor_Test(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Debug_CheckInterceptorRegistration()
        {
            _output.WriteLine("Getting DbContext...");
            
            // Get DbContext - this should trigger OnConfiguring
            var dbContext = GetDbContext();
            
            _output.WriteLine($"DbContext type: {dbContext.GetType().FullName}");
            _output.WriteLine($"DbContext created successfully");
            
            // Try to get interceptors from the service provider
            var interceptors = ServiceProvider.GetServices<IInterceptor>();
            _output.WriteLine($"Registered Interceptors count: {interceptors.Count()}");
            
            foreach (var interceptor in interceptors)
            {
                _output.WriteLine($"  - {interceptor.GetType().FullName}");
            }
        }

        [Fact]
        public async Task Debug_CheckSaveChangesFlow()
        {
            _output.WriteLine("=== Starting SaveChanges Flow Test ===");
            
            var dbContext = GetDbContext();
            var product = new Fakes.Product
            {
                Name = "Debug Product",
                Price = 100m,
                Stock = 10
            };
            
            _output.WriteLine($"Before Add: CreationTime = {product.CreationTime}");
            
            dbContext.Products.Add(product);
            _output.WriteLine("After Add (before SaveChanges)");
            
            await dbContext.SaveChangesAsync();
            _output.WriteLine($"After SaveChanges: CreationTime = {product.CreationTime}");
            
            Assert.NotEqual(default(DateTime), product.CreationTime);
        }
    }
}
