using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;

namespace RxSockets.Tests;

public class ToObservableFromAsyncEnumerableTest : TestBase
{
    public ToObservableFromAsyncEnumerableTest(ITestOutputHelper output) : base(output) { }

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

        var observable = GetSource().ToObservableFromAsyncEnumerable(TaskPoolScheduler.Default);
        //var observable = GetSource().ToObservable();

        var subscription = observable.Subscribe(x =>
        {
            Logger.LogInformation(x);
        });

        await Task.Delay(70);
        subscription.Dispose();
    }

}
