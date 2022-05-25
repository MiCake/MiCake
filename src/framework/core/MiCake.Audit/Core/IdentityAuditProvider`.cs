using MiCake.Audit.SoftDeletion;
using MiCake.Cord;
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
            if (entity is IHasCreator<TKey> creatorEntity)
                creatorEntity.CreatorID = (TKey)userID;
        }

        protected override void SetModifyUser(object entity, object userID)
        {
            if (entity is IHasModifyUser<TKey> modifyUser)
                modifyUser.ModifyUserID = (TKey)userID;
        }

        protected override void SetDeleteUser(object entity, object userID)
        {
            if (entity is ISoftDeletion && entity is IHasDeleteUser<TKey> deleteUser)
                deleteUser.DeleteUserID = (TKey)userID;
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
            if (entity is IMayHasCreator<TKey> creatorEntity)
                creatorEntity.CreatorID = (TKey)userID;
        }

        protected override void SetModifyUser(object entity, object userID)
        {
            if (entity is IMayHasModifyUser<TKey> modifyUser)
                modifyUser.ModifyUserID = (TKey)userID;
        }

        protected override void SetDeleteUser(object entity, object userID)
        {
            if (entity is ISoftDeletion && entity is IMayHasDeleteUser<TKey> deleteUser)
                deleteUser.DeleteUserID = (TKey)userID;
        }
    }
}
