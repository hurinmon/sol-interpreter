using System.Diagnostics;

namespace Interpreter
{
    public struct OneTimeStopwatch : IDisposable
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
        private readonly string _itemname;
        private readonly long _startTimestamp;

        public OneTimeStopwatch(string itemname)
        {
            _itemname = itemname;
            _startTimestamp = Stopwatch.GetTimestamp();
        }
        public void Dispose()
        {
            var duration = (Stopwatch.GetTimestamp() - _startTimestamp) * TimestampToTicks / TimeSpan.TicksPerMillisecond;
            Console.WriteLine($"{_itemname}: {duration}ms");
        }
    }
}
