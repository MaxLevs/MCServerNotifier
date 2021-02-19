using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
        
        public void Watch(SendResponseService sendResponseService)
        {
            if (QueryPort != null)
            {
                var port = QueryPort ?? 0;
                UpdateChallengeTokenTimer = new Timer(async obj =>
                {
                    Console.WriteLine("[INFO] [{Name}] Send handshake request");
                    
                    var handshakeRequest = Request.GetHandshakeRequest();
                    byte[] response = await sendResponseService.SendReceiveAsync(handshakeRequest, Host.ToString(), port, 10000);
                    
                    var challengeTokenRaw = Response.ParseHandshake(response);
                    lock (_challengeTokenLock)
                    {
                        SetChallengeToken(challengeTokenRaw);
                    }
                    
                    Console.WriteLine("[INFO] [{Name}] ChallengeToken is set up");
                }, null, 0, 30000);
                
                UpdateServerStatusTimer = new Timer(async obj =>
                {
                    Console.WriteLine("[INFO] [{Name}] Send full status request");
                    
                    var challengeToken = new byte[4];
                    lock (_challengeTokenLock)
                    {
                        Buffer.BlockCopy(_challengeToken, 0, challengeToken, 0, 4);
                    }
                    
                    var fullStatusRequest = Request.GetFullStatusRequest(challengeToken);

                    byte[] responce = await sendResponseService.SendReceiveAsync(fullStatusRequest, Host.ToString(), port, 10000);

                    ServerFullState fullState = Response.ParseFullState(responce);
                    
                    Console.WriteLine($"[INFO] [{Name}] Full status is received");
                    
                    OnFullStatusUpdated?.Invoke(this, new ServerStateEventArgs(Name, fullState));
                    
                }, null, 500, 5000);
            }
        }
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