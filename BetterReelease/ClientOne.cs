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

namespace ReeleaseEx.BetterReelease
{
    /// <summary>
    /// ClientOne.cs의 소스 코드 중 일부는 https://github.com/MineEric64/BetterLiveScreen 프로젝트의 ClientOne.cs 소스 코드 일부를 사용하였습니다.
    /// </summary>
    internal class ClientOne
    {
        public const int PORT_NUMBER = 9988;

        private TcpListener _listener;
        private CommunicationManager _comManager;

        private List<IPEndPoint> _IPEPs = new List<IPEndPoint>();

        public ClientOne()
        {
            _listener = new TcpListener(PORT_NUMBER);
            _comManager = new CommunicationManager(_listener);
            
            _listener.Start();
        }

        public async Task ConnectAsync(string ip)
        {
            IPAddress address;
            if (!IPAddress.TryParse(ip, out address)) address = (await Dns.GetHostAddressesAsync(ip))[0];

            var IPEP = new IPEndPoint(address, PORT_NUMBER);

            _IPEPs.Add(IPEP);
            _comManager.PeerConnected += _comManager_PeerConnected;
            await _comManager.ConnectAsync(IPEP);
        }

        private async void _comManager_PeerConnected(object sender, PeerEventArgs e)
        {
            var sr = new PascalStreamReader(e.Peer.Stream);

            byte[] buffer;
            ReceiveInfo info = ReceiveInfo.Empty;

            string fileName = string.Empty;
            byte[] fileData = new byte[0];

            while (true)
            {
                buffer = await sr.ReadBytesAsync();
                info = MessagePackSerializer.Deserialize<ReceiveInfo>(buffer);

                if (info.Step == 1) //File Name
                {
                    fileName = Encoding.UTF8.GetString(info.Buffer);
                }
                else if (info.Step == 2) // File Data
                {
                    fileData = info.Buffer;

                    File.WriteAllBytes(Path.Combine(Path.GetTempPath(), fileName), fileData);
                    //나중에 하자 지금은 너무 힘듦 ㅅㄱ
                    //기능 개발 : 파일 압축 해제하고 프로그램 실행 파일 자동 파악 및 자동 실행
                }

                Thread.Sleep(10);
            }
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

            File.Delete(path);
        }
    }
}
