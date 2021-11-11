using System;
using System.Diagnostics;
using System.Threading;

namespace TcpTest
{
    public class BandwidthMonitor
    {
        public long Bytes;
        
        private readonly RingBuffer _ringBuffer = new(15);
        private readonly Timer _timer;

        public BandwidthMonitor()
        {
            _timer = new Timer(Timer_ResetBytesRead);
        }

        private void Timer_ResetBytesRead(object? state)
        {
            _ringBuffer.Add(Bytes);
            Bytes = 0;
        }
        
        public void Start()
        {
            Bytes = 0;
            _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        public void Stop()
        {
            _timer.Change(TimeSpan.MaxValue, TimeSpan.MaxValue);
        }

        //returns the 15 second simple moving average speed in Megabits/second
        public double GetAverageSpeed()
        {
            return _ringBuffer.Average() / 125000d;
        }

        public void AddBytes(int bytes)
        {
            Bytes += bytes;
        }
    }
}