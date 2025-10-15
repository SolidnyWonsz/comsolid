using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
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

        //readonly static IPAddress broadcast = IPAddress.Parse("127.0.0.1");
        IPAddress broadcast;
        IPEndPoint serverEP;

        public Client(ref UserProfile profile, ref AudioService audio, string ip = "109.173.194.88")
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            socket.SendBufferSize = ConfigHandler.GetSendBuffer();
            socket.ReceiveBufferSize = ConfigHandler.GetReceiveBuffer();

            broadcast = IPAddress.Parse(ip);
            //serverEP

            this.profile = profile;
            this.audio = audio;
        }

        ConcurrentQueue<byte[]> messageQueue = new();

        public void Start()
        {
            audio.inputAudio.AudioRecorded += Audio;
            EnqueueSend(Message.CreateMessage(Message.Types.Join, data: Encoding.ASCII.GetBytes(profile.nickname)));

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
                    byte[] buffer = new byte[4096];
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    int received = socket.ReceiveFrom(buffer, ref remoteEP);

                    if (received == 0) continue;

                    int floatCount = received / sizeof(float);
                    float[] audioData = new float[floatCount];
                    Buffer.BlockCopy(buffer, 0, audioData, 0, received);
                    audio.outputAudio.InsertPlaybackData(audioData);
                }

                Thread.Sleep(2);
            }
        }

        void Audio(object? sender, AudioService.AudioRecordedArgs e)
        {
            int byteCount = e.Samples.Length * sizeof(float);
            byte[] bytes = new byte[byteCount];
            Buffer.BlockCopy(e.Samples, 0, bytes, 0, byteCount);

            EnqueueSend(Message.CreateMessage(Message.Types.Audio, data: bytes));
        }

        public void EnqueueSend(byte[] data)
        {
            messageQueue.Enqueue(data);
        }

        public void Close()
        {
            socket.SendTo(Message.CreateMessage(Message.Types.Leave), SocketFlags.None, serverEP);

            socket.Close();
            audio.inputAudio.AudioRecorded -= Audio;
        }
    }
}