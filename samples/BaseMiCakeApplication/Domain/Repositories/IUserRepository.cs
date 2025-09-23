using BaseMiCakeApplication.Domain.Aggregates;
using MiCake.DDD.Domain;

namespace BaseMiCakeApplication.Domain.Repositories;

public interface IUserRepository : IRepository<User, long>
{

}
