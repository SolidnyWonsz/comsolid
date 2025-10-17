using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using shared;

namespace ComSolid.Client
{
    public class Client
    {
        Socket socket;
        UserProfile profile;
        AudioService audio;
        ConcurrentQueue<byte[]> messageQueue = new();

        IPAddress broadcast;
        IPEndPoint serverEP;
        IPEndPoint localEP;

        public Client(ref UserProfile profile, ref AudioService audio, string ip = "109.173.194.88")
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            socket.SendBufferSize = 8192;
            socket.ReceiveBufferSize = 8192;

            broadcast = IPAddress.Parse(ip);
            serverEP = new IPEndPoint(broadcast, 5005);
            localEP = new IPEndPoint(IPAddress.Any, 0);

            this.profile = profile;
            this.audio = audio;
        }

        public void Start()
        {
            audio.inputAudio.AudioRecorded += Audio;
            EnqueueSend(Message.CreateMessage(Message.Types.Join, out _, data: Encoding.ASCII.GetBytes(profile.nickname)));

            while (true)
            {
                while (messageQueue.TryDequeue(out byte[]? data))
                {
                    try
                    {
                        socket.SendTo(data, SocketFlags.None, serverEP);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine($"{e}");
                    }
                }

                if (socket.Available > 0)
                {
                    byte[] buffer = new byte[8192];
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    int received = socket.ReceiveFrom(buffer, ref remoteEP);

                    if (received == 0) continue;

                    string msg = Encoding.UTF8.GetString(buffer, 0, received);
                    using var doc = JsonDocument.Parse(msg);
                    string? name = doc.RootElement.GetProperty("User").GetString();
                    string? base64 = doc.RootElement.GetProperty("Data").GetString();

                    byte[] byteData = Convert.FromBase64String(base64);

                    int floatCount = received / sizeof(float);
                    float[] audioData = new float[floatCount];
                    Buffer.BlockCopy(byteData, 0, audioData, 0, byteData.Length);
                    audio.outputAudio.InsertPlaybackData(name, audioData);
                }

                Thread.Sleep(2);
            }
        }

        void Audio(object? sender, AudioService.AudioRecordedArgs e)
        {
            int byteCount = e.Samples.Length * sizeof(float);
            byte[] bytes = new byte[byteCount];
            Buffer.BlockCopy(e.Samples, 0, bytes, 0, byteCount);

            EnqueueSend(Message.CreateMessage(Message.Types.Audio, out _, data: bytes));
        }

        public void EnqueueSend(byte[] data)
        {
            messageQueue.Enqueue(data);
        }

        public void Close()
        {
            socket.SendTo(Message.CreateMessage(Message.Types.Leave, out _), SocketFlags.None, serverEP);

            socket.Close();
            audio.inputAudio.AudioRecorded -= Audio;
        }
    }
}