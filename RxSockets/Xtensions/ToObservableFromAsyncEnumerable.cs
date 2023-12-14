using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace RxSockets;

public static partial class Xtensions
{
    /// <summary>
    /// Converts an async enumerable sequence into an observable sequence.
    /// </summary>
    public static IObservable<T> ToObservableFromAsyncEnumerable<T>(this IAsyncEnumerable<T> source) =>
        ToObservableFromAsyncEnumerable(source, Scheduler.Default);

    public static IObservable<T> ToObservableFromAsyncEnumerable<T>(this IAsyncEnumerable<T> source, IScheduler scheduler)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));

        return Observable.Create<T>(observer =>
        {
            return scheduler.ScheduleAsync(source, async (sch, src, ct) =>
            {
                try
                {
                    await foreach (T item in src.WithCancellation(ct).ConfigureAwait(false))
                    {
                        observer.OnNext(item);
                        if (ct.IsCancellationRequested)
                            return;
                    }
                    observer.OnCompleted();
                }
#pragma warning disable CA1031
                catch (Exception e)
#pragma warning restore CA1031
                {
                    if (ct.IsCancellationRequested)
                        return;
                    observer.OnError(e);
                }
            });
        });
    }
}
