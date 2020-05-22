using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Uow.Test
{
    public class MiCake_UowManager_Test
    {
        private UnitOfWorkOptions requiredNewOptions;
        private UnitOfWorkOptions suppressOptions;

        public MiCake_UowManager_Test()
        {
            requiredNewOptions = new UnitOfWorkOptions(null, null, UnitOfWorkScope.RequiresNew);
            suppressOptions = new UnitOfWorkOptions(null, null, UnitOfWorkScope.Suppress);
        }

        [Fact]
        public void Create_ChildUowType_Test()
        {
            var uowMangager = GetUnitOfWorkManager();

            using (var uowOne = uowMangager.Create())
            {
                Assert.IsType<UnitOfWork>(uowOne);
                using (var uowTwo = uowMangager.Create())
                {
                    Assert.IsNotType<UnitOfWork>(uowTwo);

                    using (var twoChild = uowMangager.Create())
                    {
                        Assert.IsNotType<UnitOfWork>(twoChild);
                    }
                }
                using (var uowThree = uowMangager.Create())
                {
                    Assert.IsNotType<UnitOfWork>(uowThree);
                }
            }
        }

        [Fact]
        public void Create_MultipleNestingUow_Test()
        {
            var uowMangager = GetUnitOfWorkManager();

            using (var uowOne = uowMangager.Create())
            {
                Assert.Equal(uowOne.ID, uowMangager.GetCurrentUnitOfWork().ID);

                using (var uowTwo = uowMangager.Create())
                {
                    Assert.Equal(uowTwo.ID, uowMangager.GetCurrentUnitOfWork().ID);

                    using (var uowTwo_One = uowMangager.Create())
                    {
                        Assert.Equal(uowTwo_One.ID, uowMangager.GetCurrentUnitOfWork().ID);

                        using (var uowTwo_One_One = uowMangager.Create())
                        {
                            Assert.Equal(uowTwo_One_One.ID, uowMangager.GetCurrentUnitOfWork().ID);
                        }
                    }
                }
                using (var uowThree = uowMangager.Create())
                {
                    Assert.Equal(uowThree.ID, uowMangager.GetCurrentUnitOfWork().ID);
                }
            }

            using (var uowOne = uowMangager.Create())
            {
                Assert.Equal(uowOne.ID, uowMangager.GetCurrentUnitOfWork().ID);

                using (var uowTwo = uowMangager.Create())
                {
                    Assert.Equal(uowTwo.ID, uowMangager.GetCurrentUnitOfWork().ID);

                    using (var uowTwo_One = uowMangager.Create())
                    {
                        Assert.Equal(uowTwo_One.ID, uowMangager.GetCurrentUnitOfWork().ID);

                        using (var uowTwo_One_One = uowMangager.Create())
                        {
                            Assert.Equal(uowTwo_One_One.ID, uowMangager.GetCurrentUnitOfWork().ID);
                        }
                    }
                }
                using (var uowThree = uowMangager.Create())
                {
                    Assert.Equal(uowThree.ID, uowMangager.GetCurrentUnitOfWork().ID);
                }
            }
        }

        [Fact]
        public void Create_TwoParallelUow_Test()
        {
            var uowMangager = GetUnitOfWorkManager();

            using (var uowOne = uowMangager.Create())
            {
                Assert.Equal(uowOne.ID, uowMangager.GetCurrentUnitOfWork().ID);

                using (var uowTwo = uowMangager.Create())
                {
                    Assert.Equal(uowTwo.ID, uowMangager.GetCurrentUnitOfWork().ID);
                }
                using (var uowThree = uowMangager.Create())
                {
                    Assert.Equal(uowThree.ID, uowMangager.GetCurrentUnitOfWork().ID);
                }
            }
        }

        [Fact]
        public void Create_AddTransactionFeature()
        {
            var uowMangager = GetUnitOfWorkManager();

            using (var uowOne = uowMangager.Create())
            {
                Task.Run(() =>
                {
                    Thread.Sleep(500);
                    //��ǰ��uowTwo
                    uowMangager.GetCurrentUnitOfWork();
                    Thread.Sleep(1000);
                    //��ǰ��uowOne.
                    uowMangager.GetCurrentUnitOfWork();


                    //�ڶ��߳���GetCurrentUnitOfWork�������׼ȷ��
                });

                Task.Run(() =>
                {
                    using (var uowTwo = uowMangager.Create())
                    {
                        Thread.Sleep(1000);
                    }
                });

                Thread.Sleep(1000);
            }
        }

        [Fact]
        public void Find_UowInstance_Test()
        {
            var uowMangager = GetUnitOfWorkManager();

            using (var uowOne = uowMangager.Create())
            {
                var findInstanceOne = uowMangager.GetUnitOfWork(uowOne.ID);
                Assert.Equal(uowOne.ID, findInstanceOne.ID);

                // find no have instance
                var nohaveInstance = uowMangager.GetUnitOfWork(Guid.NewGuid());
                Assert.Null(nohaveInstance);

                using (var uowTwo = uowMangager.Create())
                {
                    var findInstanceTwo = uowMangager.GetUnitOfWork(uowTwo.ID);
                    Assert.Equal(uowTwo.ID, findInstanceTwo.ID);

                    // find no have instance
                    var noTwohaveInstance = uowMangager.GetUnitOfWork(Guid.NewGuid());
                    Assert.Null(noTwohaveInstance);
                }
                using (var uowThree = uowMangager.Create())
                {
                    var findInstanceThree = uowMangager.GetUnitOfWork(uowThree.ID);
                    Assert.Equal(uowThree.ID, uowThree.ID);
                }
            }

            var nowUow = uowMangager.GetCurrentUnitOfWork();
            Assert.Null(nowUow);
        }

        [Fact]
        public void DiffentOptions_UowType_Test()
        {
            var uowMangager = GetUnitOfWorkManager();

            using (var uowOne = uowMangager.Create())
            {
                Assert.IsType<UnitOfWork>(uowOne);

                using (var uowTwo = uowMangager.Create(suppressOptions))
                {
                    Assert.Equal(uowTwo.ID, uowMangager.GetCurrentUnitOfWork().ID);
                    Assert.IsType<UnitOfWork>(uowTwo);
                }
                using (var uowThree = uowMangager.Create(requiredNewOptions))
                {
                    Assert.Equal(uowThree.ID, uowMangager.GetCurrentUnitOfWork().ID);
                    Assert.IsType<UnitOfWork>(uowThree);
                }
            }
        }

        private IUnitOfWorkManager GetUnitOfWorkManager()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            var defaultUowOptions = new UnitOfWorkDefaultOptions() { Limit = UnitOfWorkScope.Required };
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(defaultUowOptions));

            var provider = services.BuildServiceProvider();

            return provider.GetService<IUnitOfWorkManager>();
        }

        class DemoFeature : ITransactionFeature
        {
            public bool IsCommit => false;

            public bool IsRollback => false;

            public void Commit()
            {
                throw new NotImplementedException();
            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }

            public void Rollback()
            {
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
