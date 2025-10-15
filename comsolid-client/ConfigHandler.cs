using System;
using System.Text;
using System.Text.Json;

namespace ComSolid.Client
{
    public class ConfigHandler
    {
        class Config
        {
            public string? Name { get; set; }
            public int SendBufferSize { get; set; }
            public int ReceiveBufferSize { get; set; }
            public int SampleRate { get; set; }
            public int AudioBufferSize { get; set; }
        }

        readonly Config? config;

        static readonly Config config_template = new Config
        {
            Name = string.Empty,
            SendBufferSize = 4096,
            ReceiveBufferSize = 4096,
            SampleRate = 4096,
            AudioBufferSize = 4096
        };

        public ConfigHandler()
        {
            if (!File.Exists("config.json"))
            {
                File.WriteAllText("config.json", JsonSerializer.Serialize<Config>(config_template));
            }
            else
            {
                string data = File.ReadAllText("config.json");
                config = JsonSerializer.Deserialize<Config>(data);
            }
        }

        public bool IsValid()
        {
            return (config != null) && (config.Name != string.Empty);
        }

        public int GetSendBuffer() { return config.SendBufferSize; }
        public int GetReceiveBuffer() { return config.ReceiveBufferSize; }
        public int GetSampleRate() { return config.SampleRate; }
        public int GetAudioBuffer() { return config.AudioBufferSize; }
        public string GetUsername() { return config.Name; }
    }
}