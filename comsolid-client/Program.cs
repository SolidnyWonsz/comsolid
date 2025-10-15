using ComSolid.Client;
using shared;

class Program
{
    static Client? klient;
    static AudioService? audio;

    static void Main(string[] args)
    {
        int ip;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                    if (i + 1 < args.Length)
                    {
                        ip = Int32.Parse(args[i]);
                        i++;
                    }
                    else
                    {
                        Console.WriteLine($"comsolid-client: Nieprawidłowe użycie {args[i]}.\nPrawidłowe użycie: comsolid-client -i <adres IP>");
                    }
                    break;
                case "-h":
                case "--help":
                    Console.WriteLine("comsolid-client\n");
                    Console.WriteLine("Użycie: comsolid-client [OPCJA]");
                    Console.WriteLine("     -h, --help");
                    Console.WriteLine("     -i              Ustawia inny port niż domyślny 5005");
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
        klient = new Client(ref profil, ref audio);
        klient.Start();
    }

    static void OnProcessExit()
    {
        audio.Close();
        klient.Close();
    }
}