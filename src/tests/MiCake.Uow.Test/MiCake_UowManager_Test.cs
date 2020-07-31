using MiCake.Uow.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiCake.Uow.Tests
{
    public class MiCake_UowManager_Test : UnitOfWorkTestBase
    {
        private UnitOfWorkOptions RequiredNewOptions = new UnitOfWorkOptions(null, null, UnitOfWorkScope.RequiresNew);
        private UnitOfWorkOptions SuppressOptions = new UnitOfWorkOptions(null, null, UnitOfWorkScope.Suppress);

        private IServiceProvider ServiceProvider { get; }

        public MiCake_UowManager_Test()
        {
            ServiceProvider = GetServiceProvider();
        }

        [Fact]
        public void UowManager_CreateUow()
        {
            var manager = ServiceProvider.GetService<IUnitOfWorkManager>();
            var uow = manager.Create();

            Assert.NotNull(uow);
        }

        [Fact]
        public void UowManager_CreateUowWithOptions()
        {
            UnitOfWorkOptions options = new UnitOfWorkOptions(System.Data.IsolationLevel.ReadCommitted);

            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;
            var uow = manager.Create(options);

            Assert.Same(options, uow.UnitOfWorkOptions);
        }

        [Fact]
        public void UowManager_CreateMoreUow()
        {
            UnitOfWorkOptions options = new UnitOfWorkOptions(System.Data.IsolationLevel.ReadCommitted);

            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;

            using (var uow1 = manager.Create())
            {
                using (var uow2 = manager.Create())
                {
                    Assert.Same(uow2, manager.CallContext.GetCurrentUow());
                }

                Assert.Same(uow1, manager.CallContext.GetCurrentUow());
            }

            Assert.Null(manager.GetCurrentUnitOfWork());
        }

        [Fact]
        public void UowManager_CreateChildUnitOfWork()
        {
            UnitOfWorkOptions options = new UnitOfWorkOptions(System.Data.IsolationLevel.ReadCommitted);

            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;

            using (var uow1 = manager.Create())
            {
                using (var uow2 = manager.Create())
                {
                    //由于没有指定配置，使用默认Required，则会使用外环境的工作单元配置
                    Assert.IsAssignableFrom<IChildUnitOfWork>(uow2);
                }
            }

            Assert.Null(manager.GetCurrentUnitOfWork());
        }

        [Fact]
        public void UowManager_NestedUnitOfWork_UseRequiresNewScope()
        {
            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;

            using (var uow1 = manager.Create())
            {
                using (var uow2 = manager.Create(UnitOfWorkScope.RequiresNew))
                {
                    //RequiresNew,则重新创建一个独立的工作单元
                    Assert.Null(uow2 as IChildUnitOfWork);
                }
            }
        }

        [Fact]
        public void UowManager_NestedUnitOfWork_UseSurpress()
        {
            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;

            using (var uow1 = manager.Create())
            {
                using (var uow2 = manager.Create(UnitOfWorkScope.RequiresNew))
                {
                    //Suppress,则重新创建一个独立的工作单元
                    Assert.Null(uow2 as IChildUnitOfWork);
                    Assert.Equal(UnitOfWorkScope.RequiresNew, uow2.UnitOfWorkOptions.Scope);
                }
                Assert.Equal(UnitOfWorkScope.Required, uow1.UnitOfWorkOptions.Scope);
            }
        }

        [Fact]
        public void UowManager_NestedMoreUnitOfWork()
        {
            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;

            using (var uow1 = manager.Create())
            {
                using (var uow2 = manager.Create(UnitOfWorkScope.RequiresNew))
                {
                    Assert.Null(uow2 as IChildUnitOfWork);
                    Assert.Equal(uow2, manager.GetCurrentUnitOfWork());

                    using (var uow3 = manager.Create(UnitOfWorkScope.Suppress))
                    {
                        Assert.Null(uow3 as IChildUnitOfWork);
                        Assert.Equal(uow3, manager.GetCurrentUnitOfWork());

                        using (var uow4 = manager.Create())
                        {
                            Assert.IsAssignableFrom<IChildUnitOfWork>(uow4);
                            Assert.Equal(uow4, manager.GetCurrentUnitOfWork());
                        }
                    }
                }
            }

            //最后所有的工作单元都将被释放
            Assert.Null(manager.GetCurrentUnitOfWork());
        }

        [Fact]
        public void UowManager_ParallelUnitOfWork()
        {
            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;

            using (var uow1 = manager.Create())
            {
                Assert.Equal(uow1, manager.GetCurrentUnitOfWork());
            }

            using (var uow2 = manager.Create())
            {
                Assert.Equal(uow2, manager.GetCurrentUnitOfWork());
            }

            //最后所有的工作单元都将被释放
            Assert.Null(manager.GetCurrentUnitOfWork());
        }

        [Fact]
        public void UowManager_DifferenScope()
        {
            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;

            using (var uow1 = manager.Create())
            {
                Assert.Equal(uow1, manager.GetCurrentUnitOfWork());

                var manager2 = ServiceProvider.CreateScope().ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;
                using (var uow2 = manager2.Create())
                {
                    Assert.Null(uow2 as IChildUnitOfWork);
                }

                Assert.Null(manager2.GetCurrentUnitOfWork());
            }

            Assert.Null(manager.GetCurrentUnitOfWork());
        }

        [Fact]
        public void ChildUow_UseItSelfEvents()
        {
            var manager = ServiceProvider.GetService<IUnitOfWorkManager>() as UnitOfWorkManager;
            string saveInfo = "";
            string rollbackInfo = "";
            string disposeInfo = "";

            string saveInfo2 = "";
            string rollbackInfo2 = "";
            string disposeInfo2 = "";

            var options1 = new UnitOfWorkOptions();
            options1.Events.OnCompleted += s =>
            {
                saveInfo = "1";
                return Task.CompletedTask;
            };
            options1.Events.OnRollbacked += s =>
            {
                rollbackInfo = "1";
                return Task.CompletedTask;
            };
            options1.Events.OnDispose += s =>
            {
                disposeInfo = "1";
                return Task.CompletedTask;
            };

            var options2 = new UnitOfWorkOptions();
            options2.Events.OnCompleted += s =>
            {
                saveInfo2 = "2";
                return Task.CompletedTask;
            };
            options2.Events.OnRollbacked += s =>
            {
                rollbackInfo2 = "2";
                return Task.CompletedTask;
            };
            options2.Events.OnDispose += s =>
            {
                disposeInfo2 = "2";
                return Task.CompletedTask;
            };

            using (var uow1 = manager.Create(options1))
            {
                using (var uow2 = manager.Create(options2))
                {
                    uow2.SaveChanges();
                    uow2.Rollback();
                }
                uow1.SaveChanges();
                uow1.Rollback();
            }

            Assert.Equal("1", saveInfo);
            Assert.Equal("1", rollbackInfo);
            Assert.Equal("1", disposeInfo);

            Assert.Equal("2", saveInfo2);
            Assert.Equal("2", rollbackInfo2);
            Assert.Equal("2", disposeInfo2);
        }


        #region 多线程下无法控制
        //[Fact(DisplayName = "多线程条件下获取工作单元")]
        //public void Concurrent_GetUnitOfWork()
        //{
        //    var uowMangager = GetUnitOfWorkManager();

        //    using (var uowOne = uowMangager.Create())
        //    {
        //        Task.Run(() =>
        //        {
        //            Thread.Sleep(500);
        //            //当前是uowTwo
        //            uowMangager.GetCurrentUnitOfWork();
        //            Thread.Sleep(1000);
        //            //当前是uowOne.
        //            uowMangager.GetCurrentUnitOfWork();

        //            //在多线程中GetCurrentUnitOfWork会带来不准确性
        //        });

        //        Task.Run(() =>
        //        {
        //            using (var uowTwo = uowMangager.Create())
        //            {
        //                Thread.Sleep(1000);
        //            }
        //        });

        //        Thread.Sleep(1000);
        //    }
        //}
        #endregion
    }
}
