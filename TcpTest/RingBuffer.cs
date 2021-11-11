using System;
using System.Linq;

namespace TcpTest
{
    public class RingBuffer
    {
        private long[] _buffer;
        private int _capacity;
        private int _size;
        private int _index;
        public RingBuffer(int capacity)
        {
            _capacity = capacity;
            _buffer = new long[capacity];
        }

        public long Average()
        {
            return _buffer.Sum() / Math.Max(_size, 1);
        }

        public void Add(long value)
        {
            _buffer[_index] = value;
            Increment(ref _index);
            if (_size != _capacity)
            {
                _size++;
            }
        }

        private void Increment(ref int index)
        {
            if (++index == _capacity)
            {
                index = 0;
            }
        }
    }
}