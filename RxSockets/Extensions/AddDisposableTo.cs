using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

#nullable enable

namespace RxSockets
{
    public static class AddDisposableToEx
    {
        public static T AddDisposableTo<T>(this T disposable, CompositeDisposable composite) where T : IDisposable
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));
            if (composite == null)
                throw new ArgumentNullException(nameof(composite));

            composite.Add(disposable);
            return disposable;
        }

        public static CompositeDisposable Add(this CompositeDisposable composite, IEnumerable<IDisposable> disposables) 
        {
            if (composite == null)
                throw new ArgumentNullException(nameof(composite));
            if (disposables == null)
                throw new ArgumentNullException(nameof(disposables));

            foreach (var disposable in disposables)
                composite.Add(disposable);
            return composite;
        }

    }
}
