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
                    format = SDL.AUDIO_F32,
                    channels = 2,
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
            public ConcurrentQueue<float> playbackQueue = new ConcurrentQueue<float>();
            SDL.SDL_AudioCallback playbackCallbackDelegate;

            public OutputAudio()
            {
                playbackCallbackDelegate = PlaybackCallback;

                SDL.SDL_AudioSpec desiredSpec = new()
                {
                    freq = 44100,
                    format = SDL.AUDIO_F32,
                    channels = 2,
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
                float[] outputSamples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    if (playbackQueue.TryDequeue(out float sample))
                    {
                        outputSamples[i] = sample;
                    }
                    else
                    {
                        outputSamples[i] = 0f;
                    }
                }

                Marshal.Copy(outputSamples, 0, stream, sampleCount);
            }

            public void InsertPlaybackData(float[] outputSamples)
            {
                foreach (var sample in outputSamples)
                    playbackQueue.Enqueue(sample);
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