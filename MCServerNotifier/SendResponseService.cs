using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MCServerNotifier.Data;
using MCServerNotifier.Packages;

namespace MCServerNotifier
{
    public static class SendResponseService
    {
        public static async Task<byte[]> SendReceive(UdpClient client, byte[] data, int receiveAwaitIntervalSeconds)
        {
            IPEndPoint ipEndPoint = null;
            byte[] response = null;
            
            await client.SendAsync(data, data.Length);
            var responseToken = client.BeginReceive(null, null);
            responseToken.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(receiveAwaitIntervalSeconds));
            if (responseToken.IsCompleted)
            {
                try
                {
                    response = client.EndReceive(responseToken, ref ipEndPoint);
                }

                catch (Exception)
                {
                    // can't end receive
                }
            }

            if (response == null)
                throw new SocketException();

            return response;
        }
    }
}