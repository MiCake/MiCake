using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace MiCake.Core.Reactive
{
    public static class MiCaekObservableExtensions
    {
        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<Task> onNextAsync) =>
            source
                .Select(number => Observable.FromAsync(onNextAsync))
                .Concat()
                .Subscribe();

        public static IDisposable SubscribeAsync<T>(this IObservable<T> source, IObserverAsync<T> observer) =>
            source
                .Select(number => Observable.FromAsync(async () => { await observer.OnNext(number); }))
                .Concat()
                .Subscribe();

        public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> source, Func<Task> onNextAsync) =>
            source
                .Select(number => Observable.FromAsync(onNextAsync))
                .Merge()
                .Subscribe();

        public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> source, IObserverAsync<T> observer) =>
            source
                .Select(number => Observable.FromAsync(async () => { await observer.OnNext(number); }))
                .Merge()
                .Subscribe();

        public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> source, Func<Task> onNextAsync, int maxConcurrent) =>
            source
                .Select(number => Observable.FromAsync(onNextAsync))
                .Merge(maxConcurrent)
                .Subscribe();

        public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> source, IObserverAsync<T> observer, int maxConcurrent) =>
            source
                .Select(number => Observable.FromAsync(async () => { await observer.OnNext(number); }))
                .Merge(maxConcurrent)
                .Subscribe();
    }
}
