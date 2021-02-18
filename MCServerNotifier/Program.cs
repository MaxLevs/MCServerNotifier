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
            var service = new Service();
            var server = new Server("ML_VDS", "140.82.11.11", 25565);
            server.Watch(service);
        }
    }
}