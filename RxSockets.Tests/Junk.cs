using System;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Reactive.Linq;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using System.Reactive.Disposables;

namespace RxSockets.Tests
{
    public class Junk : TestBase
    {
        public Junk(ITestOutputHelper output) : base(output) { }

        //[Fact]
        public async Task T00_Example()
        {
            int i = 0;

            var observable = Observable.Create<int>(observer =>
            {
                Write("observer");

                observer.OnNext(i++);

                //observer.OnCompleted();
                return Disposable.Empty;
            });

            var n1 = await observable;
            Write(n1.ToString());
            var n2 = await observable;
            Write(n2.ToString());
            ;


        }
    }
}

