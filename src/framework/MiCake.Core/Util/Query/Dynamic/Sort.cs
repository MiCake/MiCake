namespace MiCake.Util.Query.Dynamic
{
    public class Sort
    {
        public required string PropertyName { get; set; }
        public bool Ascending { get; set; } = true;
    }
}
