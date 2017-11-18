using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace RxSocket.Tests.Utility
{
    public static class AddDisposableToEx
    {
        public static void AddDisposableTo(this IDisposable disposable, CompositeDisposable composite)
            => composite.Add(disposable);
    }
}
