using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MCServerNotifier
{
    public class Service
    {
        private UdpClient _client;
        private ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _tcsDictionary;
        
        public Service()
        {
            _client = new UdpClient();
            Task.Run(new Action (() =>
            {
                IPEndPoint ipEndPoint = null;

                while (true)
                {
                    try
                    {
                        var receivedBytes = _client.Receive(ref ipEndPoint);
                        
                        var sessionIdBytes = new byte[4];
                        Buffer.BlockCopy(receivedBytes, 1, sessionIdBytes, 0, 4);
                        var sessionId = new SessionId(sessionIdBytes);

                        if (_tcsDictionary.TryGetValue(sessionId.GetString(), out TaskCompletionSource<byte[]> tcs)) tcs.SetResult(receivedBytes);
                    }
                    catch (SocketException)
                    {
                        ;//при невозможности соединения продолжаем работать
                    }
                }
            }));        
        }

        public async Task<byte[]> SendReceiveAsync(Request request, string ip, int port, int timeOut)
        {
            var sessionId = request.SessionId.GetString();
            var tcs = new TaskCompletionSource<byte[]>();

            try
            {
                var tokenSource = new CancellationTokenSource(timeOut);
                var token = tokenSource.Token;
                if (!_tcsDictionary.ContainsKey(sessionId)) _tcsDictionary.TryAdd(sessionId, tcs);
                _client.Send(request.Data, request.Data.Length, ip, port);
                // use SendAsync?

                var result = await tcs.Task.WithCancellation(token);
                return result;
            }

            finally
            {
                _tcsDictionary.TryRemove(sessionId, out tcs);
            }
        }
    }
    
    static class TaskExtension
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            using (cancellationToken.Register(
                s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
                if (task != await Task.WhenAny(task, tcs.Task))
                    throw new OperationCanceledException(cancellationToken);
            return await task;
        }
    }
}