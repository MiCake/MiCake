using Moq;
using System;
using Xunit;

namespace MiCake.Uow.Test
{
    public class MiCake_UowManager_Test
    {
        public MiCake_UowManager_Test()
        {
        }

        [Fact]
        public void Create_TwoScpoe_UnitOfWork()
        {
            var oneUowOption = new UnitOfWorkOptions() { Limit = UnitOfWorkLimit.Required };
            var towUowOption = new UnitOfWorkOptions() { Limit = UnitOfWorkLimit.Suppress };

            IUnitOfWorkManager unitOfWorkManager = Mock.Of<IUnitOfWorkManager>();
            ITransactionFeature efTransaction = Mock.Of<ITransactionFeature>();
            ITransactionFeature adoTransaction = Mock.Of<ITransactionFeature>();

            using (var uowOne = unitOfWorkManager.Create(oneUowOption))
            {
                //Registe ef part
                //这个范围区间需要开辟一个Ef的事务
                uowOne.RegisteTranasctionFeature(Guid.NewGuid().ToString(), efTransaction);

                using (var uowTwo = unitOfWorkManager.Create(towUowOption))
                {
                    //这个范围区间需要开辟一个Ado的事务
                    uowTwo.RegisteTranasctionFeature(Guid.NewGuid().ToString(), adoTransaction);
                }

                using (var uowThree = unitOfWorkManager.Create(oneUowOption))
                {
                    //在该区间又开辟了一个工作单元 
                    //此时由于配置为Required 因此与uowOne公用一个环境事务，该Ado的事务能否融合EF的事务
                    uowThree.RegisteTranasctionFeature(Guid.NewGuid().ToString(), adoTransaction);
                }

            }
        }
    }
}
