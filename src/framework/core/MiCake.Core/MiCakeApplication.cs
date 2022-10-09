using MiCake.Core.Data;
using MiCake.Core.Time;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiCake.Core
{
    public class MiCakeApplication : IMiCakeApplication, IHasAccessor<MiCakeTransientData>, IHasSupplement<IServiceProvider>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public MiCakeApplicationOptions ApplicationOptions { get; private set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IMiCakeModuleManager ModuleManager { get; private set; } = new MiCakeModuleManager();

        private readonly IServiceCollection _services;
        private IServiceScope? _appServiceScope;

        private IServiceProvider? _appServiceProvider;
        public IServiceProvider? AppServiceProvider
        {
            get
            {
                if (_appServiceProvider == null)
                {
                    if (_needNewScope)
                        _appServiceScope = _serviceProvider?.CreateScope();

                    _appServiceProvider = _appServiceScope == null ? _serviceProvider : _appServiceScope.ServiceProvider;
                }
                return _appServiceProvider;
            }
        }

        public MiCakeTransientData AccessibleData { get; } = new();

        private Type? _entryType;
        private IMiCakeModuleBoot? _miCakeModuleBoot;
        private IServiceProvider? _serviceProvider;
        private readonly bool _needNewScope;
        private IMiCakeModuleContext? ModuleContext => ModuleManager.ModuleContext;

        private bool _isInitialized = false;
        private bool _isStarted = false;
        private bool _isShutdown = false;

        public MiCakeApplication(
            IServiceCollection services,
            MiCakeApplicationOptions options,
            bool needNewScope)
        {
            ApplicationOptions = options;
            _services = services;
            _needNewScope = needNewScope;
        }

        /// <summary>
        /// Start micake application.
        /// Must provider <see cref="IServiceProvider"/>
        /// </summary>
        public virtual void Start()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Please call MiCakeApplication.Initialize() before MiCakeApplication.Start()");

            if (AppServiceProvider == null)
                throw new ArgumentNullException(nameof(AppServiceProvider));

            if (_isStarted)
                throw new InvalidOperationException($"MiCake has already started.");

            var context = new ModuleLoadContext(AppServiceProvider, ModuleContext!.MiCakeModules, this);
            _miCakeModuleBoot!.Initialization(context);

            //Release options additional infomation.
            AccessibleData.Release();

            _isStarted = true;
        }

        /// <summary>
        /// Shudown micake application
        /// </summary>
        public virtual void ShutDown()
        {
            if (!_isStarted)
                throw new InvalidOperationException($"MiCake hasn't started yet.");

            if (_isShutdown)
                throw new InvalidOperationException($"MiCake has already shutdown.");

            var context = new ModuleLoadContext(AppServiceProvider!, ModuleContext!.MiCakeModules, this);
            _miCakeModuleBoot!.ShutDown(context);

            _appServiceScope?.Dispose();

            _isShutdown = true;
        }

        /// <summary>
        /// Initialize micake services
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException($"MiCake has already build common services. The {nameof(Initialize)} method only can called once!");

            if (_entryType == null)
                throw new NullReferenceException($"Cannot find entry module type,Please make sure you has already call {nameof(SetEntry)} method.");

            AddMiCakeCoreSerivces(_services);

            //Find all micake modules according to the entry module type
            ModuleManager.PopulateModules(_entryType, ApplicationOptions.ModuleSorter);
            if (ModuleContext == null)
            {
                throw new InvalidOperationException($"An error was encountered in {nameof(ModuleManager)} populate phase.");
            }

            var loggerFactory = _services.BuildServiceProvider().GetRequiredService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

            _miCakeModuleBoot = new MiCakeModuleBoot(loggerFactory, ModuleContext.MiCakeModules);

            var configServiceContext = new ModuleConfigServiceContext(_services, ModuleContext.MiCakeModules, this);
            _miCakeModuleBoot.ConfigServices(configServiceContext);

            _isInitialized = true;
        }

        /// <summary>
        /// Set entry module type.
        /// </summary>
        public void SetEntry(Type type)
        {
            _entryType = type ?? throw new ArgumentException("Please add startup type when you use AddMiCake().");
        }

        /// <summary>
        /// Set <see cref="IServiceProvider"/>.
        /// </summary>
        void IHasSupplement<IServiceProvider>.SetData(IServiceProvider parts)
        {
            _serviceProvider = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        private void AddMiCakeCoreSerivces(IServiceCollection services)
        {
            services.AddSingleton<IMiCakeApplication>(this);

            services.TryAddSingleton<IAppClock, AppClock>();
        }

        public virtual void Dispose()
        {
            if (!_isShutdown)
                ShutDown();
        }
    }
}
