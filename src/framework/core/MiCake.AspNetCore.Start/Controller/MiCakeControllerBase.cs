using MiCake.AspNetCore.DataWrapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MiCake.AspNetCore.Controller
{
    public abstract class MiCakeControllerBase : ControllerBase
    {
        protected AcceptedResult Accepted(string code, string message, object? data = null)
        {
            return base.Accepted(GetApiResultByCode(code, message, false, data));
        }

        protected AcceptedResult Accepted(string moduleCode, string featureCode, string message, object? data = null)
        {
            return base.Accepted(GetApiResultByModule(moduleCode, featureCode, message, false, data));
        }

        protected BadRequestObjectResult BadRequest(string code, string message, object? data = null)
        {
            return base.BadRequest(GetApiResultByCode(code, message, true, data));
        }

        protected BadRequestObjectResult BadRequest(string moduleCode, string featureCode, string message, object? data = null)
        {
            return base.BadRequest(GetApiResultByModule(moduleCode, featureCode, message, true, data));
        }

        protected NotFoundObjectResult NotFound(string code, string message, object? data = null)
        {
            return base.NotFound(GetApiResultByCode(code, message, true, data));
        }

        protected NotFoundObjectResult NotFound(string moduleCode, string featureCode, string message, object? data = null)
        {
            return base.NotFound(GetApiResultByModule(moduleCode, featureCode, message, true, data));
        }

        protected OkObjectResult Ok(string code, string message, object data)
        {
            return base.Ok(GetApiResultByCode(code, message, false, data));
        }

        protected OkObjectResult Ok(string moduleCode, string featureCode, string message, object data)
        {
            return base.Ok(GetApiResultByModule(moduleCode, featureCode, message, false, data));
        }

        public override OkObjectResult Ok([ActionResultObjectValue] object? data)
        {
            var okResponse = new ApiResponse<object>()
            {
                Result = data
            };

            return base.Ok(okResponse);
        }

        protected UnauthorizedObjectResult Unauthorized(string code, string message, object? data = null)
        {
            return base.Unauthorized(GetApiResultByCode(code, message, true, data));
        }

        protected UnauthorizedObjectResult Unauthorized(string moduleCode, string featureCode, string message, object? data = null)
        {
            return base.Unauthorized(GetApiResultByModule(moduleCode, featureCode, message, true, data));
        }

        protected virtual string GetResponseCode(string moduleCode, string featureCode)
        {
            return string.Format("{0}.{1}", moduleCode, featureCode);
        }

        private ApiResponse<object> GetApiResultByCode(string code, string message, bool isError, object? data = null)
        {
            return new ApiResponse<object>()
            {
                Code = code,
                Message = message,
                Result = data,
                HasError = isError,
            };
        }

        private ApiResponse<object> GetApiResultByModule(string moduleCode, string featureCode, string message, bool isError, object? data = null)
        {
            return new ApiResponse<object>()
            {
                Code = GetResponseCode(moduleCode, featureCode),
                Message = message,
                Result = data,
                HasError = isError,
            };
        }
    }
}
