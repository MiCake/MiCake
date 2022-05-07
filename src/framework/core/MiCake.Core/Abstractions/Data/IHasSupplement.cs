namespace MiCake.Core.Data
{
    /// <summary>
    /// Indicate the class need receive additional data. 
    /// </summary>
    public interface IHasSupplement<in TSupplementData>
    {
        void SetData(TSupplementData parts);
    }
}
