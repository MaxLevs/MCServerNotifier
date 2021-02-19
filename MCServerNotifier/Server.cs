using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MCQueryLib.Packages;
using MCQueryLib.State;
using UdpExtension;

namespace MCServerNotifier
{
    public class Server
    {
        public string Name { get; set; }
        public IPAddress Host { get; }
        public int? QueryPort { get; }
        public int? RConPort { get; }
        
        public bool IsOnline
        {
            get => _isOnline;
            private set
            {
                if (_isOnline == value) return;
                
                _isOnline = value;
                    
                switch (value)
                {
                    case true:
                        OnServerOnline?.Invoke(this, EventArgs.Empty);
                        break;
                        
                    case false:
                        OnServerOffline?.Invoke(this, EventArgs.Empty);
                        break;
                }
            }
        }
        private bool _isOnline = true;

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
        public event EventHandler OnServerOffline;
        public event EventHandler OnServerOnline;
        
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
            _statusWatcherClient = new UdpClient(Host.ToString(), QueryPort.Value);
            
            UpdateChallengeTokenTimer = new Timer(async obj =>
            {
                if (!IsOnline) return;
                Console.WriteLine($"[INFO] [{Name}] Send handshake request");
                    
                Request handshakeRequest = Request.GetHandshakeRequest();
                byte[] response = null;
                try
                {
                    response = await SendResponseService.SendReceive(_statusWatcherClient, handshakeRequest.Data, ReceiveAwaitIntervalSeconds);
                    IsOnline = true;
                    lock (_retryCounterLock)
                    {
                        RetryCounter = 0;
                    }
                }
                catch (SocketException)
                {
                    Console.WriteLine($"[WARNING] [{Name}] [UpdateChallengeTokenTimer] Server doesn't response. Try to reconnect: {RetryCounter}");
                    lock (_retryCounterLock)
                    {
                        RetryCounter++;
                        if (RetryCounter >= RetryMaxCount)
                        {
                            RetryCounter = 0;
                            WaitForServerAlive();
                        }
                    }
                }
                    
                if (response == null) return;
                
                var challengeTokenRaw = Response.ParseHandshake(response);
                lock (_challengeTokenLock)
                {
                    SetChallengeToken(challengeTokenRaw);
                }
                    
                Console.WriteLine($"[INFO] [{Name}] ChallengeToken is set up: " + BitConverter.ToString(challengeTokenRaw));
            }, null, 0, GettingChallengeTokenInterval);
                
            UpdateServerStatusTimer = new Timer(async obj =>
            {
                if (!IsOnline) return;
                
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
                    IsOnline = true;
                    lock (_retryCounterLock)
                    {
                        RetryCounter = 0;
                    }
                }
                
                catch (SocketException)
                {
                    Console.WriteLine($"[WARNING] [{Name}] [UpdateServerStatusTimer] Server doesn't response. Try to reconnect: {RetryCounter}");
                    lock (_retryCounterLock)
                    {
                        RetryCounter++;
                        if (RetryCounter >= RetryMaxCount)
                        {
                            RetryCounter = 0;
                            WaitForServerAlive();
                        }
                    }
                }
                
                if (response == null) return;

                ServerFullState fullState = Response.ParseFullState(response);
                    
                Console.WriteLine($"[INFO] [{Name}] Full status is received");
                    
                OnFullStatusUpdated?.Invoke(this, new ServerStateEventArgs(Name, fullState));
                    
            }, null, 500, GettingStatusInterval);
        }

        public async Task Unwatch()
        {
            await UpdateChallengeTokenTimer.DisposeAsync();
            await UpdateServerStatusTimer.DisposeAsync();
            _statusWatcherClient?.Dispose();
            _statusWatcherClient = null;
        }

        public async void WaitForServerAlive()
        {
            Console.WriteLine($"[WARNING] [{Name}] Server is unavailable. Waiting for reconnection...");
            IsOnline = false;
            await Unwatch();
            _statusWatcherClient = new UdpClient(Host.ToString(), QueryPort.Value);
            Timer waitTimer = null;
            waitTimer = new Timer(async obj => {
                Request handshakeRequest = Request.GetHandshakeRequest();
                byte[] response = null;
                try
                {
                    response = await SendResponseService.SendReceive(_statusWatcherClient, handshakeRequest.Data, ReceiveAwaitIntervalSeconds);
                    IsOnline = true;
                    Watch();
                    lock (_retryCounterLock)
                    {
                        RetryCounter = 0;
                    }
                    waitTimer.Dispose();
                }
                catch (SocketException)
                {
                    Console.WriteLine($"[WARNING] [{Name}] [WaitForServerAlive] Server doesn't response. Try to reconnect: {RetryCounter}");
                    lock (_retryCounterLock)
                    {
                        RetryCounter++;
                        if (RetryCounter >= RetryMaxCount)
                        {
                            Console.WriteLine($"[WARNING] [{Name}] [WaitForServerAlive] Recreate socket");
                            RetryCounter = 0;
                            _statusWatcherClient.Dispose();
                            _statusWatcherClient = new UdpClient(Host.ToString(), QueryPort.Value);
                        }
                    }
                }
            }, null, 500, 5000);
        }

        private object _retryCounterLock = new();
        private int RetryCounter = 0;
        public static int RetryMaxCount = 5;
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