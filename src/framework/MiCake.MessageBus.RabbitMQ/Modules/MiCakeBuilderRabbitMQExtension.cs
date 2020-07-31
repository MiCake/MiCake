using MiCake.Core;
using System;

namespace MiCake.MessageBus.RabbitMQ.Modules
{
    public static class MiCakeBuilderRabbitMQExtension
    {
        /// <summary>
        /// Add MiCake RabbitMQ services.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IMiCakeBuilder UseRabbitMQ(this IMiCakeBuilder builder)
        {
            return UseRabbitMQ(builder, null);
        }

        /// <summary>
        /// Add MiCake RabbitMQ services.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsBuilder"></param>
        /// <returns></returns>
        public static IMiCakeBuilder UseRabbitMQ(this IMiCakeBuilder builder, Action<RabbitMQOptions> optionsBuilder)
        {
            builder.ConfigureApplication((app, services) =>
            {
                //register ef module to micake module collection
                app.ModuleManager.AddMiCakeModule(typeof(MiCakeRabbitMQModule));

                services.AddRabbitMQ(optionsBuilder);
            });

            return builder;
        }
    }
}
