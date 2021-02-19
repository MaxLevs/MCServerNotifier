using System.Collections.Generic;
using MCServerNotifier.Data;

namespace MCServerNotifier.Packages
{
    public class Request
    {
        private readonly byte[] _magic = { 0xfe, 0xfd };
        private readonly byte[] _challenge = { 0x09 };
        private readonly byte[] _status = { 0x00 };
        public readonly SessionId SessionId = SessionId.GenerateRandomId();
        public byte[] Data { get; private set; }
        
        private Request(){}

        public static Request GetHandshakeRequest()
        {
            var request = new Request();
            
            var data = new List<byte>();
            data.AddRange(request._magic);
            data.AddRange(request._challenge);
            data.AddRange(request.SessionId.GetBytes());
            
            request.Data = data.ToArray();
            return request;
        }

        public static Request GetBasicStatusRequest(byte[] challengeToken)
        {
            var request = new Request();
            
            var data = new List<byte>();
            data.AddRange(request._magic);
            data.AddRange(request._status);
            data.AddRange(request.SessionId.GetBytes());
            data.AddRange(challengeToken);
            
            request.Data = data.ToArray();
            return request;
        }
        
        public static Request GetFullStatusRequest(byte[] challengeToken)
        {
            var request = new Request();
            
            var data = new List<byte>();
            data.AddRange(request._magic);
            data.AddRange(request._status);
            data.AddRange(request.SessionId.GetBytes());
            data.AddRange(challengeToken);
            data.AddRange(new byte[] {0x00, 0x00, 0x00, 0x00}); // Padding
            
            request.Data = data.ToArray();
            return request;
        }
    }
}