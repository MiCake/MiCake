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
                //�����Χ������Ҫ����һ��Ef������
                uowOne.RegisteTranasctionFeature(Guid.NewGuid().ToString(), efTransaction);

                using (var uowTwo = unitOfWorkManager.Create(towUowOption))
                {
                    //�����Χ������Ҫ����һ��Ado������
                    uowTwo.RegisteTranasctionFeature(Guid.NewGuid().ToString(), adoTransaction);
                }

                using (var uowThree = unitOfWorkManager.Create(oneUowOption))
                {
                    //�ڸ������ֿ�����һ��������Ԫ 
                    //��ʱ��������ΪRequired �����uowOne����һ���������񣬸�Ado�������ܷ��ں�EF������
                    uowThree.RegisteTranasctionFeature(Guid.NewGuid().ToString(), adoTransaction);
                }

            }
        }
    }
}
