using MiCake.Core.Data;

namespace MiCake.Core
{
    public static class MiCakeCoreExtensions
    {
        public static IMiCakeApplication SlotModule<T>(this IMiCakeApplication application) where T : IMiCakeModule
        {
            return SlotModule(application, typeof(T));
        }

        public static IMiCakeApplication SlotModule(this IMiCakeApplication application, Type moduleType)
        {
            if (application.ModuleManager is IModuleSlot slotModuleMg)
            {
                slotModuleMg.Slot(moduleType);
            }
            else
            {
                throw new InvalidCastException($"Current MiCake module manager is not a {nameof(IModuleSlot)} instance.");
            }

            return application;
        }

        public static IMiCakeApplication AddStartupTransientData(this IMiCakeApplication application, string key, object data)
        {
            var appTransientData = (application as IHasAccessor<MiCakeTransientData>)?.AccessibleData ??
                throw new InvalidOperationException($"Cannot add transient data for application,current application don't has any {nameof(MiCakeTransientData)} property.");

            appTransientData.Deposit(key, data);

            return application;
        }
    }
}
