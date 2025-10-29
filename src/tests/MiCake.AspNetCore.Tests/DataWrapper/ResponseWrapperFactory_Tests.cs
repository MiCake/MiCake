using MiCake.AspNetCore.DataWrapper;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    public class ResponseWrapperFactory_Tests
    {
        [Fact]
        public void CreateDefault_ReturnsValidFactory()
        {
            // Arrange
            var options = new DataWrapperOptions();

            // Act
            var factory = ResponseWrapperFactory.CreateDefault(options);

            // Assert
            Assert.NotNull(factory);
            Assert.NotNull(factory.SuccessFactory);
            Assert.NotNull(factory.ErrorFactory);
        }

        [Fact]
        public void DefaultSuccessFactory_CreatesStandardResponse()
        {
            // Arrange
            var options = new DataWrapperOptions();
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var context = new WrapperContext(null, 200, "test data");

            // Act
            var result = factory.SuccessFactory(context);

            // Assert
            Assert.IsType<ApiResponse>(result);
            var response = result as ApiResponse;
            Assert.Equal("0", response.Code);
            Assert.Equal("test data", response.Data);
        }

        [Fact]
        public void DefaultErrorFactory_CreatesErrorResponse()
        {
            // Arrange
            var options = new DataWrapperOptions();
            var factory = ResponseWrapperFactory.CreateDefault(options);
            var exception = new System.Exception("Test error");
            var context = new ErrorWrapperContext(null, 500, null, exception);

            // Act
            var result = factory.ErrorFactory(context);

            // Assert
            Assert.IsType<ErrorResponse>(result);
            var response = result as ErrorResponse;
            Assert.Equal("9999", response.Code);
            Assert.Equal("Test error", response.Message);
        }

        [Fact]
        public void CustomFactory_CanBeConfigured()
        {
            // Arrange
            var customSuccess = new { code = 200, data = "custom" };
            var customError = new { code = 500, error = "custom error" };
            
            var factory = new ResponseWrapperFactory
            {
                SuccessFactory = ctx => customSuccess,
                ErrorFactory = ctx => customError
            };

            // Act & Assert
            Assert.Same(customSuccess, factory.SuccessFactory(null));
            Assert.Same(customError, factory.ErrorFactory(null));
        }

        [Fact]
        public void GetOrCreateFactory_NoFactory_CreatesDefault()
        {
            // Arrange
            var options = new DataWrapperOptions();

            // Act
            var factory = options.GetOrCreateFactory();

            // Assert
            Assert.NotNull(factory);
            Assert.NotNull(factory.SuccessFactory);
            Assert.NotNull(factory.ErrorFactory);
        }

        [Fact]
        public void GetOrCreateFactory_ExistingFactory_ReturnsExisting()
        {
            // Arrange
            var customFactory = new ResponseWrapperFactory
            {
                SuccessFactory = ctx => "custom",
                ErrorFactory = ctx => "error"
            };
            var options = new DataWrapperOptions
            {
                WrapperFactory = customFactory
            };

            // Act
            var factory = options.GetOrCreateFactory();

            // Assert
            Assert.Same(customFactory, factory);
        }
    }
}
