using System;
using MCQueryLib.State;

namespace MCServerNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var statusWatcher = new StatusWatcher("ML_VDS", "140.82.11.11", 25565);

            statusWatcher.OnServerOnline += (sender, eventArgs) =>
            {
                if (sender == null)
                    return;

                var statusWatcher = (StatusWatcher) sender;
                Console.WriteLine($"[{statusWatcher.ServerName}] Server is online");
            };
            
            statusWatcher.OnServerOffline += (sender, eventArgs) =>
            {
                if (sender == null)
                    return;

                var statusWatcher = (StatusWatcher) sender;
                Console.WriteLine($"[{statusWatcher.ServerName}] Server is offline");
            };
            
            statusWatcher.OnFullStatusUpdated += (sender, eventArgs) =>
            {
                var serverStateEventArgs = (ServerStateEventArgs) eventArgs;
                var serverFullState = (ServerFullState) serverStateEventArgs.ServerState;
                Console.WriteLine($"[{serverStateEventArgs.ServerName}] State has updated: ({serverFullState.PlayerCount} out of {serverFullState.MaxPlayers}) [{string.Join(", ", serverFullState.PlayerList)}]");
            };
            
            statusWatcher.Watch();

            while (true)
            {
                Console.Write("> ");
                Console.ReadLine();
            }
        }
    }
}