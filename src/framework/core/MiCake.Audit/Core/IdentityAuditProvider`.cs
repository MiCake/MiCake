using MiCake.Audit.SoftDeletion;
using MiCake.Cord;
using MiCake.Core.Util.Reflection;
using MiCake.Identity;

namespace MiCake.Audit.Core
{
    internal abstract class BaseIdentityAuditProvider : IAuditProvider
    {
        private readonly ICurrentMiCakeUser _currentMiCakeUser;

        public BaseIdentityAuditProvider(ICurrentMiCakeUser currentMiCakeUser)
        {
            _currentMiCakeUser = currentMiCakeUser;
        }

        public void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            //if there is no user login.do nothing.
            if (!_currentMiCakeUser.IsLogin)
                return;

            var entity = auditObjectModel.AuditEntity;

            if (entity is not ICanAuditUser)
                return;

            var userID = _currentMiCakeUser.UserId!;
            switch (auditObjectModel.EntityState)
            {
                case RepositoryEntityState.Deleted:
                    SetDeletedUser(entity, userID);
                    break;
                case RepositoryEntityState.Modified:
                    SetUpdatedUser(entity, userID);
                    break;
                case RepositoryEntityState.Added:
                    SetCreatedUser(entity, userID);
                    break;
                default:
                    break;
            }
        }

        protected abstract void SetCreatedUser(object entity, object userID);

        protected abstract void SetUpdatedUser(object entity, object userID);

        protected abstract void SetDeletedUser(object entity, object userID);

    }

    /// <summary>
    /// Internal identity audit provider.
    /// </summary>
    internal class IdentityAuditProvider<TKey> : BaseIdentityAuditProvider where TKey : struct
    {
        public IdentityAuditProvider(ICurrentMiCakeUser currentMiCakeUser) : base(currentMiCakeUser)
        {
        }

        protected override void SetCreatedUser(object entity, object userID)
        {
            if (entity is IHasCreatedUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasCreatedUser<TKey>.CreatedBy), userID);
            }
        }

        protected override void SetUpdatedUser(object entity, object userID)
        {
            if (entity is IHasUpdatedUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasUpdatedUser<TKey>.UpdatedBy), userID);
            }
        }

        protected override void SetDeletedUser(object entity, object userID)
        {
            if (entity is ISoftDeletion && entity is IHasDeletedUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasDeletedUser<TKey>.DeletedBy), userID);
            }
        }
    }

    /// <summary>
    /// Internal identity audit provider.
    /// </summary>
    internal class IdentityAuditProviderForReferenceKey<TKey> : BaseIdentityAuditProvider where TKey : class
    {
        public IdentityAuditProviderForReferenceKey(ICurrentMiCakeUser currentMiCakeUser) : base(currentMiCakeUser)
        {
        }

        protected override void SetCreatedUser(object entity, object userID)
        {
            if (entity is IMayHasCreatedUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IMayHasCreatedUser<TKey>.CreatedBy), userID);
            }
        }

        protected override void SetUpdatedUser(object entity, object userID)
        {
            if (entity is IMayHasUpdatedUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IMayHasUpdatedUser<TKey>.UpdatedBy), userID);
            }
        }

        protected override void SetDeletedUser(object entity, object userID)
        {
            if (entity is ISoftDeletion && entity is IMayHasDeletedUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IMayHasDeletedUser<TKey>.DeletedBy), userID);
            }
        }
    }
}
