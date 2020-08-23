using System.Threading;

namespace Client.Models
{
    internal class Interval
    {
        private long _count;

        public Interval(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public int Min { get; }
        public int Max { get; }

        public long Count => _count;

        public void IncrementCount()
        {
            Interlocked.Increment(ref _count);
        }
    }
}
