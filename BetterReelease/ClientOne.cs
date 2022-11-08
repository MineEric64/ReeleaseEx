using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using LiteNetLib;
using LiteNetLib.Utils;

using MessagePack;

using Ionic.Zip;

using Path = System.IO.Path;
using System.Diagnostics;

namespace ReeleaseEx.BetterReelease
{
    /// <summary>
    /// ClientOne.cs의 소스 코드 중 일부는 https://github.com/MineEric64/BetterLiveScreen 프로젝트의 ClientOne.cs 소스 코드 일부를 사용하였습니다.
    /// </summary>
    public class ClientOne
    {
        private int _port = 9988;
        public int PORT_NUMBER => _port;

        public bool IsStarted { get; private set; } = false;
        public bool IsConnected { get; private set; } = false;

        EventBasedNetListener _listener;
        NetManager _client;

        private List<NetPeer> _IPEPs = new List<NetPeer>();

        public Action SyncWhenPeerConnected { get; set; }
        public Action SyncWhenConnectionClosed { get; set; }
        public Action<string> BetterReeleasedWhenWorker { get; set; }

        string fileName = string.Empty;
        List<byte> fileDataList = new List<byte>();

        public ClientOne()
        {
            _listener = new EventBasedNetListener();
            _listener.PeerConnectedEvent += _listener_PeerConnectedEvent;
            _listener.NetworkReceiveEvent += _listener_NetworkReceiveEvent;
            _listener.PeerDisconnectedEvent += _listener_PeerDisconnectedEvent;
            _listener.ConnectionRequestEvent += _listener_ConnectionRequestEvent;

            _client = new NetManager(_listener);
        }

        public static string GetLocalIpAddressAsync()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public async Task ConnectAsync(string ip)
        {
            IPAddress address;
            if (!IPAddress.TryParse(ip, out address)) address = (await Dns.GetHostAddressesAsync(ip))[0];

            var IPEP = new IPEndPoint(address, PORT_NUMBER);

            _client.Connect(IPEP, "reelease_ex");
            IsConnected = true;

            MessageBox.Show("Connected!", "ReeleaseEx", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Start()
        {
            _client.Start(PORT_NUMBER);
            Task.Run(() =>
            {
                while (true)
                {
                    _client.PollEvents();
                    Thread.Sleep(15);
                }
            });
            IsStarted = true;
        }

        public void Stop()
        {
            if (!IsConnected) return;

            _client.Stop();
            IsConnected = false;
            IsStarted = false;
        }

        private void _listener_PeerConnectedEvent(NetPeer peer)
        {
            IsConnected = true;
            _IPEPs.Add(peer);
            SyncWhenPeerConnected();
        }

        private void _listener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            IsConnected = false;
            _IPEPs.Remove(peer);
            SyncWhenConnectionClosed();
        }

        private void _listener_ConnectionRequestEvent(ConnectionRequest request)
        {
            if (_client.ConnectedPeersCount < 4 /* max connections */)
            {
                request.AcceptIfKey("reelease_ex");
            }
            else
            {
                request.Reject();
            }
        }

        private void _listener_NetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(reader.RawData, reader.Position, reader.RawDataSize - reader.Position);
            var info = MessagePackSerializer.Deserialize<ReceiveInfo>(buffer);

            if (info.Step == 1) //File Name
            {
                fileName = Encoding.UTF8.GetString(info.Buffer);
                Debug.WriteLine($"Received File Name to : {fileName}");
            }
            else if (info.Step >= 2) //File Data
            {
                fileDataList.AddRange(info.Buffer);

                if (info.Step == info.MaxStep)
                {
                    string filePath = Path.Combine(Path.GetTempPath(), fileName);
                    string dirPath = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(fileName));

                    if (Directory.Exists(dirPath)) Directory.Delete(dirPath, true);
                    File.WriteAllBytes(filePath, fileDataList.ToArray());

                    using (var zip = ZipFile.Read(filePath))
                    {
                        zip.ExtractAll(dirPath);
                    }

                    BetterReeleasedWhenWorker(dirPath);
                    MessageBox.Show("Better Reeleased!", "ReeleaseEx", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        public void Send(string path)
        {
            string fileName = Path.GetFileName(path);
            byte[] fileData = File.ReadAllBytes(path);

            var info1 = new ReceiveInfo(1, 1, Encoding.UTF8.GetBytes(fileName));
            byte[] buffer1 = MessagePackSerializer.Serialize(info1);

            foreach (var peer in _IPEPs)
            {
                peer.Send(buffer1, DeliveryMethod.ReliableUnordered);
            }

            int read = 0;
            int step = 2;
            int maxStep = (int)Math.Ceiling((double)fileData.Length / 65507) + 1;

            while (read < fileData.Length)
            {
                int bytesRead = read + 65507 < fileData.Length ? 65507 : fileData.Length - read;
                byte[] buffer = new byte[bytesRead];
                ReceiveInfo info2;

                Buffer.BlockCopy(fileData, read, buffer, 0, bytesRead);
                info2 = new ReceiveInfo(step++, maxStep, buffer);
                byte[] buffer2 = MessagePackSerializer.Serialize(info2);

                foreach (var peer in _IPEPs)
                {
                    peer.Send(buffer2, DeliveryMethod.ReliableUnordered);
                }

                read += bytesRead;
            }
        }
    }
}
