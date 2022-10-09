using AutoMapper;
using TodoApp.Domain.Aggregates.Identity;
using TodoApp.Domain.Aggregates.Todo;

namespace TodoApp.DtoModels.AutoMapperConfig
{
    public class MapperConfig : Profile
    {
        public MapperConfig()
        {
            CreateMap<TodoUser, TodoUserDto>();

            CreateMap<TodoItem, TodoItemDto>();
        }
    }
}
