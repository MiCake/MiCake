using AutoMapper;
using MiCake.Core.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Controllers
{
    public abstract class TodoControllerBase : ControllerBase
    {
        public IMapper Mapper => _infrastructure.Mapper;

        private readonly ControllerInfrastructure _infrastructure;

        public TodoControllerBase(ControllerInfrastructure infrastructure)
        {
            _infrastructure = infrastructure;
        }
    }

    [InjectService()]
    public class ControllerInfrastructure
    {
        public IMapper Mapper { get; set; }

        public ControllerInfrastructure(IMapper mapper)
        {
            Mapper = mapper;
        }
    }
}
