using System;
using MCQueryLib.State;

namespace MCServerNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server
            {
                Name = "ML_VDS",
                Host = "140.82.11.11",
                QueryPort = 25565
            };

            if (server.QueryPort.HasValue)
            {
                var statusWatcher = new StatusWatcher(server.Name, server.Host, server.QueryPort.Value);

                statusWatcher.OnServerOnline += (sender, eventArgs) =>
                {
                    if (sender == null)
                        return;

                    var statusWatcher = (StatusWatcher) sender;
                    Console.WriteLine($"[{DateTime.Now}] [{statusWatcher.ServerName}] Server is online");
                };
                
                statusWatcher.OnServerOffline += (sender, eventArgs) =>
                {
                    if (sender == null)
                        return;

                    var statusWatcher = (StatusWatcher) sender;
                    Console.WriteLine($"[{DateTime.Now}] [{statusWatcher.ServerName}] Server is offline");
                };
                
                statusWatcher.OnFullStatusUpdated += (sender, eventArgs) =>
                {
                    var serverStateEventArgs = (ServerStateEventArgs) eventArgs;
                    var serverFullState = (ServerFullState) serverStateEventArgs.ServerState;
                    Console.WriteLine($"[{DateTime.Now}] [{serverStateEventArgs.ServerName}] State has updated: ({serverFullState.PlayerCount} out of {serverFullState.MaxPlayers}) [{string.Join(", ", serverFullState.PlayerList)}]");
                };
                
                statusWatcher.Watch();
            }

            if (server.RConPort.HasValue)
            {
                // create rcon module
            }
            

            while (true)
            {
                // Just chill and flex here...
            }
        }
    }
}