using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(new Program().Server);
            Task.Run(new Program().Client);
            Console.Read();
        }

        private int _serverPort = 6000;
        private int _clientPort = 6001;
        private readonly Stopwatch _stopwatch = new();
        private Stopwatch _hundredStopwatch = new();

        #region Server
        private readonly Queue<byte[]> _serverTxPackets = new();
        private readonly AutoResetEvent _serverTxPacketsReady = new(false);
        private void Server()
        {
            var listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, _serverPort));
            listener.Start();
            var tcpClient = listener.AcceptTcpClient();
            Task.Run(() => ReadThread(tcpClient, Server_HandlePacket));
            Task.Run(() => WriteThread(tcpClient, _serverTxPackets, _serverTxPacketsReady));
            while (true)
                Thread.Sleep(100);
        }

        private void Server_HandlePacket(List<byte> packet)
        {
            switch (packet[4])
            {
                case 0://request
                    var req = new RequestPacket(packet.ToArray());
                    var res = ResponsePacket.CreateRandom(req);
                    Write(res);
                    break;
            }
        }

        private void Write(ResponsePacket res)
        {
            lock(_serverTxPacketsReady)
                _serverTxPackets.Enqueue(res.GetBytes());
            _serverTxPacketsReady.Set();
        }
        #endregion

        #region Client

        private readonly Queue<byte[]> _clientTxPackets = new();
        private readonly AutoResetEvent _clientTxPacketsReady = new(false);
        private void Client()
        {
            var tcpClient = new TcpClient(new IPEndPoint(IPAddress.Loopback, _clientPort));
            tcpClient.Connect(new IPEndPoint(IPAddress.Loopback, _serverPort));
            Task.Run(() => ReadThread(tcpClient, Client_HandlePacket));
            Task.Run(() => WriteThread(tcpClient, _clientTxPackets, _clientTxPacketsReady));
            _stopwatch.Start();
            _hundredStopwatch.Start();
            Write(new RequestPacket(0, 0, RequestPacket.DefaultRequestLength));
            while (true)
                Thread.Sleep(100);
        }
        
        private void Client_HandlePacket(List<byte> packet)
        {
            switch (packet[4])
            {
                case 1://response
                    var res = new ResponsePacket(packet.ToArray());
                    if (res.Begin + res.Data.Length < 1048576)
                    {
                        var req = new RequestPacket(res.Index, res.Begin + res.Data.Length, res.Data.Length);
                        Write(req);
                    }
                    else
                    {
                        Debug.WriteLine($"{res.Index} {_stopwatch.ElapsedMilliseconds}ms");
                        if ((res.Index+1) % 100 == 0)
                        {
                            Debug.WriteLine($"100th {res.Index+1} = {_hundredStopwatch.ElapsedMilliseconds}ms");
                            _hundredStopwatch.Restart();
                        }
                        _stopwatch.Restart();
                        var req = new RequestPacket(res.Index + 1, 0, res.Data.Length);
                        Write(req);
                    }
                    break;
            }
        }

        private void Write(RequestPacket req)
        {
            lock(_clientTxPackets)
                _clientTxPackets.Enqueue(req.GetBytes());
            _clientTxPacketsReady.Set();
        }
        #endregion

        #region Common
        private void ReadThread(TcpClient client, Action<List<byte>> handleAction)
        {
            var stream = client.GetStream();
            var buffer = new List<byte>();
            var byteBuffer = new byte[4 * 1024];
            while (client.Connected)
            {
                if (stream.DataAvailable)
                {
                    var bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
                    buffer.AddRange(byteBuffer[..bytesRead]);
                }
                if (!client.Connected) return;
                
                while (stream.DataAvailable && buffer.Count >= 4)
                {
                    var originalPacketSize = BitConverter.ToInt32(buffer.ToArray(), 0);
                    var remainingSize = originalPacketSize - buffer.Count;
                    if (remainingSize > 0)
                    {
                        byteBuffer = new byte[remainingSize];
                        var bytesRead = stream.Read(byteBuffer, 0, byteBuffer.Length);
                        buffer.AddRange(byteBuffer[..bytesRead]);
                    }
                    else
                    {
                        break;
                    }
                }

                while (true)
                {
                    int packetLength = 0;
                    if (buffer.Count >= 4)
                        packetLength = BitConverter.ToInt32(buffer.ToArray(), 0);

                    if (packetLength + 4 > buffer.Count || buffer.Count < 5)
                    {
                        break;
                    }

                    List<byte> packet = new List<byte>(buffer.GetRange(0, packetLength + 4));
                    buffer.RemoveRange(0, packet.Count);
                    Task.Run(() => handleAction.Invoke(packet));
                }
            }
        }

        private void WriteThread(TcpClient client, Queue<byte[]> queue, AutoResetEvent readyEvent)
        {
            while (true)
            {
                readyEvent.WaitOne();
                while (queue.Count > 0)
                {
                    byte[] packet;
                    lock (queue)
                        packet = queue.Dequeue();
                    client.GetStream().Write(packet);
                    client.GetStream().Flush();
                }
            }
        }
        #endregion
    }
}
