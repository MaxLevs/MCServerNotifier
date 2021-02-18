using System.Net;
using System.Net.Sockets;

namespace MCServerNotifier
{
    public class Server
    {
        public IPAddress Host { get; }
        public int? QueryPort { get; }
        public int? RConPort { get; }

        public Server(string host, int? queryPort, int? rConPort)
        {
            Host = Dns.GetHostAddresses(host)[0];
            QueryPort = queryPort;
            RConPort = rConPort;

            if (QueryPort == null && RConPort == null)
            {
                throw new IncorrectServerEntryPoint(Host);
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