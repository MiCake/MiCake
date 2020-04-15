﻿using MiCake.Core.DependencyInjection;
using MiCake.DDD.Extensions.LifeTime;
using MiCake.DDD.Extensions.Store.Configure;
using MiCake.EntityFrameworkCore.Interprets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiCake.EntityFrameworkCore
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public class MiCakeDbContext : DbContext
    {
        public MiCakeDbContext(DbContextOptions options) : base(options)
        {
        }

        protected MiCakeDbContext()
        {
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="modelBuilder"><see cref="ModelBuilder"/></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //EFCore will cached model info.This method only called once
            var storeModelExpression = new EFModelExpressionProvider().GetExpression();
            storeModelExpression.Interpret(StoreConfig.Instance.GetStoreModel(), modelBuilder);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var entities = ChangeTracker.Entries();

            BeforeSaveChanges(entities);
            var result = base.SaveChanges(acceptAllChangesOnSuccess);
            AfterSaveChanges(entities);

            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var entities = ChangeTracker.Entries();

            await BeforeSaveChangesAsync(entities, cancellationToken);
            var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await AfterSaveChangesAsync(entities, cancellationToken);

            return result;
        }

        #region LifeTime
        protected virtual void BeforeSaveChanges(IEnumerable<EntityEntry> entityEntries)
        {
            if (ServiceLocator.Instance == null)
                return;

            using (var scpoed = ServiceLocator.Instance.Locator.CreateScope())
            {
                var provider = scpoed.ServiceProvider;

                var repositoryPreLifetime = provider.GetServices<IRepositoryPreSaveChanges>().OrderBy(p => p.Order);

                foreach (var preSaveChange in repositoryPreLifetime)
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
        }

        protected virtual void AfterSaveChanges(IEnumerable<EntityEntry> entityEntries)
        {
            if (ServiceLocator.Instance == null)
                return;

            using (var scpoed = ServiceLocator.Instance.Locator.CreateScope())
            {
                var provider = scpoed.ServiceProvider;

                var repositoryPostLifetime = provider.GetServices<IRepositoryPostSaveChanges>().OrderBy(p => p.Order);

                foreach (var postSaveChange in repositoryPostLifetime)
                {
                    foreach (var entity in entityEntries)
                    {
                        var state = entity.State.ToRepositoryState();
                        postSaveChange.PostSaveChanges(state, entity.Entity);
                    }
                }
            }
        }

        protected virtual async Task BeforeSaveChangesAsync(
            IEnumerable<EntityEntry> entityEntries,
            CancellationToken cancellationToken)
        {
            if (ServiceLocator.Instance == null)
                return;

            using (var scpoed = ServiceLocator.Instance.Locator.CreateScope())
            {
                var provider = scpoed.ServiceProvider;

                var repositoryPreLifetime = provider.GetServices<IRepositoryPreSaveChanges>().OrderBy(p => p.Order);

                foreach (var preSaveChange in repositoryPreLifetime)
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

        protected virtual async Task AfterSaveChangesAsync(
            IEnumerable<EntityEntry> entityEntries,
            CancellationToken cancellationToken)
        {
            if (ServiceLocator.Instance == null)
                return;

            using (var scpoed = ServiceLocator.Instance.Locator.CreateScope())
            {
                var provider = scpoed.ServiceProvider;

                var repositoryPostLifetime = provider.GetServices<IRepositoryPostSaveChanges>().OrderBy(p => p.Order);

                foreach (var postSaveChange in repositoryPostLifetime)
                {
                    foreach (var entity in entityEntries)
                    {
                        var state = entity.State.ToRepositoryState();
                        await postSaveChange.PostSaveChangesAsync(state, entity.Entity, cancellationToken);
                    }
                }
            }
        }
        #endregion
    }
}
