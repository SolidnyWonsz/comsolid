using ComSolid.Client;
using shared;

class Program
{
    static Client? klient;
    static AudioService? audio;

    static async Task Main(string[] args)
    {
        Console.WriteLine("comsolid-client");   
        ConfigHandler config = new ConfigHandler();

        if (config.GetUsername() == string.Empty)
        {
            Console.WriteLine("Ustaw nick w pliku config.json");
            return;
        }

        AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => OnProcessExit();
        Console.CancelKeyPress += (_, ea) => OnProcessExit();

        audio = new();

        UserProfile profil = new UserProfile(config.GetUsername());
        klient = new Client(config.GetSendBuffer(), config.GetReceiveBuffer(), ref profil, ref audio);
        klient.Start();
    }

    static void OnProcessExit()
    {
        audio.Close();
        klient.Close();
    }
}