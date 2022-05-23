using AutoMapper;
using TodoApp.Domain.Aggregates.Identity;

namespace TodoApp.DtoModels.AutoMapperConfig
{
    public class MapperConfig : Profile
    {
        public MapperConfig()
        {
            CreateMap<TodoUser, TodoUserDto>();
        }
    }
}
