using System;
using System.Reactive.Disposables;

namespace RxSocket
{
    public static class AddDisposableToEx
    {
        public static void AddDisposableTo(this IDisposable disposable, CompositeDisposable composite) =>
            composite.Add(disposable);
    }
}
