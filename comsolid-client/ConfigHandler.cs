using System;
using System.Text;
using System.Text.Json;

namespace ComSolid.Client
{
    public static class ConfigHandler
    {
        class Config
        {
            public string? Name { get; set; }
            public int SendBufferSize { get; set; }
            public int ReceiveBufferSize { get; set; }
            public ushort SampleRate { get; set; }
            public byte Channels { get; set; }
            public int Frequency { get; set; }
        }

        static Config? config;

        static readonly Config config_template = new Config
        {
            Name = string.Empty,
            SendBufferSize = 4096,
            ReceiveBufferSize = 4096,
            SampleRate = 512,
            Channels = 2,
            Frequency = 44100,
        };

        public static void Load()
        {
            if (!File.Exists("config.json"))
            {
                File.WriteAllText("config.json", JsonSerializer.Serialize<Config>(config_template));
                config = config_template;
            }
            else
            {
                string data = File.ReadAllText("config.json");
                config = JsonSerializer.Deserialize<Config>(data);
            }
        }

        public static bool IsValid()
        {
            return (config != null) && (config.Name != string.Empty);
        }

        public static int GetSendBuffer() { return config.SendBufferSize; }
        public static int GetReceiveBuffer() { return config.ReceiveBufferSize; }
        public static string GetUsername() { return config.Name; }
        public static ushort GetSampleRate() { return config.SampleRate; }
        public static byte GetChannels() { return config.Channels; }
        public static int GetFrequency() { return config.Frequency; }
    }
}