using ComSolid.Client;
using shared;

class Program
{
    static Client? klient;
    static AudioService? audio;

    static void Main(string[] args)
    {
        bool _loopback = false; 

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-l":
                case "--loopback:":
                    _loopback = true;
                    break;
                case "-h":
                case "--help":
                    Console.WriteLine("comsolid-client\n");
                    Console.WriteLine("Użycie: comsolid-client [OPCJA]");
                    Console.WriteLine("     -h, --help");
                    Console.WriteLine("     -l, --loopback      Łączy się z lokalnym serwerem (localhost)");
                    return;
            }
        }

        ConfigHandler.Load();

        if (ConfigHandler.GetUsername() == string.Empty)
        {
            Console.WriteLine("Ustaw nick w pliku config.json");
            return;
        }

        AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => OnProcessExit();
        Console.CancelKeyPress += (_, ea) => OnProcessExit();

        audio = new();

        UserProfile profil = new UserProfile(ConfigHandler.GetUsername());
        
        if (_loopback)
        {
            klient = new Client(ref profil, ref audio, "127.0.0.1");
        }
        else
        {
            klient = new Client(ref profil, ref audio);
        }

        klient.Start();
    }

    static void OnProcessExit()
    {
        audio.Close();
        klient.Close();
    }
}