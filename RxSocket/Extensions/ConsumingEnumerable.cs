using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace RxSocket
{
    public static class ConsumingEnumerableEx
    {
        public static ConsumingEnumerable<T> ToConsumingEnumerable<T>(this IObservable<T> source)
            => new ConsumingEnumerable<T>(source);
    }

    public class ConsumingEnumerable<T> : IEnumerable<T>, IDisposable
    {
        private readonly BlockingCollection<Notification<T>> Items = new BlockingCollection<Notification<T>>();
        private readonly IDisposable Subscription;

        internal ConsumingEnumerable(IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            Subscription = source.Materialize().Subscribe(Items.Add);
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in Items.GetConsumingEnumerable())
            {
                if (item.Kind == NotificationKind.OnNext)
                    yield return item.Value;
                else if (item.Kind == NotificationKind.OnCompleted)
                    yield break;
                else // if (item.Kind == NotificationKind.OnError)
                    throw item.Exception;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            Subscription.Dispose();
            Items.Dispose();
        }
    }
}


