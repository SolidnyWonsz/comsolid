using System;
using System.Net;
using System.Text;
using System.Text.Json;

namespace shared
{
    public class Message(Message.Types type, string? user, byte[]? data)
    {
        public enum Types : ushort
        {
            Heartbeat = 0,
            Join = 1,
            Leave = 100,
            Audio = 200
        }

        public Types Type { get; set; } = type;
        public string? User { get; set; } = user;
        public byte[]? Data { get; set; } = data;

        public static byte[] CreateMessage(Message.Types type, out Message msg, IPEndPoint? user = null, byte[]? data = null)
        {
            msg = new(type, user?.ToString(), data);
            string json = JsonSerializer.Serialize(msg);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}