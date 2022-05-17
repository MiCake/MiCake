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
        private readonly IDataWrapperExecutor _dataWrapperExecutor;

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
            DataWrapperOptions options = new();
            DataWrapperContext context = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalData, context);

            Assert.NotNull(result);
            Assert.IsType<ApiResponse>(result);
        }

        [Fact]
        public void WrapperSuccess_OneCustomerModel_InRange()
        {
            var originalData = "dududu";

            CustomWrapperModel customWrapperModel = new();
            customWrapperModel.AddProperty("Name", s => "Name");
            customWrapperModel.AddProperty("Person", s => "Person");
            customWrapperModel.AddProperty("Old", s => "1");

            var costomConfigs = new Dictionary<Range, CustomWrapperModel>();
            costomConfigs.Add(200..300, customWrapperModel);

            DataWrapperOptions options = new()
            {
                UseCustomModel = true,
                CustomModelConfig = costomConfigs
            };
            DataWrapperContext context = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalData, context);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<IResultDataWrapper>(result);
        }

        [Fact]
        public void WrapperSuccess_OneCustomerModel_NotInRange()
        {
            var originalData = "dududu";

            CustomWrapperModel customWrapperModel = new();
            customWrapperModel.AddProperty("Name", s => "Name");
            customWrapperModel.AddProperty("Person", s => "Person");
            customWrapperModel.AddProperty("Old", s => "1");

            var costomConfigs = new Dictionary<Range, CustomWrapperModel>();
            costomConfigs.Add(300..300, customWrapperModel);

            DataWrapperOptions options = new()
            {
                UseCustomModel = true,
                CustomModelConfig = costomConfigs
            };
            DataWrapperContext context = new(new OkObjectResult(originalData), CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalData, context);

            Assert.Same(originalData, result);
        }

        [Fact]
        public void WrapperSuccess_MoreCustomerModel()
        {
            var originalData = "dududu";

            CustomWrapperModel customWrapperModel = new();
            customWrapperModel.AddProperty("Name", s => "Name");
            customWrapperModel.AddProperty("Person", s => "Person");
            customWrapperModel.AddProperty("Old", s => "1");

            CustomWrapperModel customWrapperModel2 = new();
            customWrapperModel2.AddProperty("DiDiDi", s => "DiDiDi");

            var costomConfigs = new Dictionary<Range, CustomWrapperModel>();
            costomConfigs.Add(200..201, customWrapperModel);
            costomConfigs.Add(300..401, customWrapperModel2);

            DataWrapperOptions options = new()
            {
                UseCustomModel = true,
                CustomModelConfig = costomConfigs
            };
            DataWrapperContext context = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalData, context);

            Assert.NotNull(result.GetType().GetProperty("Name").Name);

            DataWrapperContext context2 = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 300), options);
            var result2 = _dataWrapperExecutor.WrapApiResponse(originalData, context2);

            Assert.NotNull(result2.GetType().GetProperty("DiDiDi").Name);
        }

        [Fact]
        public void WrapperSuccess_MoreCustomerModel_OverlappingStatuCodes()
        {
            var originalData = "dududu";

            CustomWrapperModel customWrapperModel = new();
            customWrapperModel.AddProperty("Name", s => "Name");
            customWrapperModel.AddProperty("Person", s => "Person");
            customWrapperModel.AddProperty("Old", s => "1");

            CustomWrapperModel customWrapperModel2 = new();
            customWrapperModel2.AddProperty("DiDiDi", s => "DiDiDi");

            var costomConfigs = new Dictionary<Range, CustomWrapperModel>();
            costomConfigs.Add(200..301, customWrapperModel);
            costomConfigs.Add(200..401, customWrapperModel2);

            DataWrapperOptions options = new()
            {
                UseCustomModel = true,
                CustomModelConfig = costomConfigs
            };
            DataWrapperContext context = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalData, context);

            Assert.NotNull(result.GetType().GetProperty("Name").Name);

            DataWrapperContext context2 = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 300), options);
            var result2 = _dataWrapperExecutor.WrapApiResponse(originalData, context2);

            Assert.NotNull(result2.GetType().GetProperty("Name").Name);
        }

        [Fact]
        public void WrapperSuccess_CustomerModel_NullConfig()
        {
            var originalData = "dududu";

            DataWrapperOptions options = new()
            {
                UseCustomModel = true,
                CustomModelConfig = null
            };
            DataWrapperContext context = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalData, context);

            Assert.Same(originalData, result);
        }

        [Fact]
        public void WrapperSuccess_OriginalIsCustomerWrapperData()
        {
            var originalData = new TestResultDataWrapper() { CompanyName = "MiCake" };

            DataWrapperOptions options = new();
            DataWrapperContext context = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 200), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalData, context);

            Assert.NotNull(result);
            Assert.Same(originalData, result);
        }

        [Fact]
        public void WrapperSuccess_NullOptions()
        {
            var originalData = new ObjectResult(123);

            DataWrapperContext optionsNullContext = new(new ObjectResult(originalData), CreateFakeHttpContext("Get", 200), null);
            Assert.Throws<ArgumentNullException>(() => _dataWrapperExecutor.WrapApiResponse(originalData, optionsNullContext));

            DataWrapperContext httpContextNullContext = new(new ObjectResult(originalData), null, new DataWrapperOptions());
            Assert.Throws<ArgumentNullException>(() => _dataWrapperExecutor.WrapApiResponse(originalData, httpContextNullContext));
        }

        [Fact]
        public void WrapperSuccess_OriginalIsProblemDetail_WrapProblemDetailsIsFalse()
        {
            DataWrapperOptions options = new();

            var originalProblemDetail = new BadRequestObjectResult(new ProblemDetails() { Title = "This is ProblemDetail" });
            DataWrapperContext context = new(originalProblemDetail, CreateFakeHttpContext("Get", 400), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalProblemDetail.Value, context);

            Assert.NotNull(result);
            Assert.Same(originalProblemDetail.Value, result);
        }

        [Fact]
        public void WrapperSuccess_OriginalIsProblemDetail_WrapProblemDetailsIsTrue()
        {
            DataWrapperOptions options = new()
            {
                WrapProblemDetails = true
            };

            var originalProblemDetail = new BadRequestObjectResult(new ProblemDetails() { Title = "This is ProblemDetail" });
            DataWrapperContext context = new(originalProblemDetail, CreateFakeHttpContext("Get", 400), options);
            var result = _dataWrapperExecutor.WrapApiResponse(originalProblemDetail.Value, context);

            Assert.NotNull(result);
            Assert.IsAssignableFrom<ApiResponse>(result);
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
