using MiCake.Audit;
using MiCake.Audit.Core;
using MiCake.DDD.Extensions;

namespace MiCake.Identity.Audit
{
    /// <summary>
    /// Internal identity audit provider.
    /// </summary>
    /// <typeparam name="TKey">The type of the primary key for the user.</typeparam>
    internal class IdentityAuditProvider<TKey> : IAuditProvider
    {
        public void ApplyAudit(AuditObjectModel auditObjectModel)
        {
            //if entity is not micake user. do noting.
            if (!(auditObjectModel.AuditEntity is IMiCakeUser<TKey> micakeUser))
                return;

            var entity = auditObjectModel.AuditEntity;
            switch (auditObjectModel.EntityState)
            {
                case RepositoryEntityState.Deleted:
                    SetDeleteUser(entity, micakeUser);
                    break;
                case RepositoryEntityState.Modified:
                    SetModifyUser(entity, micakeUser);
                    break;
                case RepositoryEntityState.Added:
                    SetCreateUser(entity, micakeUser);
                    break;
                default:
                    break;
            }
        }

        private void SetCreateUser<TUserKey>(object entity, IMiCakeUser<TUserKey> miCakeUser)
        {
            if (entity is IHasCreator<TUserKey> creatorEntity)
            {
                creatorEntity.CreatorID = miCakeUser.Id;
            }
        }

        private void SetModifyUser<TUserKey>(object entity, IMiCakeUser<TUserKey> miCakeUser)
        {
            if (entity is IHasModifyUser<TUserKey> modifyUser)
            {
                modifyUser.ModifyUserID = miCakeUser.Id;
            }
        }

        private void SetDeleteUser<TUserKey>(object entity, IMiCakeUser<TUserKey> miCakeUser)
        {
            if (entity is IHasDeleteUser<TUserKey> deleteUser)
            {
                deleteUser.DeleteUserID = miCakeUser.Id;
            }
        }
    }
}
