using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MCServerNotifier.Packages;
using MCServerNotifier.State;

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

        private UdpClient _statusWatcherClient;
        
        private void SetChallengeToken(byte[] challengeToken)
        {
            Buffer.BlockCopy(challengeToken, 0, _challengeToken, 0, 4);
        }
        
        private Timer UpdateChallengeTokenTimer { get; set; }
        private Timer UpdateServerStatusTimer { get; set; }
        
        public event EventHandler OnFullStatusUpdated;
        
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
        
        public async void Watch()
        {
            if (QueryPort == null) return;
            var ipEndPoint = IPEndPoint.Parse($"{Host}:{QueryPort.Value}");
            _statusWatcherClient = new UdpClient(ipEndPoint);
            
            UpdateChallengeTokenTimer = new Timer(async obj =>
            {
                Console.WriteLine($"[INFO] [{Name}] Send handshake request");
                    
                Request handshakeRequest = Request.GetHandshakeRequest();
                byte[] response = null;
                try
                {
                    response = await SendResponseService.SendReceive(_statusWatcherClient, handshakeRequest.Data, ReceiveAwaitIntervalSeconds);
                }
                catch (SocketException)
                {
                    WaitForServerAlive(ipEndPoint.Port);
                }
                    
                var challengeTokenRaw = Response.ParseHandshake(response);
                lock (_challengeTokenLock)
                {
                    SetChallengeToken(challengeTokenRaw);
                }
                    
                Console.WriteLine($"[INFO] [{Name}] ChallengeToken is set up");
            }, null, 0, GettingChallengeTokenInterval);
                
            UpdateServerStatusTimer = new Timer(async obj =>
            {
                Console.WriteLine($"[INFO] [{Name}] Send full status request");
                    
                var challengeToken = new byte[4];
                lock (_challengeTokenLock)
                {
                    Buffer.BlockCopy(_challengeToken, 0, challengeToken, 0, 4);
                }
                    
                var fullStatusRequest = Request.GetFullStatusRequest(challengeToken);

                byte[] response = null;
                try
                {
                    response = await SendResponseService.SendReceive(_statusWatcherClient, fullStatusRequest.Data, ReceiveAwaitIntervalSeconds);
                }
                
                catch (SocketException)
                {
                    WaitForServerAlive(ipEndPoint.Port);
                }

                ServerFullState fullState = Response.ParseFullState(response);
                    
                Console.WriteLine($"[INFO] [{Name}] Full status is received");
                    
                OnFullStatusUpdated?.Invoke(this, new ServerStateEventArgs(Name, fullState));
                    
            }, null, 500, GettingStatusInterval);
        }

        public async Task Unwatch()
        {
            await UpdateChallengeTokenTimer.DisposeAsync();
            await UpdateServerStatusTimer.DisposeAsync();
            _statusWatcherClient.Dispose();
            _statusWatcherClient = null;
        }

        public async void WaitForServerAlive(int port)
        {
            await Unwatch();
            var ipEndPoint = IPEndPoint.Parse($"{Host}:{port}");
            Timer waitTimer = null;
            waitTimer = new Timer(async obj => {
                try {
                    using (TcpClient tcpClient = new TcpClient()) 
                    {
                        tcpClient.Connect(ipEndPoint);
                        if (waitTimer == null) return;
                        await waitTimer.DisposeAsync();
                        Watch();
                    } 
                }  catch (SocketException) { }
            }, null, 500, 5000);
        }

        public static int ReceiveAwaitIntervalSeconds = 10;
        public static int GettingChallengeTokenInterval = 30000;
        public static int GettingStatusInterval = 5000;
    }

    public class ServerStateEventArgs : EventArgs
    {
        public string ServerName { get; }
        public ServerState ServerState { get; }
        
        public ServerStateEventArgs(string serverName, ServerState serverState)
        {
            ServerName = serverName;
            ServerState = serverState;
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