namespace MiCake.Core.DependencyInjection
{
    /// <summary>
    /// Tag current <see cref="MiCakeModule"/> need auto register services to DI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoDIAttribute : Attribute
    {
    }

    /// <summary>
    /// Disable current <see cref="MiCakeModule"/> auto register services to DI.
    /// 
    /// <para>
    ///     when you use this attribute, even if you have <see cref="AutoDIAttribute"/> tag, it also will be covered.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DisableAutoDIAttribute : Attribute { }
}
