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
            public byte Channels { get; set; }
        }

        static Config? config;

        static readonly Config config_template = new Config
        {
            Name = string.Empty,
            Channels = 2
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

        public static string GetUsername() { return config.Name; }
        public static byte GetChannels() { return config.Channels; }
    }
}