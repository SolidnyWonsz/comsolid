using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using SDL2;
using ComSolid.Client;
using System.Diagnostics;

namespace ComSolid.Client
{
    public class AudioService
    {
        #region InputAudio
        public class InputAudio
        {
            uint captureDevice;
            SDL.SDL_AudioCallback recordingCallbackDelegate;

            public InputAudio()
            {
                recordingCallbackDelegate = RecordingCallback;
                SDL.SDL_AudioSpec desiredSpec = new()
                {
                    freq = 44100,
                    format = SDL.AUDIO_F32SYS,
                    channels = ConfigHandler.GetChannels(),
                    samples = 512,
                    callback = recordingCallbackDelegate
                };

                captureDevice = SDL.SDL_OpenAudioDevice(
                    null,
                    1,
                    ref desiredSpec,
                    out SDL.SDL_AudioSpec obtainedSpec,
                    0
                );

                if (captureDevice == 0)
                {
                    throw new Exception($"Nie można było otworzyć urządzenia audio: {SDL.SDL_GetError()}");
                }

                StartListening();
            }

            public void StartListening()
            {
                SDL.SDL_PauseAudioDevice(captureDevice, 0);
            }

            public void Mute()
            {
                SDL.SDL_PauseAudioDevice(captureDevice, 1);
            }

            void RecordingCallback(IntPtr userdata, IntPtr stream, int len)
            {
                int sampleCount = len / sizeof(float);
                float[] samples = new float[sampleCount];

                Marshal.Copy(stream, samples, 0, sampleCount);

                AudioRecorded?.Invoke(this, new AudioRecordedArgs { Samples = samples });
            }

            public void Close()
            {
                SDL.SDL_PauseAudioDevice(captureDevice, 1);
                SDL.SDL_CloseAudioDevice(captureDevice);
            }

            public event EventHandler<AudioRecordedArgs> AudioRecorded;
        }

        public class AudioRecordedArgs : EventArgs
        {
            public required float[] Samples { get; set; }
        }

        #endregion
        #region OutputAudio

        public class OutputAudio
        {
            uint outputDevice;
            ConcurrentDictionary<string, ConcurrentQueue<float[]>> playbackQueue = new ConcurrentDictionary<string, ConcurrentQueue<float[]>>();
            SDL.SDL_AudioCallback playbackCallbackDelegate;

            public OutputAudio()
            {
                playbackCallbackDelegate = PlaybackCallback;

                SDL.SDL_AudioSpec desiredSpec = new()
                {
                    freq = 44100,
                    format = SDL.AUDIO_F32SYS,
                    channels = ConfigHandler.GetChannels(),
                    samples = 512,
                    callback = playbackCallbackDelegate
                };

                outputDevice = SDL.SDL_OpenAudioDevice(null, 0, ref desiredSpec, out SDL.SDL_AudioSpec obtainedSpec, 0);

                if (outputDevice == 0)
                {
                    throw new Exception($"Nie można było otworzyć urządzenia audio: {SDL.SDL_GetError()}");
                }

                SDL.SDL_PauseAudioDevice(outputDevice, 0);
            }

            void PlaybackCallback(IntPtr userdata, IntPtr stream, int len)
            {
                int sampleCount = len / sizeof(float);

                float[] mixBuffer = new float[sampleCount];

                int activeStreams = 0;

                foreach (var queuePair in playbackQueue)
                {
                    var queue = queuePair.Value;
                    int samplesRead = 0;

                    while (samplesRead < sampleCount && queue.TryDequeue(out var sample))
                    {
                        int copyCount = Math.Min(sample.Length, sampleCount - samplesRead);

                        for (int i = 0; i < copyCount; i++)
                        {
                            mixBuffer[samplesRead + i] += sample[i];
                        }

                        samplesRead += copyCount;
                    }

                    if (samplesRead > 0)
                        activeStreams++;
                }

                if (activeStreams > 0)
                {
                    float gain = 1f / activeStreams;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        mixBuffer[i] = Math.Clamp(mixBuffer[i] * gain, -1f, 1f);
                    }
                }

                Marshal.Copy(mixBuffer, 0, stream, sampleCount);
            }

            public void InsertPlaybackData(string user, float[] outputSamples)
            {
                var queue = playbackQueue.GetOrAdd(user, _ => new ConcurrentQueue<float[]>());

                queue.Enqueue(outputSamples);
            }

            public void Close()
            {
                SDL.SDL_PauseAudioDevice(outputDevice, 1);
                SDL.SDL_CloseAudioDevice(outputDevice);
            }
        }

        #endregion
        #region AudioService

        public InputAudio inputAudio;
        public OutputAudio outputAudio;

        public AudioService()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) < 0)
            {
                throw new Exception($"Błąd SDL: ${SDL.SDL_GetError()}");
            }

            inputAudio = new();
            outputAudio = new();
        }

        public void Close()
        {
            inputAudio.Close();
            outputAudio.Close();
            SDL.SDL_Quit();
        }

        #endregion
    }
}