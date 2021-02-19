using System;
using System.Linq;
using MCQueryLib.State;
using TerminalNotifierLib;

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
                var notifierOptionsLocker = new object();
                var notifierOptions = new TerminalNotifierOptions
                {
                    Title = $"[{server.Name}] MC Server notification",
                    ContentImage = "Resources/notification_icon.png",
                };

                statusWatcher.OnServerOnline += (sender, eventArgs) =>
                {
                    if (sender == null)
                        return;

                    var watcher = (StatusWatcher) sender;

                    lock (notifierOptionsLocker)
                    {
                        notifierOptions.Message = "Server is [ONLINE]";
                        notifierOptions.Sound = NotificationSound.Ping;
                        TerminalNotifierWrapper.Notify(notifierOptions);
                    }
                    Console.WriteLine($"[{DateTime.Now}] [{watcher.ServerName}] Server is online");
                };
                
                statusWatcher.OnServerOffline += (sender, eventArgs) =>
                {
                    if (sender == null)
                        return;

                    var watcher = (StatusWatcher) sender;
                    
                    lock (notifierOptionsLocker)
                    {
                        notifierOptions.Message = "Server is [OFFLINE]";
                        notifierOptions.Sound = NotificationSound.Basso;
                        TerminalNotifierWrapper.Notify(notifierOptions);
                    }
                    Console.WriteLine($"[{DateTime.Now}] [{watcher.ServerName}] Server is offline");
                };

                string[] storedPlayerList = null;
                statusWatcher.OnFullStatusUpdated += (sender, eventArgs) =>
                {
                    var serverStateEventArgs = (ServerStateEventArgs) eventArgs;
                    var serverFullState = (ServerFullState) serverStateEventArgs.ServerState;
                    Console.WriteLine($"[{DateTime.Now}] [{serverStateEventArgs.ServerName}] State has updated: ({serverFullState.PlayerCount} out of {serverFullState.MaxPlayers}) [{string.Join(", ", serverFullState.PlayerList)}]");

                    if (storedPlayerList != null)
                    {
                        lock (notifierOptionsLocker)
                        {
                            foreach (var playerName in serverFullState.PlayerList)
                            {
                                if (storedPlayerList.Contains(playerName)) continue;
                                notifierOptions.Message = $"\\[{playerName}] has JOINED the game";
                                notifierOptions.Sound = NotificationSound.Submarine;
                                TerminalNotifierWrapper.Notify(notifierOptions);
                            }

                            foreach (var playerName in storedPlayerList)
                            {
                                if (serverFullState.PlayerList.Contains(playerName)) continue;
                                notifierOptions.Message = $"\\[{playerName}] has LEFT the game";
                                notifierOptions.Sound = NotificationSound.Submarine;
                                TerminalNotifierWrapper.Notify(notifierOptions);
                            }
                        }
                    }

                    storedPlayerList = serverFullState.PlayerList;
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