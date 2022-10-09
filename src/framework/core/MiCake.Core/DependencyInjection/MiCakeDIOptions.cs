namespace MiCake.Core.DependencyInjection
{
    public class MiCakeDIOptions
    {
        /// <summary>
        /// Configuration items for auto injection service
        /// 
        /// When a class that implements an <see cref="ITransientService"/> or <see cref="ISingletonService"/>
        /// or <see cref="IScopedService"/> interface will be injected automatically.
        /// But we need to determine which type of service this class is.
        /// 
        /// defalut: find class all interfaces.The service whose interface name contains the class name.
        /// </summary>
        public FindAutoServiceTypesDelegate? FindAutoServiceTypes { get; set; }
    }
}
