using System;
using System.Linq;

namespace MCServerNotifier.Data
{
    public class SessionId
    {
        private readonly byte[] _sessionId;

        public SessionId (byte[] sessionId)
        {
            _sessionId = sessionId;
        }

        public static SessionId GenerateRandomId()
        {
            var sessionId = new byte[4];
            new Random().NextBytes(sessionId);
            sessionId = sessionId.Select(@byte => (byte)(@byte & 0x0F)).ToArray();
            return new SessionId(sessionId);
        }

        public string GetString()
        {
            return BitConverter.ToString(_sessionId);
        }

        public byte[] GetBytes()
        {
            var sessionId = new byte[4];
            Buffer.BlockCopy(_sessionId, 0, sessionId, 0, 4);
            return sessionId;
        }
    }
}