namespace MiCake.Core.Data
{
    /// <summary>
    /// define a class need an necessary parts
    /// </summary>
    public interface INeedParts<in TParts>
    {
        void SetParts(TParts parts);
    }
}
