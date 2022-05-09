namespace MiCake.Core.Modularity
{
    /// <summary>
    /// Indicate the module is a micake core module.
    /// 
    /// <para>
    ///    Be careful:This attitude is only use in internal development.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CoreModuleAttribute : Attribute
    {

    }
}
