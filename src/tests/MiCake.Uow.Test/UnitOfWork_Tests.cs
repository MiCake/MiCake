using MiCake.Uow.Test.Fakes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Transactions;
using Xunit;

namespace MiCake.Uow.Test
{
    public class UnitOfWork_Tests : UnitOfWorkTestBase
    {
        public UnitOfWork_Tests()
        {
        }

        [Fact]
        public void UnitOfWork_OnlyOneProvider_CanCreate()
        {
            var provider = this.GetServiceProvider(AddScopeTransactionProvider);

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

            Assert.True(currentDbExecutor.IsDispose);
            Assert.Null(Transaction.Current);
        }

        [Fact]
        public async Task UnitOfWork_OnlyOneProvider_CanCreate_Async()
        {
            var provider = this.GetServiceProvider(AddScopeTransactionProvider);

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
            var provider = this.GetServiceProvider();

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
        public void UnitOfWork_NoAnyProvider_DbContextManualDispose()
        {
            //没有事务支持程序
            var provider = this.GetServiceProvider();
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
        public void MoreProvider_UseSuitableProvider()
        {
            var provider = this.GetServiceProvider(AddTwoDifferentTypeProvider);

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
        public void MoreSuitableProvider_UseOrderLowest()
        {
            var provider = this.GetServiceProvider(AddTwoSameTypeProvider);

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
        public void MoreProvider_ButNoSuitable_ShouldNoTransaction()
        {
            var provider = this.GetServiceProvider(AddTwoSameTypeProvider);

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
        public void MoreProvider_CannotReused()
        {
            var provider = this.GetServiceProvider(AddTwoSameTypeProvider);

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
        public void MoreProvider_CanReused()
        {
            var provider = this.GetServiceProvider(AddTwoSameTypeProvider);

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
        public void SuppressOption_ExecutorBeEntrusted_AndNotOpenTransaction()
        {
            var provider = this.GetServiceProvider(AddScopeTransactionProvider);

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
        public void ChildUow_DifferentOptionFromParent_UseParentTransactionOption()
        {

        }

        [Fact]
        public void ChildUow_TransactionOperationWillDelayedtoParent()
        {

        }

        [Fact]
        public void UnitOfWorkEvent_WillTrigger()
        {

        }

        [Fact]
        public void AlreadyHasTransactionExecutor_ShouldNotCreateNewTrsansation()
        {

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
