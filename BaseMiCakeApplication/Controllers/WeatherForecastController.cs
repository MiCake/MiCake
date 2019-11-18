using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiCake.Core.Abstractions;
using MiCake.Core.Abstractions.DependencyInjection;
using MiCake.Core.Abstractions.ExceptionHandling;
using MiCake.Core.Abstractions.Logging;
using MiCake.Core.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Serilog.ILogger;

namespace BaseMiCakeApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private IServiceProvider _serviceProvider;
        private IClassA demoClassA;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            demoClassA = (IClassA)serviceProvider.GetRequiredService(typeof(IClassA));
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();

            var demoA = ServiceLocator.Instance.Locator.GetService<IClassA>();
            var handler = ServiceLocator.Instance.GetSerivce<ILogErrorHandlerProvider>();

            _logger.LogInformation("lalalala");
            Log.Information("asdfaf");
            Log.Information(demoClassA.StrWrite());

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();

        }
    }
}
