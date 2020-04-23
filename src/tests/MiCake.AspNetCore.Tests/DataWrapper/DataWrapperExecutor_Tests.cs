using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Xunit;

namespace MiCake.AspNetCore.Tests.DataWrapper
{
    public class DataWrapperExecutor_Tests
    {
        private IDataWrapperExecutor _dataWrapperExecutor;

        public DataWrapperExecutor_Tests()
        {
            _dataWrapperExecutor = new DefaultWrapperExecutor();
        }

        [Theory]
        [InlineData("1111")]
        [InlineData(null)]
        [InlineData(123)]
        public void WrapperSuccess_NormalData_ShouldReturnApiResponse(object originalData)
        {
            DataWrapperOptions options = new DataWrapperOptions();
            DataWrapperContext context = new DataWrapperContext(originalData, CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapSuccesfullysResult(originalData, context);

            Assert.NotNull(result);
            Assert.IsType<ApiResponse>(result);
        }

        [Fact]
        public void WrapperSuccess_CustomerModel()
        {
            var originalData = "dududu";
            var customerProperties = new Dictionary<string, ConfigWrapperPropertyDelegate>();
            customerProperties.Add("Name", s => "Name");
            customerProperties.Add("Person", s => "Person");
            customerProperties.Add("Old", s => "1");

            DataWrapperOptions options = new DataWrapperOptions()
            {
                UseCustomModel = true,
                CustomerProperty = customerProperties
            };
            DataWrapperContext context = new DataWrapperContext(originalData, CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapSuccesfullysResult(originalData, context);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IResultDataWrapper>(result);
        }


        private HttpContext CreateFakeHttpContext(string method, int statusCode)
        {
            var fakeHttpContext = new DefaultHttpContext();
            fakeHttpContext.Request.Method = method;
            fakeHttpContext.Response.StatusCode = statusCode;

            return fakeHttpContext;
        }
    }
}
