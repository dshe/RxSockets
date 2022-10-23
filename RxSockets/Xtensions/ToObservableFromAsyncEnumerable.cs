using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
namespace RxSockets;

public static partial class Xtensions
{
    /// <summary>
    /// Converts an async enumerable sequence into an observable sequence.
    /// </summary>
    public static IObservable<T> ToObservableFromAsyncEnumerable<T>(this IAsyncEnumerable<T> source, IScheduler? scheduler = null)
    {
        ArgumentNullException.ThrowIfNull(source);

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
#pragma warning disable CA1031
                catch (Exception e)
#pragma warning restore CA1031
                {
                    if (!ct.IsCancellationRequested)
                        observer.OnError(e);
                }
            });
        });
    }
}
