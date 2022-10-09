using System.Reflection;
using System.Reflection.Emit;

namespace MiCake.Core.Util.Reflection
{
    public static class EmitHelper
    {
        /// <summary>
        /// The default micake dynamic assembly name.
        /// </summary>
        public const string MiCakeDynamicAssemblyName = "MiCakeDynamicAssembly";

        /// <summary>
        /// The default micake dynamic module name.
        /// </summary>
        public const string MiCakeDynamicModuleName = "MiCakeDynamicModule";

        /// <summary>
        /// Creating a basic class(has no properties or fields or methods).
        /// </summary>
        /// <param name="className">Created class name</param>
        /// <param name="assemblyName"></param>
        /// <param name="moduleName"></param>
        /// <param name="typeAttributes"></param>
        /// <param name="baseType"></param>
        /// <returns><see cref="TypeBuilder"/></returns>
        public static TypeBuilder CreateClass(string className,
                                       string assemblyName = "",
                                       string moduleName = "",
                                       TypeAttributes typeAttributes = TypeAttributes.Public,
                                       Type? baseType = null)
        {
            CheckValue.NotNullOrEmpty(className, nameof(className));

            var asmNameStr = string.IsNullOrEmpty(assemblyName) ? MiCakeDynamicAssemblyName : assemblyName;
            var moduleNameStr = string.IsNullOrEmpty(moduleName) ? MiCakeDynamicModuleName : moduleName;

            var asmName = new AssemblyName(asmNameStr);
            var builder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var moduleBuilder = builder.DefineDynamicModule(moduleNameStr);

            return moduleBuilder.DefineType(className, typeAttributes, baseType); ;
        }

        /// <summary>
        /// Create property in the current <see cref="TypeBuilder"/>
        /// </summary>
        /// <param name="typeBuilder"><see cref="TypeBuilder"/></param>
        /// <param name="propertyName">The name of the property to be created</param>
        /// <param name="propertyType">The type of the property to be created</param>
        /// <returns>orignal <see cref="TypeBuilder"/></returns>
        public static TypeBuilder CreateProperty(
            TypeBuilder typeBuilder,
            string propertyName,
            Type propertyType)
        {
            CheckValue.NotNull(typeBuilder, nameof(typeBuilder));
            CheckValue.NotNullOrEmpty(propertyName, nameof(propertyName));
            CheckValue.NotNull(propertyType, nameof(propertyType));

            //filed info 
            var fieldInfo = typeBuilder.DefineField($"_{propertyName.ToLower()}", propertyType, FieldAttributes.Private);

            //method info 
            var getMethod = typeBuilder.DefineMethod($"get_{propertyName}", MethodAttributes.Public, propertyType, null);
            var setMethod = typeBuilder.DefineMethod($"set_{propertyName}", MethodAttributes.Public, null, new Type[] { propertyType });

            var ilOfGet = getMethod.GetILGenerator();
            ilOfGet.LdArg(0); //arguement : this 
            ilOfGet.Emit(OpCodes.Ldfld, fieldInfo);
            ilOfGet.Return();

            var ilOfSet = setMethod.GetILGenerator();
            ilOfSet.PushParameters(true, 2); //arguements : 1:this 2:value
            ilOfSet.Emit(OpCodes.Stfld, fieldInfo);
            ilOfSet.Return();

            //property
            var propertyBuilde = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType, null);
            propertyBuilde.SetGetMethod(getMethod);
            propertyBuilde.SetSetMethod(setMethod);

            return typeBuilder;
        }
    }
}
