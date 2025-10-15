using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Linq;
using shared;

namespace ComSolid.Server
{
    public class Server
    {
        UdpClient listener;
        IPEndPoint groupEP;
        ConcurrentDictionary<IPEndPoint, string> users;

        public Server(int port = 5005)
        {
            listener = new UdpClient(port);
            groupEP = new IPEndPoint(IPAddress.Any, port);
            users = new ConcurrentDictionary<IPEndPoint, string>();

            Console.WriteLine($"Nasluchiwanie na porcie {port}");
        }

        public void Start()
        {
            try
            {
                while (true)
                {
                    Listen();
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
            }
        }

        void Listen()
        {
            byte[] data = listener.Receive(ref groupEP);
            if (data.Length == 0) return;

            Console.WriteLine("JAKIES DANE KURWA");

            string text = Encoding.UTF8.GetString(data);
            using var doc = JsonDocument.Parse(text);
            ushort type = doc.RootElement.GetProperty("Type").GetUInt16();
            string? name = doc.RootElement.GetProperty("User").GetString();
            
            string? base64 = doc.RootElement.GetProperty("Data").GetString();

            //Console.WriteLine($"({groupEP}) ({name}):");

            switch (type)
            {
                case 0:
                    //Console.WriteLine("[HEARTBEAT]");
                    break;
                case 1:
                    HandleJoin(ref groupEP, name);
                    break;
                case 100:
                    HandleLeave(ref groupEP);
                    break;
                case 200:
                    byte[] byteData = Convert.FromBase64String(base64);
                    BroadcastData(ref groupEP, ref byteData);
                    break;
            }
        }

        void BroadcastData(ref IPEndPoint _groupEP, ref byte[] data)
        {
            foreach (var user in users.Keys)
            {
                if (user.Equals(_groupEP)) continue;
                listener.SendAsync(data, data.Length, user);
            }
        }

        void HandleJoin(ref IPEndPoint _groupEP, string username)
        {
            Console.WriteLine($"({DateTime.Now}) ({groupEP}) [JOIN]");
            users.TryAdd(_groupEP, username);
        }

        void HandleLeave(ref IPEndPoint _groupEP)
        {
            Console.WriteLine($"({DateTime.Now}) ({groupEP}) [LEAVE]");
            users.TryRemove(_groupEP, out string? _value);
        }
    }
}