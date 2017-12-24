using System;
using System.Collections.Generic;
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

        public static CompositeDisposable Add(this CompositeDisposable composite, IEnumerable<IDisposable> disposables) 
        {
            foreach (var disposable in disposables)
                composite.Add(disposable);
            return composite;
        }

    }
}
