using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RxSockets
{
    public interface IAsyncDisconnectable
    {
        Task DisconnectAsync();
    }

    public static class AddDisconnectableToEx
    {
        public static T AddDisconnectableTo<T>(this T source, IList<IAsyncDisconnectable> list) where T : IAsyncDisconnectable
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            list.Add(source);
            return source;
        }
    }
}
