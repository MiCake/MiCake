namespace MiCake.Core.Data
{
    /// <summary>
    /// Indicate a instance can receive data and change self value from external.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public interface ICanApplyData<TData>
    {
        void Apply(TData data);
    }
}
