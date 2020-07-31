using MiCake.Uow.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace MiCake.Uow.Tests
{
    public class UnitOfWork_Tests : UnitOfWorkTestBase
    {
        public UnitOfWork_Tests()
        {
        }

        [Fact]
        public void UnitOfWork_OnlyOneProvider_CanCreate()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);

                //Provider接受了当前Executor,并且开启了事务
                Assert.True(currentDbExecutor.HasTransaction);
                //使用TransactionScope时，将会开启一个当前事务
                Assert.NotNull(Transaction.Current);

                uow.SaveChanges();
            }

            Thread.Sleep(100);
            Assert.True(currentDbExecutor.IsDispose);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public async Task UnitOfWork_OnlyOneProvider_CanCreate_Async()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);

                //Provider接受了当前Executor,并且开启了事务
                Assert.True(currentDbExecutor.HasTransaction);
                //使用TransactionScope时，将会开启一个当前事务
                Assert.NotNull(Transaction.Current);

                await uow.SaveChangesAsync();
            }

            Assert.True(currentDbExecutor.IsDispose);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public void UnitOfWork_NoAnyProvider()
        {
            //没有事务支持程序
            var provider = GetServiceProvider();

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var dbContext = new FakeDbContext();
            var currentDbExecutor = new ScopeDbExecutor(dbContext);

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);
                uow.SaveChanges();

                Assert.False(currentDbExecutor.HasTransaction);
            }

            //没有事务提供程序，但是该操作对象还是会被托管释放
            Assert.True(currentDbExecutor.IsDispose);
            Assert.True(dbContext.IsDispose);
        }

        [Fact]
        public async Task UnitOfWork_NoAnyProvider_Async()
        {
            //没有事务支持程序
            var provider = GetServiceProvider();

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var dbContext = new FakeDbContext();
            var currentDbExecutor = new ScopeDbExecutor(dbContext);

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);
                await uow.SaveChangesAsync();

                Assert.False(currentDbExecutor.HasTransaction);
            }

            //没有事务提供程序，但是该操作对象还是会被托管释放
            Assert.True(currentDbExecutor.IsDispose);
            Assert.True(dbContext.IsDispose);
        }

        [Fact]
        public void UnitOfWork_NoAnyProvider_DbContextManualDispose()
        {
            //没有事务支持程序
            var provider = GetServiceProvider();
            var uowManager = provider.GetService<IUnitOfWorkManager>();

            FakeDbContext fakeDbContext;
            ScopeDbExecutor fakeDbExecutor;
            using (var uow = uowManager.Create())
            {
                using (var dbContext = new FakeDbContext())
                {
                    fakeDbContext = dbContext;

                    fakeDbExecutor = new ScopeDbExecutor(dbContext);

                    uow.TryAddDbExecutor(fakeDbExecutor);
                    uow.SaveChanges();

                    Assert.False(fakeDbExecutor.HasTransaction);
                    Assert.False(fakeDbContext.IsDispose);
                }
                //手动释放了dbcontext;
                Assert.True(fakeDbContext.IsDispose);
            }
            //dbContext还会在工作单元释放时被释放，但是不会报错。
            Assert.True(fakeDbContext.IsDispose);
        }

        [Fact]
        public async Task UnitOfWork_NoAnyProvider_DbContextManualDispose_Async()
        {
            //没有事务支持程序
            var provider = GetServiceProvider();
            var uowManager = provider.GetService<IUnitOfWorkManager>();

            FakeDbContext fakeDbContext;
            ScopeDbExecutor fakeDbExecutor;
            using (var uow = uowManager.Create())
            {
                using (var dbContext = new FakeDbContext())
                {
                    fakeDbContext = dbContext;

                    fakeDbExecutor = new ScopeDbExecutor(dbContext);

                    await uow.TryAddDbExecutorAsync(fakeDbExecutor);
                    await uow.SaveChangesAsync();

                    Assert.False(fakeDbExecutor.HasTransaction);
                    Assert.False(fakeDbContext.IsDispose);
                }
                //手动释放了dbcontext;
                Assert.True(fakeDbContext.IsDispose);
            }
            //dbContext还会在工作单元释放时被释放，但是不会报错。
            Assert.True(fakeDbContext.IsDispose);
        }

        [Fact]
        public void MoreProvider_UseSuitableProvider()
        {
            var provider = GetServiceProvider(AddTwoDifferentTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);

                //具有多个Provider，但是它只会接受TestScopeTransactionProvider
                Assert.True(currentDbExecutor.HasTransaction);
                Assert.IsType<TransactionScope>(currentDbExecutor.DbOjectInstance.Trsansaction);

                uow.SaveChanges();
            }

            Assert.True(currentDbExecutor.IsDispose);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public async Task MoreProvider_UseSuitableProvider_Async()
        {
            var provider = GetServiceProvider(AddTwoDifferentTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);

                //具有多个Provider，但是它只会接受TestScopeTransactionProvider
                Assert.True(currentDbExecutor.HasTransaction);
                Assert.IsType<TransactionScope>(currentDbExecutor.DbOjectInstance.Trsansaction);

                await uow.SaveChangesAsync();
            }

            Assert.True(currentDbExecutor.IsDispose);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public void MoreSuitableProvider_UseOrderLowest()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);

                //具有多个同类型的Provider，但是它只会接受TestScopeTransactionProvider
                var transactionObj = currentDbExecutor.TransactionObject as TestScopeTransactionObject;

                Assert.NotNull(transactionObj);
                Assert.Equal("TestScope", transactionObj.Source);

                uow.SaveChanges();
            }

            Assert.True(currentDbExecutor.IsDispose);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public async Task MoreSuitableProvider_UseOrderLowest_Async()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);

                //具有多个同类型的Provider，但是它只会接受TestScopeTransactionProvider
                var transactionObj = currentDbExecutor.TransactionObject as TestScopeTransactionObject;

                Assert.NotNull(transactionObj);
                Assert.Equal("TestScope", transactionObj.Source);

                await uow.SaveChangesAsync();
            }

            Assert.True(currentDbExecutor.IsDispose);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public void MoreProvider_ButNoSuitable_ShouldNoTransaction()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            //两个provider都不接受类型为DemoDbExecutor
            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new DemoDbExecutor(new DemoDbContext());

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);
                //没有事务提供程序为它提供事务
                Assert.False(currentDbExecutor.HasTransaction);

                uow.SaveChanges();
            }

            //但是会被添加到工作单元，最后于工作单元一同释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public async Task MoreProvider_ButNoSuitable_ShouldNoTransaction_Async()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            //两个provider都不接受类型为DemoDbExecutor
            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new DemoDbExecutor(new DemoDbContext());

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);
                //没有事务提供程序为它提供事务
                Assert.False(currentDbExecutor.HasTransaction);

                await uow.SaveChangesAsync();
            }

            //但是会被添加到工作单元，最后于工作单元一同释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public void MoreProvider_CannotReused()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());
            var currentDbExecutor2 = new DemoDbExecutor(new DemoDbContext());

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);
                //此时工作单元中已经有一个事务，但是由于类型不同currentDbExecutor2不能复用
                uow.TryAddDbExecutor(currentDbExecutor2);

                Assert.True(currentDbExecutor.HasTransaction);
                Assert.False(currentDbExecutor2.HasTransaction);

                uow.SaveChanges();
            }

            //但是会被添加到工作单元，最后于工作单元一同释放
            Assert.True(currentDbExecutor.IsDispose);
            Assert.True(currentDbExecutor2.IsDispose);
        }

        [Fact]
        public async Task MoreProvider_CannotReused_Async()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());
            var currentDbExecutor2 = new DemoDbExecutor(new DemoDbContext());

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);
                //此时工作单元中已经有一个事务，但是由于类型不同currentDbExecutor2不能复用
                uow.TryAddDbExecutor(currentDbExecutor2);

                Assert.True(currentDbExecutor.HasTransaction);
                Assert.False(currentDbExecutor2.HasTransaction);

                await uow.SaveChangesAsync();
            }

            //但是会被添加到工作单元，最后于工作单元一同释放
            Assert.True(currentDbExecutor.IsDispose);
            Assert.True(currentDbExecutor2.IsDispose);
        }

        [Fact]
        public void MoreProvider_CanReused()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());
            var currentDbExecutor2 = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);
                //此时工作单元中已经有一个事务，同一类型的执行对象可以复用已有事务
                uow.TryAddDbExecutor(currentDbExecutor2);

                Assert.True(currentDbExecutor.HasTransaction);
                Assert.True(currentDbExecutor2.HasTransaction);


                //currentDbExecutor2的事务来源是上一个事务
                var transactionObj = currentDbExecutor2.TransactionObject as TestScopeTransactionObject;
                Assert.NotNull(transactionObj);
                Assert.Equal("TestScope", transactionObj.Source);

                uow.SaveChanges();
            }

            //但是会被添加到工作单元，最后于工作单元一同释放
            Assert.True(currentDbExecutor.IsDispose);
            Assert.True(currentDbExecutor2.IsDispose);
        }


        [Fact]
        public async Task MoreProvider_CanReused_Async()
        {
            var provider = GetServiceProvider(AddTwoSameTypeProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());
            var currentDbExecutor2 = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);
                //此时工作单元中已经有一个事务，同一类型的执行对象可以复用已有事务
                await uow.TryAddDbExecutorAsync(currentDbExecutor2);

                Assert.True(currentDbExecutor.HasTransaction);
                Assert.True(currentDbExecutor2.HasTransaction);


                //currentDbExecutor2的事务来源是上一个事务
                var transactionObj = currentDbExecutor2.TransactionObject as TestScopeTransactionObject;
                Assert.NotNull(transactionObj);
                Assert.Equal("TestScope", transactionObj.Source);

                await uow.SaveChangesAsync();
            }

            //但是会被添加到工作单元，最后于工作单元一同释放
            Assert.True(currentDbExecutor.IsDispose);
            Assert.True(currentDbExecutor2.IsDispose);
        }

        [Fact]
        public void SuppressOption_ExecutorBeEntrusted_AndNotOpenTransaction()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create(UnitOfWorkScope.Suppress))
            {
                uow.TryAddDbExecutor(currentDbExecutor);

                //Provider接受了当前Executor,但是由于Suppress配置，所以不会为该对象开启事务
                Assert.False(currentDbExecutor.HasTransaction);

                uow.SaveChanges();
            }

            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public async Task SuppressOption_ExecutorBeEntrusted_AndNotOpenTransaction_Async()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create(UnitOfWorkScope.Suppress))
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);

                //Provider接受了当前Executor,但是由于Suppress配置，所以不会为该对象开启事务
                Assert.False(currentDbExecutor.HasTransaction);

                await uow.SaveChangesAsync();
            }

            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public void ChildUow_TransactionOperationWillDelayedtoParent()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                using (var uow2 = uowManager.Create())
                {
                    uow2.TryAddDbExecutor(currentDbExecutor);

                    //当前会具有外围的事务
                    Assert.True(currentDbExecutor.HasTransaction);

                    uow2.SaveChanges();
                }

                //虽然在uow2之外，但是由于使用了子uow，所以此时也不会被uow2释放
                Assert.False(currentDbExecutor.IsDispose);

                uow.SaveChanges();
            }

            //uow结束，则uow2包含的操作也将释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public async Task ChildUow_TransactionOperationWillDelayedtoParent_Async()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            using (var uow = uowManager.Create())
            {
                using (var uow2 = uowManager.Create())
                {
                    await uow2.TryAddDbExecutorAsync(currentDbExecutor);

                    //当前会具有外围的事务
                    Assert.True(currentDbExecutor.HasTransaction);

                    await uow2.SaveChangesAsync();
                }

                //虽然在uow2之外，但是由于使用了子uow，所以此时也不会被uow2释放
                Assert.False(currentDbExecutor.IsDispose);

                await uow.SaveChangesAsync();
            }

            //uow结束，则uow2包含的操作也将释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public void UnitOfWorkEvent_WillTrigger()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            string saveChangedInfo = "";
            string rollbackInfo = "";
            string disposeInfo = "";

            var options = new UnitOfWorkOptions();
            options.Events.OnCompleted += s =>
            {
                saveChangedInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnRollbacked += s =>
            {
                rollbackInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnDispose += s =>
            {
                disposeInfo = "over";
                return Task.CompletedTask;
            };

            using (var uow = uowManager.Create(options))
            {
                uow.TryAddDbExecutor(currentDbExecutor);

                uow.SaveChanges();

                uow.Rollback();
            }

            Assert.Equal("over", saveChangedInfo);
            Assert.Equal("over", rollbackInfo);
            Assert.Equal("over", disposeInfo);

            //uow结束，则uow2包含的操作也将释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public async Task UnitOfWorkEvent_WillTrigger_Async()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            string saveChangedInfo = "";
            string rollbackInfo = "";
            string disposeInfo = "";

            var options = new UnitOfWorkOptions();
            options.Events.OnCompleted += s =>
            {
                saveChangedInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnRollbacked += s =>
            {
                rollbackInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnDispose += s =>
            {
                disposeInfo = "over";
                return Task.CompletedTask;
            };

            using (var uow = uowManager.Create(options))
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);

                await uow.SaveChangesAsync();

                await uow.RollbackAsync();
            }

            Assert.Equal("over", saveChangedInfo);
            Assert.Equal("over", rollbackInfo);
            Assert.Equal("over", disposeInfo);

            //uow结束，则uow2包含的操作也将释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public void ChildUnitOfWorkEvent_WillTrigger()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            string saveChangedInfo = "";
            string rollbackInfo = "";
            string disposeInfo = "";

            var options = new UnitOfWorkOptions();
            options.Events.OnCompleted += s =>
            {
                saveChangedInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnRollbacked += s =>
            {
                rollbackInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnDispose += s =>
            {
                disposeInfo = "over";
                return Task.CompletedTask;
            };

            using (var uow = uowManager.Create())
            {
                using (var uow2 = uowManager.Create(options))
                {
                    uow2.TryAddDbExecutor(currentDbExecutor);

                    uow2.SaveChanges();

                    uow2.Rollback();
                }
            }

            Assert.Equal("over", saveChangedInfo);
            Assert.Equal("over", rollbackInfo);
            Assert.Equal("over", disposeInfo);

            //uow结束，则uow2包含的操作也将释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public async Task ChildUnitOfWorkEvent_WillTrigger_Async()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());

            string saveChangedInfo = "";
            string rollbackInfo = "";
            string disposeInfo = "";

            var options = new UnitOfWorkOptions();
            options.Events.OnCompleted += s =>
            {
                saveChangedInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnRollbacked += s =>
            {
                rollbackInfo = "over";
                return Task.CompletedTask;
            };
            options.Events.OnDispose += s =>
            {
                disposeInfo = "over";
                return Task.CompletedTask;
            };

            using (var uow = uowManager.Create())
            {
                using (var uow2 = uowManager.Create(options))
                {
                    await uow2.TryAddDbExecutorAsync(currentDbExecutor);

                    await uow2.SaveChangesAsync();

                    await uow2.RollbackAsync();
                }
            }

            Assert.Equal("over", saveChangedInfo);
            Assert.Equal("over", rollbackInfo);
            Assert.Equal("over", disposeInfo);

            //uow结束，则uow2包含的操作也将释放
            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public void AlreadyHasTransactionExecutor_ShouldNotCreateNewTrsansation()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());
            //在托管给uow之前就已经具有了事务
            var transaScope = new TransactionScope();
            currentDbExecutor.UseTransaction(new TestScopeTransactionObject(transaScope));

            using (var uow = uowManager.Create())
            {
                uow.TryAddDbExecutor(currentDbExecutor);

                uow.SaveChanges();
            }

            transaScope.Dispose();//由于是外界的事务，所以uow不会托管该事务，需要手动释放

            Assert.True(currentDbExecutor.IsDispose);
        }

        [Fact]
        public async Task AlreadyHasTransactionExecutor_ShouldNotCreateNewTrsansation_Async()
        {
            var provider = GetServiceProvider(AddScopeTransactionProvider);

            var uowManager = provider.GetService<IUnitOfWorkManager>();
            var currentDbExecutor = new ScopeDbExecutor(new FakeDbContext());
            //在托管给uow之前就已经具有了事务
            var transaScope = new TransactionScope();
            currentDbExecutor.UseTransaction(new TestScopeTransactionObject(transaScope));

            using (var uow = uowManager.Create())
            {
                await uow.TryAddDbExecutorAsync(currentDbExecutor);

                await uow.SaveChangesAsync();
            }

            transaScope.Dispose();//由于是外界的事务，所以uow不会托管该事务，需要手动释放

            Assert.True(currentDbExecutor.IsDispose);
        }

        private Action<IServiceCollection> AddScopeTransactionProvider = s
            => s.AddTransient<ITransactionProvider, TestScopeTransactionProvider>();

        private void AddTwoDifferentTypeProvider(IServiceCollection services)
        {
            services.AddTransient<ITransactionProvider, TestScopeTransactionProvider>();
            services.AddTransient<ITransactionProvider, TestDemoTransactionProvider>();
        }

        private void AddTwoSameTypeProvider(IServiceCollection services)
        {
            services.AddTransient<ITransactionProvider, TestScopeTransactionProvider>();
            services.AddTransient<ITransactionProvider, LowLevelScopeTransactionProvider>();
        }
    }
}
