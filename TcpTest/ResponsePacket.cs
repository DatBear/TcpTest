using System;
using System.Collections.Generic;

namespace TcpTest
{
    public class ResponsePacket
    {
        private static readonly Random R = new();
        private static readonly byte[] RandomBytes;
        static ResponsePacket()
        {
            RandomBytes = new byte[1048576];
            R.NextBytes(RandomBytes);
        }


        public int Length => 9 + Data.Length;
        public byte Type => 1;
        public int Index { get; }
        public int Begin { get; }
        public byte[] Data { get; }

        

    public ResponsePacket(int index, int begin, byte[] data)
        {
            Index = index;
            Begin = begin;
            Data = data;
        }

        public ResponsePacket(byte[] packet)
        {
            Index = BitConverter.ToInt32(packet, 5);
            Begin = BitConverter.ToInt32(packet, 9);
            Data = new byte[packet.Length - 13];
            Buffer.BlockCopy(packet, 13, Data, 0, packet.Length-13);
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Length));
            bytes.Add(Type);
            bytes.AddRange(BitConverter.GetBytes(Index));
            bytes.AddRange(BitConverter.GetBytes(Begin));
            bytes.AddRange(Data);
            return bytes.ToArray();
        }

        public static ResponsePacket CreateRandom(RequestPacket req)
        {
            var dataStart = R.Next(0, 1048576-req.RequestedLength-1);
            byte[] data = RandomBytes[dataStart..(dataStart + req.RequestedLength)];
            return new ResponsePacket(req.Index, req.Begin, data);
        }

        public static ResponsePacket CreateEmpty(RequestPacket req)
        {
            return new ResponsePacket(req.Index, req.Begin, new byte[req.RequestedLength]);
        }
    }
}