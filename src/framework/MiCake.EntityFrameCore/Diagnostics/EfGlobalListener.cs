using System;
using System.Diagnostics;


namespace MiCake.EntityFrameworkCore.Diagnostics
{
    /// <summary>
    /// So far, efcore(version:3.1) has not implemented the interceptor of saveChanges. 
    /// Therefore, the method of subscribing diagnostic events is adopted
    /// https://github.com/dotnet/efcore/issues/12024
    /// </summary>

    //[Cancel:See Azure Board #ISSUE 12]
    [Obsolete]
    internal class EfGlobalListener : IObserver<DiagnosticListener>
    {
        ////need ServiceProvider to reslove services
        //private IServiceProvider _serviceProvider;

        //private SaveChangesInterceptor saveChangesInterceptor;
        //internal SaveChangesInterceptor SaveChangesInterceptor
        //{
        //    get
        //    {
        //        if (saveChangesInterceptor != null)
        //            return saveChangesInterceptor;

        //        saveChangesInterceptor = new SaveChangesInterceptor(_serviceProvider);
        //        return saveChangesInterceptor;
        //    }
        //}

        //public EfGlobalListener(IServiceProvider serviceProvider)
        //{
        //    _serviceProvider = serviceProvider;
        //}

        //public void OnCompleted()
        //{
        //}

        //public void OnError(Exception error)
        //{
        //}

        //public void OnNext(DiagnosticListener listener)
        //{
        //    if (listener.Name == DbLoggerCategory.Name)
        //    {
        //        listener.SubscribeAsync(SaveChangesInterceptor);
        //    }
        //}
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(DiagnosticListener value)
        {
            throw new NotImplementedException();
        }
    }
}
