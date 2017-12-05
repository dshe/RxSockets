using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RxSockets
{
    public sealed class Limiter
    {
        private readonly List<long> ring = new List<long>();
        private readonly int rate;
        private int index;

        public Limiter(int rate = 0)
        {
            if (rate <= 0)
                return;
            this.rate = rate;
            ring.AddRange(Enumerable.Range(1, rate).Select(i => 0L));
        }

        public void Limit(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (ring)
            {
                if (rate > 0)
                {
                    index = (index == rate - 1) ? 0 : index + 1;
                    var wait = 1 + (ring[index] - Stopwatch.GetTimestamp())/(double) Stopwatch.Frequency;
                    if (wait > 0)
                    {
                        //Debug.WriteLine("{0} delay: {1:N0}", index, wait / Stopwatch.Frequency * 1000);
                        Task.Delay(TimeSpan.FromSeconds(wait)).GetAwaiter().GetResult();
                    }
                    ring[index] = Stopwatch.GetTimestamp();
                }
                action();
            }
        }
    }
}
