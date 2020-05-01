using MiCake.AspNetCore.DataWrapper;
using MiCake.AspNetCore.DataWrapper.Internals;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
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

        [Fact]
        public void WrapperSuccess_OrignalIsCustomerWrapperData()
        {
            var originalData = new TestResultDataWrapper() { CompanyName = "MiCake" };

            DataWrapperOptions options = new DataWrapperOptions();
            DataWrapperContext context = new DataWrapperContext(originalData, CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapSuccesfullysResult(originalData, context);

            Assert.NotNull(result);
            Assert.Same(originalData, result);
        }

        [Fact]
        public void WrapperExecutor_CacheUserModel()
        {
            var originalData = new ObjectResult(123);
            var dataWrapperExecutor = new DefaultWrapperExecutor();
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

            dataWrapperExecutor.WrapSuccesfullysResult(originalData, context);

            //Other model. this config will not be exceute. beacuse WrapperExecutor already has a cache type.
            var customerProperties2 = new Dictionary<string, ConfigWrapperPropertyDelegate>();
            customerProperties.Add("Name2", s => "Name");
            customerProperties.Add("Person2", s => "Person");
            customerProperties.Add("Old2", s => "1");

            DataWrapperOptions options2 = new DataWrapperOptions()
            {
                UseCustomModel = true,
                CustomerProperty = customerProperties2
            };
            DataWrapperContext context2 = new DataWrapperContext(originalData, CreateFakeHttpContext("Get", 200), options2);
            var resultInfo = dataWrapperExecutor.WrapSuccesfullysResult(originalData, context);

            var nameProp = resultInfo.GetType().GetProperty("Name");
            var name2Prop = resultInfo.GetType().GetProperty("Name2");

            Assert.NotNull(nameProp);
            Assert.Null(name2Prop);
        }

        [Fact]
        public void WrapperSuccess_NullOptions()
        {
            var originalData = new ObjectResult(123);

            DataWrapperContext optionsNullContext = new DataWrapperContext(originalData, CreateFakeHttpContext("Get", 200), null);
            Assert.Throws<ArgumentNullException>(() => _dataWrapperExecutor.WrapSuccesfullysResult(originalData, optionsNullContext));

            DataWrapperContext httpContextNullContext = new DataWrapperContext(originalData, null, new DataWrapperOptions());
            Assert.Throws<ArgumentNullException>(() => _dataWrapperExecutor.WrapSuccesfullysResult(originalData, httpContextNullContext));
        }

        private HttpContext CreateFakeHttpContext(string method, int statusCode)
        {
            var fakeHttpContext = new DefaultHttpContext();
            fakeHttpContext.Request.Method = method;
            fakeHttpContext.Response.StatusCode = statusCode;

            return fakeHttpContext;
        }


        public class TestResultDataWrapper : IResultDataWrapper
        {
            public string CompanyName { get; set; }
        }
    }
}
