using System;

namespace MiCake.Identity.User
{
    public abstract class CurrentUser : CurrentUser<long>
    {
    }

    public abstract class CurrentUser<UserKeyType> : ICurrentUser<UserKeyType>, ISetAuditPropertyAbility
    {
        public abstract UserKeyType UserID { get; set; }

        public CurrentUser()
        {
        }

        public virtual void SetAuditProperty(object targetObject, SetAuditPropertyType type)
        {
            var auditPropertyProperty = targetObject.GetType().GetProperty(type.ToString());
            if (auditPropertyProperty == null)
                return;

            //if userKey type is different from audit generic type. throw a exception
            if (!(auditPropertyProperty.PropertyType == UserID.GetType()))
                throw new ArgumentException("Audit Interface UserKeyType is different from current user primary key." +
                    $"This current primary key is:{typeof(UserKeyType).Name}," +
                    $"But the {targetObject.GetType().Name} key type is : {auditPropertyProperty.PropertyType.Name}");

            auditPropertyProperty.SetValue(targetObject, UserID);
        }

        public virtual void SetAuditProperty(object targetObject, SetAuditPropertyType type, Func<bool> assignmentRule)
        {
            if (!assignmentRule())
                return;

            SetAuditProperty(targetObject, type);
        }
    }
}
