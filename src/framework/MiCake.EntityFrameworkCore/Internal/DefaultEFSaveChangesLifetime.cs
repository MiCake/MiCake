using MiCake.DDD.Extensions.Lifetime;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore.Internal
{
    internal class DefaultEFSaveChangesLifetime : IEFSaveChangesLifetime
    {
        private readonly IEnumerable<IRepositoryPreSaveChanges> _repositoryPreSaveChanges;
        private readonly IEnumerable<IRepositoryPostSaveChanges> _repositoryPostSaveChanges;


        public DefaultEFSaveChangesLifetime(
            IEnumerable<IRepositoryPreSaveChanges> repositoryPreSaveChanges,
            IEnumerable<IRepositoryPostSaveChanges> repositoryPostSaveChanges)
        {
            _repositoryPreSaveChanges = repositoryPreSaveChanges.OrderBy(p => p.Order);
            _repositoryPostSaveChanges = repositoryPostSaveChanges.OrderBy(p => p.Order);
        }

        public void AfterSaveChanges(IEnumerable<EntityEntry> entityEntries)
        {
            foreach (var postSaveChange in _repositoryPostSaveChanges)
            {
                foreach (var entity in entityEntries)
                {
                    var state = entity.State.ToRepositoryState();
                    postSaveChange.PostSaveChanges(state, entity.Entity);
                }
            }
        }

        public async Task AfterSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            foreach (var postSaveChange in _repositoryPostSaveChanges)
            {
                foreach (var entity in entityEntries)
                {
                    var state = entity.State.ToRepositoryState();
                    await postSaveChange.PostSaveChangesAsync(state, entity.Entity, cancellationToken);
                }
            }
        }

        public void BeforeSaveChanges(IEnumerable<EntityEntry> entityEntries)
        {
            foreach (var preSaveChange in _repositoryPreSaveChanges)
            {
                foreach (var entity in entityEntries)
                {
                    var originalEFState = entity.State;

                    var state = entity.State.ToRepositoryState();
                    state = preSaveChange.PreSaveChanges(state, entity.Entity);

                    if (state.ToEFState() != originalEFState) entity.State = state.ToEFState();
                }
            }
        }

        public async Task BeforeSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            foreach (var preSaveChange in _repositoryPreSaveChanges)
            {
                foreach (var entity in entityEntries)
                {
                    var originalEFState = entity.State;

                    var state = entity.State.ToRepositoryState();
                    state = await preSaveChange.PreSaveChangesAsync(state, entity.Entity, cancellationToken);

                    if (state.ToEFState() != originalEFState) entity.State = state.ToEFState();
                }
            }
        }
    }
}
