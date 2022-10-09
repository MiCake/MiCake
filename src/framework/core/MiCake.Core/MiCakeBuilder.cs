namespace MiCake.Core
{
    /// <summary>
    /// A build for <see cref="IMiCakeApplication"/>
    /// </summary>
    public class MiCakeBuilder : IMiCakeBuilder
    {
        private readonly MiCakeApplicationOptions _options;
        private readonly Type _entryType;
        private readonly IServiceCollection _services;
        private readonly IMiCakeApplication _app;

        private Action<IMiCakeApplication, IServiceCollection>? _configureAction;

        public MiCakeBuilder(IServiceCollection services,
                             Type entryType,
                             MiCakeApplicationOptions options,
                             bool needNewScope = false)
        {
            _services = services;
            _entryType = entryType;
            _options = options ?? new MiCakeApplicationOptions();

            _app = new MiCakeApplication(_services, _options, needNewScope);
            _app.SetEntry(_entryType);
        }

        public IMiCakeApplication Build()
        {
            _configureAction?.Invoke(_app, _services);
            _app.Initialize();

            return _app;
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication> configureApp)
        {
            return ConfigureApplication((app, s) => configureApp(app));
        }

        public IMiCakeBuilder ConfigureApplication(Action<IMiCakeApplication, IServiceCollection> configureApp)
        {
            _configureAction += configureApp;
            return this;
        }
    }
}
