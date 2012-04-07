using System;
using System.Diagnostics;

namespace ReflectionUtils
{
    public class Profiler : IDisposable
    {
        private readonly Action<string> _writer;
        private readonly Stopwatch _stopwatch;

        public Profiler(string message, Action<string> writer)
        {
            _writer = writer;
            writer(message);
            _stopwatch = Stopwatch.StartNew();
        }

        public Profiler(Action<string> writer)
        {
            _writer = writer;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _writer(_stopwatch.Elapsed.ToString());
        }
    }
}