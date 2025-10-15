using System.Collections;
using ComSolid.Server;

class Program
{
    static void Main(string[] args)
    {
        int port = 5005;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p":
                case "--port":
                    if (i + 1 < args.Length)
                    {
                        port = Int32.Parse(args[i + 1]);
                        i++;
                    }
                    else
                    {
                        Console.WriteLine($"comsolid-server: Nieprawidłowe użycie {args[i]}.\nPrawidłowe użycie: comsolid-server -p <port> lub comsolid --port <port>");
                    }

                    break;
                case "-h":
                case "--help":
                    Console.WriteLine("comsolid-server\n");
                    Console.WriteLine("Użycie: comsolid-server [OPCJA]");
                    Console.WriteLine("     -h, --help");
                    Console.WriteLine("     -p, --port      Ustawia inny port niż domyślny 5005");
                    return;
            }
        }

        Server server = new Server(port);
        server.Start();
    }
}