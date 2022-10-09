namespace MiCake.Core.Data
{
    /// <summary>
    /// <para>
    ///     This interface declare class has accessor data.
    /// </para>
    /// <para>
    ///     You can use <see cref="AccessibleData"/> to get accessor data.
    /// </para>
    /// </summary>
    public interface IHasAccessor<TDataType>
    {
        TDataType AccessibleData { get; }
    }
}
