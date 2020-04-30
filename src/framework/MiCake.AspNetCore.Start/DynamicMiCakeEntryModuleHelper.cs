using MiCake.AspNetCore.Modules;
using MiCake.Core.Modularity;
using MiCake.Core.Util.Reflection.Emit;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MiCake.AspNetCore.Start
{
    internal static class DynamicMiCakeEntryModuleHelper
    {
        public static Type CreateDynamicEntryModule()
        {
            //Get entry assembly.
            var entryAssembly = Assembly.GetEntryAssembly();

            var entryModuletType = EmitHelper.CreateClass("DynamicMiCakeEntryModule",
                                      entryAssembly.GetName().FullName,
                                      "DynamicMiCakeEntryModule",
                                      TypeAttributes.Public,
                                      typeof(MiCakeModule));

            Type[] ctorParams = new Type[] { typeof(Type[]) };
            ConstructorInfo classConstructor = typeof(RelyOnAttribute).GetConstructor(ctorParams);
            CustomAttributeBuilder aspNetCoreModuleAttribute = new CustomAttributeBuilder(
                        classConstructor,
                        new object[] { new Type[] { typeof(MiCakeAspNetCoreModule) } });

            entryModuletType.SetCustomAttribute(aspNetCoreModuleAttribute);

            return entryModuletType.CreateType();
        }
    }
}
