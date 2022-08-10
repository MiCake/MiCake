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

            if (entity is not IHasAuditUser)
                return;

            var userID = _currentMiCakeUser.UserId!;
            switch (auditObjectModel.EntityState)
            {
                case RepositoryEntityState.Deleted:
                    SetDeleteUser(entity, userID);
                    break;
                case RepositoryEntityState.Modified:
                    SetModifyUser(entity, userID);
                    break;
                case RepositoryEntityState.Added:
                    SetCreateUser(entity, userID);
                    break;
                default:
                    break;
            }
        }

        protected abstract void SetCreateUser(object entity, object userID);

        protected abstract void SetModifyUser(object entity, object userID);

        protected abstract void SetDeleteUser(object entity, object userID);

    }

    /// <summary>
    /// Internal identity audit provider.
    /// </summary>
    internal class IdentityAuditProvider<TKey> : BaseIdentityAuditProvider where TKey : struct
    {
        public IdentityAuditProvider(ICurrentMiCakeUser currentMiCakeUser) : base(currentMiCakeUser)
        {
        }

        protected override void SetCreateUser(object entity, object userID)
        {
            if (entity is IHasCreator<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasCreator<TKey>.CreatorID), userID);
            }
        }

        protected override void SetModifyUser(object entity, object userID)
        {
            if (entity is IHasModifyUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasModifyUser<TKey>.ModifyUserID), userID);
            }
        }

        protected override void SetDeleteUser(object entity, object userID)
        {
            if (entity is ISoftDeletion && entity is IHasDeleteUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IHasDeleteUser<TKey>.DeleteUserID), userID);
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

        protected override void SetCreateUser(object entity, object userID)
        {
            if (entity is IMayHasCreator<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IMayHasCreator<TKey>.CreatorID), userID);
            }
        }

        protected override void SetModifyUser(object entity, object userID)
        {
            if (entity is IMayHasModifyUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IMayHasModifyUser<TKey>.ModifyUserID), userID);
            }
        }

        protected override void SetDeleteUser(object entity, object userID)
        {
            if (entity is ISoftDeletion && entity is IMayHasDeleteUser<TKey>)
            {
                ReflectionHelper.SetValueByPath(entity, entity.GetType(), nameof(IMayHasDeleteUser<TKey>.DeleteUserID), userID);
            }
        }
    }
}
