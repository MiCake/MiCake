using MiCake.Audit.Tests.Fakes;
using System;
using Xunit;

namespace MiCake.Audit.Tests
{
    public class AuditPropertySetter_Test : AuditTestBase
    {
        private Guid userID = new Guid("cc1b0a62-c76f-487f-8805-e6657aedbf27");

        public AuditPropertySetter_Test() : base()
        {
        }

        #region modifition
        [Fact]
        public void HasModifier_ShouldHasModificationTime()
        {
            ModelHasModificationTime modelHasModifier = new ModelHasModificationTime();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));

            auditContext.ObjectSetter.SetModificationInfo(modelHasModifier);

            Assert.NotNull(modelHasModifier.ModficationTime);
            Assert.NotEqual(default(DateTime), modelHasModifier.ModficationTime);
        }

        [Fact]
        public void HasMofificationAudit_ShouldHasMofifierID()
        {
            ModelHasModificationAuditGeneric modelHasModificationAudit = new ModelHasModificationAuditGeneric();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));

            auditContext.ObjectSetter.SetModificationInfo(modelHasModificationAudit);

            Assert.NotNull(modelHasModificationAudit.ModficationTime);

            Assert.Equal(modelHasModificationAudit.ModifierID, userID);
        }

        [Fact]
        public void HasMofificationAudit_DifferentUserTypeThrowException()
        {
            ModelHasModificationAudit modelHasModificationAudit = new ModelHasModificationAudit();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));
            Assert.Throws<ArgumentException>(() =>
            {
                auditContext.ObjectSetter.SetModificationInfo(modelHasModificationAudit);
            });
        }

        #endregion

        #region creation
        [Fact]
        public void HasCreationTime_ShouldHasCreationTime()
        {
            ModelHasCreationTime modelHasCreationTime = new ModelHasCreationTime();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));

            auditContext.ObjectSetter.SetCreationInfo(modelHasCreationTime);

            Assert.NotEqual(default, modelHasCreationTime.CreationTime);
        }

        [Fact]
        public void HasCreationAudit_ShouldHasCreatorID()
        {
            ModelHasCreationAuditGeneric modelHasCreationAudit = new ModelHasCreationAuditGeneric();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));

            auditContext.ObjectSetter.SetCreationInfo(modelHasCreationAudit);

            Assert.NotEqual(default, modelHasCreationAudit.CreationTime);

            Assert.Equal(modelHasCreationAudit.CreatorID, userID);
        }

        [Fact]
        public void HasCreationAudit_DifferentUserTypeThrowException()
        {
            ModelHasCreationAudit modelHasCreationAudit = new ModelHasCreationAudit();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));
            Assert.Throws<ArgumentException>(() =>
            {
                auditContext.ObjectSetter.SetCreationInfo(modelHasCreationAudit);
            });
        }
        #endregion

        #region deletion
        [Fact]
        public void HasDeletionTimeAndSoftDeletion_ShouldHasDeletionTime()
        {
            ModelHasDeletionTimeAndSoftDeletion modelHasDeletionTimeAndSoftDeletion = new ModelHasDeletionTimeAndSoftDeletion();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));

            auditContext.ObjectSetter.SetDeletionInfo(modelHasDeletionTimeAndSoftDeletion);

            Assert.NotEqual(default, modelHasDeletionTimeAndSoftDeletion.DeletionTime.Value);
        }

        [Fact]
        public void HasDeletionTimeWithoutDeletionTime_ShouldNoDeletionTime()
        {
            ModelHasDeletionTime modelHasDeletionTime = new ModelHasDeletionTime();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));

            auditContext.ObjectSetter.SetDeletionInfo(modelHasDeletionTime);

            Assert.Null(modelHasDeletionTime.DeletionTime);
        }


        [Fact]
        public void HasDeletionAudit_ShouldHasDeleteUserID()
        {
            ModelHasDeletionAuditGeneric modelHasDeletionAudit = new ModelHasDeletionAuditGeneric();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));

            auditContext.ObjectSetter.SetDeletionInfo(modelHasDeletionAudit);

            Assert.NotEqual(default, modelHasDeletionAudit.DeletionTime.Value);

            Assert.Equal(modelHasDeletionAudit.DeleteUserID, userID);
        }

        [Fact]
        public void HasDeletionAudit_DifferentUserTypeThrowException()
        {
            ModelHasDeletionAudit modelHasDeletionAudit = new ModelHasDeletionAudit();
            var auditContext = (IAuditContext)ServiceProvider.GetService(typeof(IAuditContext));
            Assert.Throws<ArgumentException>(() =>
            {
                auditContext.ObjectSetter.SetDeletionInfo(modelHasDeletionAudit);
            });
        }
        #endregion
    }
}
