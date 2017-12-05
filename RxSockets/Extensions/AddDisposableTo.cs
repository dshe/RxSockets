using System;
using System.Reactive.Disposables;

namespace RxSockets
{
    public static class AddDisposableToEx
    {
        public static T AddDisposableTo<T>(this T disposable, CompositeDisposable composite) where T : IDisposable
        {
            composite.Add(disposable);
            return disposable;
        }
    }
}
