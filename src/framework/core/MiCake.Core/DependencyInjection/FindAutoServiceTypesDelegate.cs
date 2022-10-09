namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// 
    /// <para>
    /// 
    /// When a class that implements an <see cref="ITransientService"/> or <see cref="ISingletonService"/>
    /// or <see cref="IScopedService"/> interface can be injected automatically.
    /// But you need to specify the logic of which interfaces can be registered.
    /// 
    /// </para>
    /// </summary>
    /// <param name="type">the class type</param>
    /// <param name="inheritInterfaces">All interfaces inherited by this class</param>
    /// <returns>service types</returns>
    public delegate List<Type> FindAutoServiceTypesDelegate(Type type, List<Type> inheritInterfaces);


    internal static class DefaultFindServiceTypes
    {
        public static FindAutoServiceTypesDelegate Finder = (type, interfaces) =>
        {
            var result = new List<Type>();

            var typeName = type.Name.ToUpper();
            foreach (var inhertInterface in interfaces)
            {
                var interfaceName = inhertInterface.Name.ToUpper();
                if (interfaceName.StartsWith("I"))
                {
                    if (interfaceName.Contains(typeName))
                        result.Add(inhertInterface);
                }
            }

            return result;
        };
    }
}
