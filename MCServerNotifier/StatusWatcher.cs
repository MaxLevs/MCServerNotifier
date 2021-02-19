using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MCQueryLib;
using MCQueryLib.Packages;
using MCQueryLib.State;
using UdpExtension;

namespace MCServerNotifier
{
    public class StatusWatcher
    {
        public string ServerName { get; set; }

        private readonly McQuery _mcQuery;
        public IPAddress Host => _mcQuery.Host;
        public int Port => _mcQuery.Port;
        
        public bool IsOnline
        {
            get => _mcQuery.IsOnline;
            private set
            {
                if (_mcQuery.IsOnline == value) return;
                
                _mcQuery.IsOnline= value;
                    
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

        private Timer UpdateChallengeTokenTimer { get; set; }
        private Timer UpdateServerStatusTimer { get; set; }
        
        public StatusWatcher(string serverName, string host, int queryPort)
        {
            ServerName = serverName;
            _mcQuery = new McQuery(Dns.GetHostAddresses(host)[0], queryPort);
            _mcQuery.InitSocket();
        }

        #region Retry Parameters
        
        private object _retryCounterLock = new();
        private int RetryCounter = 0;
        public static int RetryMaxCount = 5;
        //todo: change to Static realisation which changes this parameter into all instances of McQuery
        public int ResponseWaitIntervalSecond
        {
            get => _mcQuery.ResponseWaitIntervalSecond;
            set => _mcQuery.ResponseWaitIntervalSecond = value;
        }
        public static int GettingChallengeTokenInterval = 30000;
        public static int GettingStatusInterval = 5000;
        
        #endregion
        
        public async void Watch()
        {
            UpdateChallengeTokenTimer = new Timer(async obj =>
            {
                if (!IsOnline) return;
                
                if(Debug)
                    Console.WriteLine($"[INFO] [{ServerName}] Send handshake request");

                try
                {
                    var challengeToken = await _mcQuery.GetHandshake();
                    
                    IsOnline = true;
                    lock (_retryCounterLock)
                    {
                        RetryCounter = 0;
                    }
                    
                    if(Debug)
                        Console.WriteLine($"[INFO] [{ServerName}] ChallengeToken is set up: " + BitConverter.ToString(challengeToken));
                }
                
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is McQueryServerIsOffline || ex is ChallengeTokenIsNullException)
                    {
                        if(Debug)
                            Console.WriteLine($"[WARNING] [{ServerName}] [UpdateChallengeTokenTimer] Server doesn't response. Try to reconnect: {RetryCounter}");
                        
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

                    else
                    {
                        throw;
                    }
                }
                
            }, null, 0, GettingChallengeTokenInterval);
                
            UpdateServerStatusTimer = new Timer(async obj =>
            {
                if (!IsOnline) return;
                
                if(Debug)
                    Console.WriteLine($"[INFO] [{ServerName}] Send full status request");

                try
                {
                    var response = await _mcQuery.GetFullStatus();
                    
                    IsOnline = true;
                    lock (_retryCounterLock)
                    {
                        RetryCounter = 0;
                    }
                    
                    if(Debug)
                        Console.WriteLine($"[INFO] [{ServerName}] Full status is received");
                    
                    OnFullStatusUpdated?.Invoke(this, new ServerStateEventArgs(ServerName, response));
                }
                
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is McQueryServerIsOffline || ex is ChallengeTokenIsNullException)
                    {
                        if(Debug)
                            Console.WriteLine($"[WARNING] [{ServerName}] [UpdateServerStatusTimer] Server doesn't response. Try to reconnect: {RetryCounter}");
                        
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
                    
                    else
                    {
                        throw;
                    }
                }
                
            }, null, 500, GettingStatusInterval);
        }

        public async Task Unwatch()
        {
            await UpdateChallengeTokenTimer.DisposeAsync();
            await UpdateServerStatusTimer.DisposeAsync();
        }

        public async void WaitForServerAlive()
        {
            if(Debug)
                Console.WriteLine($"[WARNING] [{ServerName}] Server is unavailable. Waiting for reconnection...");
            
            IsOnline = false;
            await Unwatch();
            
            _mcQuery.InitSocket();
            
            Timer waitTimer = null;
            waitTimer = new Timer(async obj => {
                try
                {
                    await _mcQuery.GetHandshake();
                    
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
                    if(Debug)
                        Console.WriteLine($"[WARNING] [{ServerName}] [WaitForServerAlive] Server doesn't response. Try to reconnect: {RetryCounter}");
                    
                    lock (_retryCounterLock)
                    {
                        RetryCounter++;
                        if (RetryCounter >= RetryMaxCount)
                        {
                            if(Debug)
                                Console.WriteLine($"[WARNING] [{ServerName}] [WaitForServerAlive] Recreate socket");
                            
                            RetryCounter = 0;
                            _mcQuery.InitSocket();
                        }
                    }
                }
            }, null, 500, 5000);
        }

        
        public event EventHandler OnFullStatusUpdated;
        public event EventHandler OnServerOffline;
        public event EventHandler OnServerOnline;
        
        public bool Debug { get; set; }
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