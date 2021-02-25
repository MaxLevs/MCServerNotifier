using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using MCQueryLib.State;
using TerminalNotifierLib;

namespace MCServerNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            using var serversConfig = File.OpenText("Resources/servers.json");
            var servers = JsonSerializer.Deserialize<Server[]>(serversConfig.ReadToEnd());
            
            if (servers == null) return;

            var rnd = new Random();
            foreach (var server in servers)
            {
                if (server.QueryPort.HasValue)
                {
                    var statusWatcher = new StatusWatcher(server.Name, server.Host, server.QueryPort.Value, rnd) {Debug = true};
                    var notifierOptionsLocker = new object();
                    var notifierOptions = new TerminalNotifierOptions
                    {
                        Title = $"\\[{server.Name}] MC Server notification",
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
                        // Console.WriteLine( $"[{DateTime.Now}] [{serverStateEventArgs.ServerName}] State has updated: ({serverFullState.NumPlayers} out of {serverFullState.MaxPlayers}) [{string.Join(", ", serverFullState.PlayerList)}]");
                        Console.WriteLine( $"[{DateTime.Now}] [{serverStateEventArgs.ServerName}] State has updated:\n{serverFullState}\n");

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
            }

            while (true)
            {
                // Just chill and flex here...
                Console.ReadLine();
            }
        }
    }
}