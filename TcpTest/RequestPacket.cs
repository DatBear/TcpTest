using System;
using System.Collections.Generic;

namespace TcpTest
{
    public class RequestPacket
    {
        public static int DefaultRequestLength => (int)Math.Pow(2, 14);
        public int Length => 13;
        public byte Type => 0;
        public int Index { get; private set; }
        public int Begin { get; private set; }
        public int RequestedLength { get; private set; }


        public RequestPacket(int index, int begin, int requestedLength)
        {
            Index = index;
            Begin = begin;
            RequestedLength = requestedLength;
        }

        public RequestPacket(byte[] packet)
        {
            Index = BitConverter.ToInt32(packet, 5);
            Begin = BitConverter.ToInt32(packet, 9);
            RequestedLength = BitConverter.ToInt32(packet, 13);
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Length));
            bytes.Add(Type);
            bytes.AddRange(BitConverter.GetBytes(Index));
            bytes.AddRange(BitConverter.GetBytes(Begin));
            bytes.AddRange(BitConverter.GetBytes(RequestedLength));
            return bytes.ToArray();
        }
    }
}