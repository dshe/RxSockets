using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace RxSockets
{
    public static partial class ToObservableFromAsyncEnum
    {
        /// <summary>
        /// Converts an async enumerable sequence into an observable sequence.
        /// </summary>
        public static IObservable<T> ToObservableFromAsyncEnumerable<T>(this IAsyncEnumerable<T> source, IScheduler? scheduler = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            scheduler ??= Scheduler.Default;

            return Observable.Create<T>(observer =>
            {
                return scheduler.ScheduleAsync(async (_, ct) =>
                {
                    try
                    {
                        await foreach (T item in source.WithCancellation(ct).ConfigureAwait(false))
                        {
                            observer.OnNext(item);
                            if (ct.IsCancellationRequested)
                                return;
                        }
                        observer.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        if (!ct.IsCancellationRequested)
                            observer.OnError(e);
                    }
                });
            });
        }

        /*
        /// <summary>
        /// Rx\reactive-main\Ix.NET\Source\System.Linq.Async\System\Linq\Operators\ToObservable.cs
        /// </summary>
        public static IObservable<TSource> ToObservable<TSource>(this IAsyncEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return new ToObservableObservable<TSource>(source);
        }

        private sealed class ToObservableObservable<T> : IObservable<T>
        {
            private readonly IAsyncEnumerable<T> _source;
            public ToObservableObservable(IAsyncEnumerable<T> source) => _source = source;

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var ctd = new CancellationTokenDisposable();

                async void Core()
                {
                    await using var e = _source.GetAsyncEnumerator(ctd.Token); // takes cancellation token
                    do
                    {
                        bool hasNext;
                        var value = default(T)!; // note!
                        try
                        {
                            // note that this does not take cancellation token here
                            hasNext = await e.MoveNextAsync().ConfigureAwait(false);
                            if (hasNext)
                                value = e.Current;
                        }
                        catch (Exception ex)
                        {
                            if (!ctd.Token.IsCancellationRequested)
                                observer.OnError(ex);
                            return;
                        }
                        if (!hasNext)
                        {
                            observer.OnCompleted();
                            return;
                        }
                        observer.OnNext(value);
                    }
                    while (!ctd.Token.IsCancellationRequested);
                }

                Core(); // Fire and forget ?!

                return ctd;
            }
        }

        internal sealed class CancellationTokenDisposable : IDisposable
        {
            private readonly CancellationTokenSource _cts = new ();
            public CancellationToken Token => _cts.Token;
            public void Dispose()
            {
                if (!_cts.IsCancellationRequested)
                    _cts.Cancel();
            }
        }
        */
    }
}
