using MiCake.AspNetCore;
using MiCake.AspNetCore.DataWrapper;
using Microsoft.AspNetCore.Mvc;

namespace BaseMiCakeApplication.MiCakeFeatures
{
    public static class DataWrappedFeature
    {
        public static MiCakeAspNetOptions UseCustomModel(this MiCakeAspNetOptions options)
        {
            //要使用自定义模型格式，必须启动该配置
            options.DataWrapperOptions.UseCustomModel = true;

            //表示当返回code在200-300之间时，将使用该自定义模型
            options.DataWrapperOptions.CustomModelConfig.Add(200..300, CreateCustomModel());

            return options;
        }

        private static CustomWrapperModel CreateCustomModel()
        {
            CustomWrapperModel result = new CustomWrapperModel("MiCakeCustomModel");

            result.AddProperty("company", s => "MiCake");
            result.AddProperty("statusCode", s => (s.ResultData as ObjectResult).StatusCode ?? s.HttpContext.Response.StatusCode);
            result.AddProperty("result", s => (s.ResultData as ObjectResult).Value);

            return result;
        }
    }
}
