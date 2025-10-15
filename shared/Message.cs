using System;
using System.Net;
using System.Text;
using System.Text.Json;

namespace shared
{
    public class Message(Message.Types type, byte[]? data)
    {
        public enum Types : ushort
        {
            Heartbeat = 0,
            Join = 1,
            Leave = 100,
            Audio = 200
        }

        public Types Type { get; set; } = type;
        public byte[] Data { get; set; } = data;

        public static byte[] CreateMessage(Message.Types type, IPAddress? user = null, byte[]? data = null)
        {
            Message msg = new(type, data);
            string json = JsonSerializer.Serialize(msg);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}