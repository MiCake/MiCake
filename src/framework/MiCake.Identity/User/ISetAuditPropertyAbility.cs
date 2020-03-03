using System;

namespace MiCake.Identity.User
{
    /// <summary>
    /// Provides the ability to set up creators for a entity
    /// </summary>
    public interface ISetAuditPropertyAbility
    {
        /// <summary>
        /// set targetObject CreatorID property
        /// </summary>
        /// <param name="targetObject">a object who has CreatorID property</param>
        /// <param name="type"><see cref="SetAuditPropertyType"/></param>
        void SetAuditProperty(object targetObject, SetAuditPropertyType type);

        /// <summary>
        /// set targetObject CreatorID property
        /// </summary>
        /// <param name="targetObject">a object who has CreatorID property</param>
        /// <param name="type"><see cref="SetAuditPropertyType"/></param>
        /// <param name="assignmentRule">if this delegate return true,can assignment.</param>
        void SetAuditProperty(object targetObject, SetAuditPropertyType type, Func<bool> assignmentRule);
    }

    public enum SetAuditPropertyType
    {
        CreatorID,
        ModifierID,
        DeleteUserID,
    }
}
