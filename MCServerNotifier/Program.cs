using System;
using MCServerNotifier.State;

namespace MCServerNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Server("ML_VDS", "140.82.11.11", 25565);
            
            server.OnFullStatusUpdated += (sender, eventArgs) =>
            {
                var serverStateEventArgs = (ServerStateEventArgs) eventArgs;
                var serverFullState = (ServerFullState) serverStateEventArgs.ServerState;
                Console.WriteLine($"[{serverStateEventArgs.ServerName}] State has updated: {string.Join(", ", serverFullState.PlayerList)}");
            };
            
            server.Watch();

            while (true)
            {
                // ignore
            }
        }
    }
}