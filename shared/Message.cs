namespace shared
{
    public struct Message(Message.Types type, string user, byte[]? data)
    {
        public enum Types : ushort
        {
            Heartbeat = 0,
            Join = 1,
            Leave = 100,
            Audio = 200
        }

        public Types Type { get; set; } = type;
        public string User { get; set; } = user;
        public byte[] Data { get; set; } = data;
    }
}