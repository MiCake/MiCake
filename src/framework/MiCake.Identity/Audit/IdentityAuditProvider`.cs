using MiCake.Audit;
using MiCake.Audit.Core;
using MiCake.Audit.SoftDeletion;
using MiCake.DDD.Extensions;

namespace MiCake.Identity.Audit
{
    /// <summary>
    /// Internal identity audit provider.
    /// </summary>
    /// <typeparam name="TKey">The type of the primary key for the user.</typeparam>
    internal class IdentityAuditProvider<TKey> : IAuditProvider
    {
        private readonly ICurrentMiCakeUser<TKey> _currentMiCakeUser;

        public IdentityAuditProvider(ICurrentMiCakeUser<TKey> currentMiCakeUser)
        {
            _currentMiCakeUser = currentMiCakeUser;
        }

        public virtual void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            //if there is no user login.do nothing.
            if (_currentMiCakeUser.UserId.Equals(default(TKey)))
                return;

            var entity = auditObjectModel.AuditEntity;
            var userID = _currentMiCakeUser.UserId;
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

        protected virtual void SetCreateUser(object entity, object userID)
        {
            if (entity is IHasCreator<TKey> creatorEntity)
                creatorEntity.CreatorID = (TKey)userID;
        }

        protected virtual void SetModifyUser(object entity, object userID)
        {
            if (entity is IHasModifyUser<TKey> modifyUser)
                modifyUser.ModifyUserID = (TKey)userID;
        }

        protected virtual void SetDeleteUser(object entity, object userID)
        {
            if (entity is ISoftDeletion && entity is IHasDeleteUser<TKey> deleteUser)
                deleteUser.DeleteUserID = (TKey)userID;
        }
    }

    internal class IdentityAuditProviderForStruct<TKey> : IdentityAuditProvider<TKey> where TKey : struct
    {
        public IdentityAuditProviderForStruct(ICurrentMiCakeUser<TKey> currentMiCakeUser) : base(currentMiCakeUser)
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
