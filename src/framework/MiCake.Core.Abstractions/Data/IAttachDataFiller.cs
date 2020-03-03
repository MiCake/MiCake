namespace MiCake.Core.Data
{
    /// <summary>
    /// Defined a attach data filler
    /// </summary>
    public interface IAttachDataFiller
    {
        /// <summary>
        /// Used to fill attach data
        /// </summary>
        /// <param name="data">Data to be filled in</param>
        /// <param name="source">the data source</param>
        void FillData(IAttachData data, object source);
    }
}
