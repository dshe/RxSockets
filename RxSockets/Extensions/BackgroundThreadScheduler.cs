using System.Reactive.Concurrency;
namespace RxSockets;

public static partial class Extension
{
    // Usage: IObservable<T>.SubscribeOn(NewThreadScheduler.BackgroundThread("SubscriberThread"));
    public static NewThreadScheduler BackgroundThread(this NewThreadScheduler _, string name = "NewBackgroundThread")
    {
        return new NewThreadScheduler(threadStart => new Thread(threadStart)
        {
            Name = name,
            IsBackground = true
        });
    }

}
