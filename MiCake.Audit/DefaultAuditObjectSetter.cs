using MiCake.Core.Util.Reflection;
using MiCake.Identity.User;
using System;

namespace MiCake.Audit
{
    public class DefaultAuditObjectSetter : IAuditObjectSetter
    {
        private readonly IServiceProvider _serviceProvider;
        public DefaultAuditObjectSetter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void SetCreationInfo(object targetObject)
        {
            SetCreationTime(targetObject);
            SetCreator(targetObject);
        }

        public void SetDeletionInfo(object targetObject)
        {
            if (!(targetObject is ISoftDeletion softDeletion))
                return;

            softDeletion.IsDeleted = true;

            SetDeletionTime(softDeletion);
            SetDeleteUser(softDeletion);
        }

        public void SetModificationInfo(object targetObject)
        {
            SetModificationTime(targetObject);
            SetModifier(targetObject);
        }

        protected virtual void SetCreationTime(object targetObject)
        {
            if (!(targetObject is IHasCreationTime hasCreationTimeObject))
                return;

            if (hasCreationTimeObject.CreationTime == default)
                hasCreationTimeObject.CreationTime = DateTime.Now;
        }

        protected virtual void SetCreator(object targetObject)
        {
            if (!(targetObject is IHasAuditUser))
                return;

            var targetType = targetObject.GetType();
            if (!(targetObject is IHasCreator ||
                TypeHelper.IsImplementedGenericInterface(targetType, typeof(IHasCreator<>))))
                return;

            var currentUser = (ISetAuditPropertyAbility)_serviceProvider.GetService(typeof(ISetAuditPropertyAbility));
            currentUser?.SetAuditProperty(targetObject, SetAuditPropertyType.CreatorID);
        }

        protected virtual void SetModificationTime(object targetObject)
        {
            if (!(targetObject is IHasModificationTime hasModificationTimeObject))
                return;

            hasModificationTimeObject.ModficationTime = DateTime.Now;
        }

        protected virtual void SetModifier(object targetObject)
        {
            if (!(targetObject is IHasAuditUser))
                return;

            var targetType = targetObject.GetType();
            if (!(targetObject is IHasModifier ||
                TypeHelper.IsImplementedGenericInterface(targetType, typeof(IHasModifier<>))))
                return;

            var currentUser = (ISetAuditPropertyAbility)_serviceProvider.GetService(typeof(ISetAuditPropertyAbility));
            currentUser?.SetAuditProperty(targetObject, SetAuditPropertyType.ModifierID);
        }

        protected virtual void SetDeletionTime(object targetObject)
        {
            if (!(targetObject is IHasDeletionTime hasDeletionTimeObject))
                return;

            hasDeletionTimeObject.DeletionTime = DateTime.Now;
        }

        protected virtual void SetDeleteUser(object targetObject)
        {
            if (!(targetObject is IHasAuditUser))
                return;

            var targetType = targetObject.GetType();
            if (!(targetObject is IHasDeleteUser ||
                TypeHelper.IsImplementedGenericInterface(targetType, typeof(IHasDeleteUser<>))))
                return;

            var currentUser = (ISetAuditPropertyAbility)_serviceProvider.GetService(typeof(ISetAuditPropertyAbility));
            currentUser?.SetAuditProperty(targetObject, SetAuditPropertyType.DeleteUserID);
        }

    }
}
