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

using Open.P2P;
using Open.P2P.Listeners;
using Open.P2P.IO;
using Open.P2P.EventArgs;
using Open.P2P.Streams.Readers;

using MessagePack;

using Ionic.Zip;

using Path = System.IO.Path;
using TcpListener = Open.P2P.Listeners.TcpListener;

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

        private UdpListener _listener;
        private CommunicationUdpManager _comManager;

        private List<IPEndPoint> _IPEPs = new List<IPEndPoint>();

        public Action SyncWhenPeerConnected { get; set; }
        public Action SyncWhenConnectionClosed { get; set; }
        public Action<string> BetterReeleasedWhenWorker { get; set; }

        string fileName = string.Empty;
        List<byte> fileDataList = new List<byte>();

        public ClientOne()
        {
            _listener = new UdpListener(PORT_NUMBER);
            _comManager = new CommunicationUdpManager();
            _comManager.PacketReceived += _comManager_PacketReceived;
            //_comManager.PeerConnected += _comManager_PeerConnected;
            _comManager.ConnectionClosed += _comManager_ConnectionClosed;
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

            _IPEPs.Add(IPEP);

            //await _comManager.ConnectAsync(IPEP);
            IsConnected = true;

            MessageBox.Show("Connected!", "ReeleaseEx", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void Start()
        {
            _listener.Start();
            IsStarted = true;
        }

        public void Stop()
        {
            if (!IsConnected) return;

            _listener.Stop();
            IsConnected = false;
            IsStarted = false;
        }

        private void _comManager_PeerConnected(object sender, PeerEventArgs e) //for TCP
        {
            IsConnected = true;
            SyncWhenPeerConnected();

            if (!_IPEPs.Contains(e.Peer.EndPoint)) _IPEPs.Add(e.Peer.EndPoint);

            Task.Run(async () =>
            {
                string fileName = string.Empty;
                byte[] fileData = new byte[0];

                using (var reader = new MessagePackStreamReader(e.Peer.Stream))
                {
                    CancellationToken token = new CancellationToken();

                    while (IsConnected)
                    {
                        ReadOnlySequence<byte>? bufferPacked = await reader.ReadAsync(token);

                        if (bufferPacked != null)
                        {
                            var info = MessagePackSerializer.Deserialize<ReceiveInfo>(bufferPacked.Value);

                            if (info.Step == 1) //File Name
                            {
                                fileName = Encoding.UTF8.GetString(info.Buffer);
                            }
                            else if (info.Step == 2) //File Data
                            {
                                fileData = info.Buffer;
                                string filePath = Path.Combine(Path.GetTempPath(), fileName);
                                string dirPath = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(fileName));

                                if (Directory.Exists(dirPath)) Directory.Delete(dirPath, true);
                                File.WriteAllBytes(filePath, fileData);

                                using (var zip = ZipFile.Read(filePath))
                                {
                                    zip.ExtractAll(dirPath);
                                }

                                BetterReeleasedWhenWorker(dirPath);
                                MessageBox.Show("Better Reeleased!", "ReeleaseEx", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        Thread.Sleep(10);
                    }
                }
            });
        }

        private void _comManager_PacketReceived(object sender, UdpPacketReceivedEventArgs e) //for UDP
        {
            if (!IsConnected)
            {
                IsConnected = true;
                SyncWhenPeerConnected();
                if (!_IPEPs.Contains(e.EndPoint)) _IPEPs.Add(e.EndPoint);
            }

            var info = MessagePackSerializer.Deserialize<ReceiveInfo>(e.Data);

            if (info.Step == 1) //File Name
            {
                fileName = Encoding.UTF8.GetString(info.Buffer);
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

        private void _comManager_ConnectionClosed(object sender, ConnectionEventArgs e)
        {
            _IPEPs.Remove(e.EndPoint);
            SyncWhenConnectionClosed();
        }

        public async Task SendAsync(string path)
        {
            string fileName = Path.GetFileName(path);
            byte[] fileData = File.ReadAllBytes(path);

            var info1 = new ReceiveInfo(1, 1, Encoding.UTF8.GetBytes(fileName));
            byte[] buffer1 = MessagePackSerializer.Serialize(info1);

            await _comManager.SendAsync(buffer1, 0, buffer1.Length, _IPEPs);

            int read = 0;
            int step = 1;
            int maxStep = (int)Math.Ceiling((double)fileData.Length / 65507);

            while (read < fileData.Length)
            {
                int bytesRead = read + 65507 < fileData.Length ? 65507 : fileData.Length - read;
                byte[] buffer = new byte[bytesRead];
                ReceiveInfo info2;

                Buffer.BlockCopy(fileData, read, buffer, 0, read + bytesRead);
                info2 = new ReceiveInfo(step++, maxStep, buffer);

                await _comManager.SendAsync(buffer1, 0, buffer1.Length, _IPEPs);

                read += bytesRead;
            }
        }
    }
}
