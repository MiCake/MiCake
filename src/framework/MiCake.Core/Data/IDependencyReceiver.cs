namespace MiCake.Core.Data
{
    /// <summary>
    /// define a class need an necessary parts
    /// </summary>
    public interface IDependencyReceiver<in TDependency>
    {
        void AddDependency(TDependency parts);
    }
}
