using Dapper;
using MiCake.Core.DependencyInjection;
using MiCake.Dapper;
using MiCake.SqlReader;
using Microsoft.Extensions.Options;
using TodoApp.DtoModels;

namespace TodoApp.QueryReader.Queries
{
    public interface IConcernedTodoQuery
    {
        Task<PagingQueryResult<TodoItemDto>> PaginationGetPersonConcernedTodo(PaginationFilter filter, int userId);
    }

    [InjectService(typeof(IConcernedTodoQuery))]
    public class ConcernedTodoQuery : NgSqlQuery, IConcernedTodoQuery
    {
        public ConcernedTodoQuery(ISqlReader sqlReader, IOptions<MiCakeDapperOptions> options) : base(sqlReader, options)
        {
        }

        public override string CurrentSectionName => "concerned-todo";

        public async Task<PagingQueryResult<TodoItemDto>> PaginationGetPersonConcernedTodo(PaginationFilter filter, int userId)
        {
            await NgConnection!.OpenAsync();

            var sqlBuilder = new SqlBuilder().Where($"ct.\"UserId\" = @UserId", new { UserId = userId });
            var template = sqlBuilder.AddTemplate(GetSql(Sql_Count_ConcernedTodoWithFilter));
            var count = await DbConnection.QueryFirstOrDefaultAsync<int>(template.RawSql, template.Parameters);

            // no data.
            if (count == 0)
            {
                return PagingQueryResult<TodoItemDto>.Empty(filter);
            }

            var pagingBuilder = new SqlBuilder().Where($"ct.\"UserId\" = @UserId", new { UserId = userId });
            var pagingTemplate = pagingBuilder.AddTemplate(GetSql(Sql_Paging_ConcernedTodoWithFilter), new { filter.PageSize, filter.CurrentStartNo });
            var result = await DbConnection.QueryAsync<TodoItemDto>(pagingTemplate.RawSql, pagingTemplate.Parameters);

            return PagingQueryResult<TodoItemDto>.Result(filter, count, result.ToList());
        }

        private const string Sql_Count_ConcernedTodoWithFilter = "Count_ConcernedTodoWithFilter";

        private const string Sql_Paging_ConcernedTodoWithFilter = "Paging_ConcernedTodoWithFilter";
    }
}
