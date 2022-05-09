namespace MiCake.Core
{
    public sealed class DefaultMiCakeBuilderProvider : IMiCakeBuilderProvider
    {
        private readonly IServiceCollection _services;
        private readonly Type _entryModule;
        private readonly MiCakeApplicationOptions _options;
        private readonly bool _needScope;

        public DefaultMiCakeBuilderProvider(
            IServiceCollection services,
            Type entryModule,
            MiCakeApplicationOptions options,
            bool needScope)
        {
            _services = services;
            _entryModule = entryModule;
            _options = options;
            _needScope = needScope;
        }

        public IMiCakeBuilder GetMiCakeBuilder()
             => new MiCakeBuilder(_services, _entryModule, _options, _needScope);
    }
}
