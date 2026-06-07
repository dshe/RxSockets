using System.Reactive.Concurrency;
namespace RxSockets.Tests;

public class SomeTest(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task Test1()
    {
        Write(Environment.CurrentManagedThreadId + ": start " + Thread.CurrentThread.Name);

        IObservable<string> observable = Observable.Create<string>(observer =>
        {
            return NewThreadScheduler.Default.ScheduleLongRunning((ct) =>
            {
                //ct.IsDisposed
                //ct.
                Write(Environment.CurrentManagedThreadId + ": " + Thread.CurrentThread.Name);
                //Thread.CurrentThread.Name = "Producer";
                //Write("Producing");
                Write(Environment.CurrentManagedThreadId + ": producing");
                observer.OnNext(OnNext("a"));
                observer.OnNext(OnNext("b"));
                observer.OnNext(OnNext("c"));
                observer.OnCompleted();
            });
            //return Disposable.Empty;

        });

        observable.ObserveOn(NewThreadScheduler.Default.BackgroundThread("SubscriberThread")).Subscribe(msg =>
        //observable.SubscribeOn(NewThreadScheduler.Default.BackgroundThread("SubscriberThread")).Subscribe(msg =>
        //observable.Subscribe(msg =>
        {
            Write(Environment.CurrentManagedThreadId + ": " + msg + " " + Thread.CurrentThread.Name);
        });

        await Task.Delay(1000, TestContext.Current.CancellationToken);
    }

    private string OnNext(string str)
    {
        Write(Environment.CurrentManagedThreadId + ": observing " + str);
        return str;
    }

}
