using System;
using System.Net;
using MCQueryLib;

namespace MCServerNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new McQuery(IPAddress.Loopback, 25565);
        }
    }
}