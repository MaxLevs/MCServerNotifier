using System;
using System.Net;
using System.Net.Sockets;
using MCServerNotifier.State;

namespace MCServerNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var server = new Server("ML_VDS", "140.82.11.11", 25565);

            server.OnServerOnline += (sender, eventArgs) =>
            {
                if (sender == null)
                    return;

                var serverEntity = (Server) sender;
                Console.WriteLine($"[{serverEntity.Name}] Server is online");
            };
            
            server.OnServerOffline += (sender, eventArgs) =>
            {
                if (sender == null)
                    return;

                var serverEntity = (Server) sender;
                Console.WriteLine($"[{serverEntity.Name}] Server is offline");
            };
            
            server.OnFullStatusUpdated += (sender, eventArgs) =>
            {
                var serverStateEventArgs = (ServerStateEventArgs) eventArgs;
                var serverFullState = (ServerFullState) serverStateEventArgs.ServerState;
                Console.WriteLine($"[{serverStateEventArgs.ServerName}] State has updated: ({serverFullState.PlayerCount} out of {serverFullState.MaxPlayers}) [{string.Join(", ", serverFullState.PlayerList)}]");
            };
            
            server.Watch();

            while (true)
            {
                // ignore
            }
        }
    }
}