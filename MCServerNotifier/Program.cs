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
            byte[] fakeChallengeToken = { 0xFF, 0xFF, 0xFF, 0xFF };
            Request handshake = Request.GetHandshakeRequest();
            Request basicStatus = Request.GetBasicStatusRequest(fakeChallengeToken);
            Request fullStatus = Request.GetFullStatusRequest(fakeChallengeToken);
            
            const int padding = 23;
            Console.WriteLine("Handshake request: ".PadRight(padding) + BitConverter.ToString(handshake.Data));
            Console.WriteLine("Basic Status request: ".PadRight(padding) + BitConverter.ToString(basicStatus.Data));
            Console.WriteLine("Full Status request: ".PadRight(padding) + BitConverter.ToString(fullStatus.Data));
        }
    }
}