using System;
using BaseMiCakeApplication.Domain.Aggregates;
using BaseMiCakeApplication.Domain.Repositories;
using MiCake.EntityFrameworkCore.Repository;

namespace BaseMiCakeApplication.EFCore.Repositories;

public class UserRepository : EFRepository<BaseAppDbContext, User, long>, IUserRepository
{
    public UserRepository(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}
