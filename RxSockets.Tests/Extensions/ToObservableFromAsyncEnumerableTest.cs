using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RxSockets.Tests;

public class ToObservableFromAsyncEnumerableTest(ITestOutputHelper output) : TestBase(output)
{
    private static async IAsyncEnumerable<string> GetSource([EnumeratorCancellation] CancellationToken ct = default)
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await Task.Delay(10, ct);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            yield return i.ToString();
        }
        yield break;
    }

    [Fact]
    public async Task Test()
    {
        Logger.LogInformation("start");

        IObservable<string> observable = GetSource().ToObservableFromAsyncEnumerable(TaskPoolScheduler.Default);

        IDisposable subscription = observable.Subscribe(x =>
        {
            Logger.LogInformation(x);
        });

        await Task.Delay(70);
        subscription.Dispose();
    }
}
