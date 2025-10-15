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
        readonly static IPAddress broadcast = IPAddress.Parse("83.8.15.223");
        //readonly static IPAddress broadcast = IPAddress.Parse("109.173.194.88");

        readonly static IPEndPoint serverEP = new IPEndPoint(broadcast, 5005);
        CancellationTokenSource sendCts = new();
        Task? StartTask;

        public Client(int sendBuffer, int receiveBuffer, ref UserProfile profile, ref AudioService audio)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            socket.SendBufferSize = sendBuffer;
            socket.ReceiveBufferSize = receiveBuffer;

            this.profile = profile;
            this.audio = audio;
        }

        public void Start()
        {
            audio.inputAudio.AudioRecorded += Audio;
            EnqueueSend(CreateMessage(Message.Types.Join));
            StartTask = Task.Run(StartAsync);
        }

        async Task StartAsync()
        {
            while (!sendCts.Token.IsCancellationRequested)
            {
                while (sendQueue.TryDequeue(out byte[]? data))
                {
                    try
                    {
                        await socket.SendToAsync(data, SocketFlags.None, serverEP);
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine($"{e}");
                    }
                }

                byte[] buffer = new byte[4096];

                SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, new IPEndPoint(IPAddress.Any, 0));
                int received = result.ReceivedBytes;

                if (received == 0) return;

                int floatCount = received / sizeof(float);
                float[] audioData = new float[floatCount];
                Buffer.BlockCopy(buffer, 0, audioData, 0, received);

                audio.outputAudio.InsertPlaybackData(audioData);

                await Task.Delay(5, sendCts.Token);
            }
        }

        ConcurrentQueue<byte[]> sendQueue = new();

        void Audio(object? sender, AudioService.AudioRecordedArgs e)
        {
            int byteCount = e.Samples.Length * sizeof(float);
            byte[] bytes = new byte[byteCount];
            Buffer.BlockCopy(e.Samples, 0, bytes, 0, byteCount);

            EnqueueSend(CreateMessage(Message.Types.Audio, bytes));
        }

        byte[] CreateMessage(Message.Types type, byte[]? data = null)
        {
            Message msg = new(type, profile.nickname, data);
            string json = JsonSerializer.Serialize(msg);
            return Encoding.UTF8.GetBytes(json);
        }

        public void EnqueueSend(byte[] data)
        {
            if (!sendCts.IsCancellationRequested)
            {
                sendQueue.Enqueue(data);
            }
        }

        public async Task Close()
        {
            await socket.SendToAsync(CreateMessage(Message.Types.Leave), SocketFlags.None, serverEP);

            sendCts.Cancel();
            StartTask?.Wait();

            socket.Close();
            audio.inputAudio.AudioRecorded -= Audio;
        }
    }
}