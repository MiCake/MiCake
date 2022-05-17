namespace MiCake.Cord.LinqFilter.Extensions
{
    public static class PagingExtensions
    {
        public static IQueryable<T> Page<T>(this IQueryable<T> query, int page, int pageSize)
        {
            var pageIndex = page - 1;
            if (pageIndex < 0)
            {
                pageIndex = 0;
            }

            return query.Skip(pageIndex * pageSize).Take(pageSize);
        }
    }
}
