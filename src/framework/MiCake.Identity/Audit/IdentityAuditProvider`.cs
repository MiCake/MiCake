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
    public class IdentityAuditProvider<TKey> : IAuditProvider
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

        private void SetCreateUser<TUserKey>(object entity, TUserKey userID)
        {
            if (entity is IHasCreator<TUserKey> creatorEntity)
                creatorEntity.CreatorID = userID;
        }

        private void SetModifyUser<TUserKey>(object entity, TUserKey userID)
        {
            if (entity is IHasModifyUser<TUserKey> modifyUser)
                modifyUser.ModifyUserID = userID;
        }

        private void SetDeleteUser<TUserKey>(object entity, TUserKey userID)
        {
            if (entity is ISoftDeletion && entity is IHasDeleteUser<TUserKey> deleteUser)
                deleteUser.DeleteUserID = userID;
        }
    }
}
