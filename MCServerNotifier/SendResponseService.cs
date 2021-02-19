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
    public class SendResponseService
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
        
        private readonly UdpClient _client;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _tcsDictionary;
        private Task ReceiveTask { get; }
        
        public SendResponseService()
        {
            _client = new UdpClient();
            _tcsDictionary = new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();
            ReceiveTask = Task.Run(new Action (() =>
            {
                IPEndPoint ipEndPoint = null;

                while (true)
                {
                    try
                    {
                        var receivedBytes = _client.Receive(ref ipEndPoint);
                        var sessionId = Response.ParseSessionId(receivedBytes);
                        if (_tcsDictionary.TryGetValue(sessionId.GetString(), out TaskCompletionSource<byte[]> tcs)) tcs.SetResult(receivedBytes);
                    }
                    catch (SocketException)
                    {
                        //при невозможности соединения продолжаем работать
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

        public void Wait()
        {
            ReceiveTask.Wait();
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