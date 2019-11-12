using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiCake.Core.Abstractions.ExceptionHandling;
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
        private InjectDemoClassA demoClassA;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IServiceProvider serviceProvider,InjectDemoClassA demoClassA)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();

            var instance = _serviceProvider.GetService(typeof(InjectDemoClassA));

            var a = _serviceProvider.GetService(typeof(Microsoft.AspNetCore.Mvc.Routing.UrlHelperFactory));
            _logger.LogInformation("lalalala");
            Log.Information("asdfaf");

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
