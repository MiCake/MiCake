using AutoMapper;
using MiCake.Core.DependencyInjection;
using MiCake.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Controllers
{
    public abstract class TodoControllerBase : ControllerBase
    {
        public IMapper Mapper => _infrastructure.Mapper;

        public int? CurrentUserId => _infrastructure.CurrentUserId;

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

        public int? CurrentUserId => currentUser.UserId as int?;

        private readonly ICurrentMiCakeUser currentUser;

        public ControllerInfrastructure(IMapper mapper, ICurrentMiCakeUser user)
        {
            Mapper = mapper;
            currentUser = user;
        }
    }
}
