using System;
using System.Linq;
using System.Net;
using System.Threading;
using MCQueryLib;

namespace MCServerNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            var mcQuery = new McQuery(IPAddress.Loopback, 25565);
            var delayTime = 1000;
            bool? isOnline = null;
            string[] playerBuff = null;
            
            while (true)
            {
                var fullStat = mcQuery.GetFullStat();
                
                if (playerBuff != null)
                {
                    foreach (var player in fullStat.Players)
                    {
                        if (!playerBuff.Contains(player))
                        {
                            Console.WriteLine($"[{player}] has joined the game");
                        }
                    }
                    
                    foreach (var player in playerBuff)
                    {
                        if (!fullStat.Players.Contains(player))
                        {
                            Console.WriteLine($"[{player}] has left the game");
                        }
                    }
                }

                if (isOnline != null && isOnline != mcQuery.Online)
                {
                    Console.WriteLine($"Server is online? [{mcQuery.Online}]");
                }
                isOnline = mcQuery.Online;

                playerBuff = fullStat.Players;
                
                Thread.Sleep(delayTime);
            }
        }
    }
}