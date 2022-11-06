using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Open.P2P;
using Open.P2P.Listeners;
using Open.P2P.IO;
using Open.P2P.EventArgs;
using Open.P2P.Streams.Readers;

using Path = System.IO.Path;
using MessagePack;
using Ionic.Zip;
using System.Windows;
using System.Windows.Threading;

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

        private TcpListener _listener;
        private CommunicationManager _comManager;

        private List<IPEndPoint> _IPEPs = new List<IPEndPoint>();

        public Action SyncWhenPeerConnected { get; set; }
        public Action<string> BetterReeleasedWhenWorker { get; set; }

        public ClientOne()
        {
            _listener = new TcpListener(PORT_NUMBER);
            _comManager = new CommunicationManager(_listener);
            _comManager.PeerConnected += _comManager_PeerConnected;
            _comManager.ConnectionClosed += _comManager_ConnectionClosed;
        }

        public async Task ConnectAsync(string ip)
        {
            IPAddress address;
            if (!IPAddress.TryParse(ip, out address)) address = (await Dns.GetHostAddressesAsync(ip))[0];

            var IPEP = new IPEndPoint(address, PORT_NUMBER);

            _IPEPs.Add(IPEP);

            await _comManager.ConnectAsync(IPEP);
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

            IsConnected = false;
            IsStarted = false;

            _listener.Stop();
        }

        private async void _comManager_PeerConnected(object sender, PeerEventArgs e)
        {
            var sr = new PascalStreamReader(e.Peer.Stream);

            byte[] buffer;
            ReceiveInfo info = ReceiveInfo.Empty;

            string fileName = string.Empty;
            byte[] fileData = new byte[0];

            IsConnected = true;
            SyncWhenPeerConnected();

            while (IsConnected) //Background Worker
            {
                buffer = await sr.ReadBytesAsync(); //수신 입장에서 받지를 못함. 나중에 고쳐야할 듯 지금은 시간이 너무 늦기도 했고 멘탈 나감 ㅅㄱ
                MessageBox.Show("OKYA"); //이 메시지가 송수신 둘다 보이지 않음
                info = MessagePackSerializer.Deserialize<ReceiveInfo>(buffer);

                if (info.Step == 1) //File Name
                {
                    fileName = Encoding.UTF8.GetString(info.Buffer);
                    MessageBox.Show(fileName);
                }
                else if (info.Step == 2) //File Data
                {
                    fileData = info.Buffer;
                    string filePath = Path.Combine(Path.GetTempPath(), fileName);
                    string dirPath = Path.Combine(AppContext.BaseDirectory, Path.GetFileNameWithoutExtension(fileName));

                    File.WriteAllBytes(filePath, fileData);

                    using (var zip = ZipFile.Read(filePath))
                    {
                        zip.ExtractAll(dirPath);
                    }

                    BetterReeleasedWhenWorker(dirPath);
                    MessageBox.Show("Better Reeleased!", "ReeleaseEx", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Thread.Sleep(10);
            }
        }

        private void _comManager_ConnectionClosed(object sender, ConnectionEventArgs e)
        {
            _IPEPs.Remove(e.EndPoint);
        }

        public async Task SendAsync(string path)
        {
            string fileName = Path.GetFileName(path);
            byte[] fileData = File.ReadAllBytes(path);

            var info1 = new ReceiveInfo(1, Encoding.UTF8.GetBytes(fileName));
            var info2 = new ReceiveInfo(2, fileData);

            byte[] buffer1 = MessagePackSerializer.Serialize(info1);
            byte[] buffer2 = MessagePackSerializer.Serialize(info2);

            await _comManager.SendAsync(buffer1, 0, buffer1.Length, _IPEPs);
            await _comManager.SendAsync(buffer2, 0, buffer2.Length, _IPEPs);
        }
    }
}
