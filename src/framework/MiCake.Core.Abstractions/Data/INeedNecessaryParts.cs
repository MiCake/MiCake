namespace MiCake.Core.Data
{
    /// <summary>
    /// define a class need an necessary parts
    /// </summary>
    public interface INeedNecessaryParts<in TParts>
    {
        void SetNecessaryParts(TParts parts);
    }
}
