using MiCake.Cord.Lifetime;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

        public async Task AfterSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            var cloneEntries = entityEntries.ToList();
            foreach (var postSaveChange in _repositoryPostSaveChanges)
            {
                foreach (var entity in cloneEntries)
                {
                    var state = entity.State.ToRepositoryState();
                    await postSaveChange.PostSaveChangesAsync(state, entity.Entity, cancellationToken);
                }
            }
        }

        public async Task BeforeSaveChangesAsync(IEnumerable<EntityEntry> entityEntries, CancellationToken cancellationToken = default)
        {
            var cloneEntries = entityEntries.ToList();
            foreach (var preSaveChange in _repositoryPreSaveChanges)
            {
                foreach (var entity in cloneEntries)
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
