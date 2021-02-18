using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MCServerNotifier
{
    public class Server
    {
        public string Name { get; set; }
        public IPAddress Host { get; }
        public int? QueryPort { get; }
        public int? RConPort { get; }

        private object _challengeTokenLock = new();
        private byte[] _challengeToken = new byte[4];
        
        public ServerFullState FullState { get; private set; }

        private void SetChallengeToken(byte[] challengeToken)
        {
            Buffer.BlockCopy(challengeToken, 0, _challengeToken, 0, 4);
        }
        
        private Timer UpdateChallengeTokenTimer { get; set; }
        private Timer UpdateServerStatusTimer { get; set; }
        
        public Server(string name, string host, int? queryPort = null, int? rConPort = null)
        {
            Name = name;
            Host = Dns.GetHostAddresses(host)[0];
            QueryPort = queryPort;
            RConPort = rConPort;

            if (QueryPort == null && RConPort == null)
            {
                throw new IncorrectServerEntryPoint(Host);
            }
        }
        
        public void Watch(Service service)
        {
            if (QueryPort != null)
            {
                var port = QueryPort ?? 0;
                UpdateChallengeTokenTimer = new Timer(async obj =>
                {
                    var handshakeRequest = Request.GetHandshakeRequest();
                    byte[] response = await service.SendReceiveAsync(handshakeRequest, Host.ToString(), port, 10000);
                    
                    var challengeTokenRaw = Response.ParseHandshake(response);
                    lock (_challengeTokenLock)
                    {
                        Buffer.BlockCopy(challengeTokenRaw, 0, _challengeToken, 0, 4);
                    }
                }, null, 0, 30000);
                
                UpdateServerStatusTimer = new Timer(async obj =>
                {
                    var challengeToken = new byte[4];
                    lock (_challengeTokenLock)
                    {
                        Buffer.BlockCopy(_challengeToken, 0, challengeToken, 0, 4);
                    }
                    
                    var fullStatusRequest = Request.GetFullStatusRequest(challengeToken);

                    byte[] responce = await service.SendReceiveAsync(fullStatusRequest, Host.ToString(), port, 10000);

                    FullState = Response.ParseFullState(responce);
                }, null, 500, 5000);
            }
        }
    }

    public class IncorrectServerEntryPoint : SocketException
    {
        public IPAddress Host { get; }
        public override string Message { get; }
        
        public IncorrectServerEntryPoint(IPAddress host)
        {
            Host = host;
            Message = "Incorrect entry point for host " + Host + ". RConPort and QueryPort are both null.";
        }
    }
}