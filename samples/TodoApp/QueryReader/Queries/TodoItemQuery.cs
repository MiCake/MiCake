using Dapper;
using MiCake.Core.DependencyInjection;
using MiCake.Dapper;
using MiCake.SqlReader;
using Microsoft.Extensions.Options;
using TodoApp.Domain.Aggregates.Todo;
using TodoApp.DtoModels;

namespace TodoApp.QueryReader.Queries
{
    public interface ITodoItemQuery
    {
        Task<PagingQueryResult<TodoItemDto>> PaginationGetTodo(PaginationFilter filter);
    }

    [InjectService(typeof(ITodoItemQuery))]
    public class TodoItemQuery : NgSqlQuery, ITodoItemQuery
    {
        public TodoItemQuery(ISqlReader sqlReader, IOptions<MiCakeDapperOptions> options) : base(sqlReader, options)
        {
        }

        public override string CurrentSectionName => "todo";

        public async Task<PagingQueryResult<TodoItemDto>> PaginationGetTodo(PaginationFilter filter)
        {
            await NgConnection!.OpenAsync();

            var sqlBuilder = new SqlBuilder().Where($"ti.\"State\" = @StateType", new { StateType = TodoItemStateType.Waiting });
            var template = sqlBuilder.AddTemplate(GetSql("Count_TodoWithFilter"));
            var count = await DbConnection.QueryFirstOrDefaultAsync<int>(template.RawSql, template.Parameters);

            // no data.
            if (count == 0)
            {
                return PagingQueryResult<TodoItemDto>.Empty(filter);
            }


            var pagingBuilder = new SqlBuilder().Where($"ti.\"State\" = @StateType", new { StateType = TodoItemStateType.Waiting })
                                                .OrderBy($"ti.\"CreationTime\" desc");
            var pagingTemplate = pagingBuilder.AddTemplate(GetSql("Paging_TodoWithFilter"), new { filter.PageSize, filter.CurrentStartNo });
            var result = await DbConnection.QueryAsync<TodoItemDto>(pagingTemplate.RawSql, pagingTemplate.Parameters);

            return PagingQueryResult<TodoItemDto>.Result(filter, count, result.ToList());
        }
    }
}
