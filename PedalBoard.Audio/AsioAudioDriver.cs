using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.Asio;
using PedalBoard.Core;
using System.Runtime.InteropServices;

namespace PedalBoard.Audio
{
    public class AsioAudioDriver : IAudioDriver
    {
        private AsioOut _asioOut;
        private string _driverName;
        private int _sampleRate;
        private List<IAudioEffect> _effects = new List<IAudioEffect>();
        
        // Input buffers
        private float[] _processBuffer;

        public string Name => _driverName;

        public AsioAudioDriver(string driverName = null)
        {
            if (string.IsNullOrEmpty(driverName))
            {
                var drivers = AsioOut.GetDriverNames();
                if (drivers.Any())
                {
                    _driverName = drivers.First();
                }
            }
            else
            {
                _driverName = driverName;
            }
        }

        public IEnumerable<string> GetInputDevices()
        {
            return AsioOut.GetDriverNames();
        }

        public void Start(string inputDriverName, int sampleRate, int bufferSize)
        {
            if (_asioOut != null)
            {
                Stop();
            }

            if (!string.IsNullOrEmpty(inputDriverName))
            {
                _driverName = inputDriverName;
            }

            if (string.IsNullOrEmpty(_driverName)) throw new Exception("No ASIO Driver specified.");

            try 
            {
                _asioOut = new AsioOut(_driverName);
                _sampleRate = sampleRate;

                // Configure Input Monitoring
                // In NAudio's AsioOut, simply subscribing to AudioAvailable enables input recording
                _asioOut.AudioAvailable += OnAudioAvailable;
                
                // We need to initialize the driver. 
                // AsioOut.Init() takes an IWaveProvider. Since we are doing direct buffer manipulation
                // in the callback (Passthrough), we can pass a dummy provider that produces silence,
                // BUT the AsioOut will try to read from it to fill the output.
                
                // CRITICAL: We want to write to the output buffers OURSELVES in OnAudioAvailable.
                // If we pass a provider to Init(), AsioOut's internal callback might overwrite what we write 
                // or we might race.
                
                // However, NAudio's AsioOut implementation logic:
                // 1. driver.CreateBuffers(...)
                // 2. The callback from ASIO fires.
                // 3. NAudio's callback reads from sourceStream (the provider) and writes to output buffers.
                // 4. Then it fires AudioAvailable.
                
                // This means NAudio will overwrite our manual changes if we do them *before* it runs?
                // Wait, AudioAvailable is fired *after* reading from the source stream?
                // Looking at NAudio source: 
                // It reads from sourceStream -> converts -> fills output buffer.
                // Then it fires AudioAvailable with the buffers.
                
                // So if we write to OutputBuffers in AudioAvailable, we are overwriting whatever NAudio put there.
                // So passing a silence provider is safe.
                
                var dummyProvider = new BufferedWaveProvider(new WaveFormat(sampleRate, 2)); // Stereo
                // We don't write anything to it, so it produces silence (0s).
                
                _asioOut.Init(dummyProvider);
                _asioOut.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing ASIO: {ex.Message}");
                throw;
            }
        }

        private void OnAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
        {
            // e.InputBuffers: IntPtr[]
            // e.OutputBuffers: IntPtr[]
            
            int samples = e.SamplesPerBuffer;
            
            if (_processBuffer == null || _processBuffer.Length < samples)
            {
                _processBuffer = new float[samples];
            }

            // 1. READ INPUT (Assume Channel 0 - Left Mono)
            // If we want stereo input, we'd need to mix or process two channels.
            // Let's stick to Mono Input -> Mono Process -> Stereo Output
            
            unsafe
            {
                // Verify we have input buffers
                if (e.InputBuffers.Length > 0)
                {
                    if (e.AsioSampleType == AsioSampleType.Int32LSB)
                    {
                        int* inPtr = (int*)e.InputBuffers[0];
                        for (int i = 0; i < samples; i++)
                        {
                            _processBuffer[i] = inPtr[i] / (float)int.MaxValue;
                        }
                    }
                    else if (e.AsioSampleType == AsioSampleType.Float32LSB)
                    {
                        float* inPtr = (float*)e.InputBuffers[0];
                        for (int i = 0; i < samples; i++)
                        {
                            _processBuffer[i] = inPtr[i];
                        }
                    }
                    // TODO: Handle Int16, Int24, etc if needed.
                }
                else
                {
                    // No input, silence
                    Array.Clear(_processBuffer, 0, samples);
                }
            }

            // 2. PROCESS EFFECTS
            lock(_effects)
            {
                foreach(var effect in _effects)
                {
                    if(effect.IsEnabled)
                    {
                        effect.Process(_processBuffer, 0, samples, _sampleRate);
                    }
                }
            }

            // 3. WRITE OUTPUT
            // NAudio has already filled output with silence (from dummy provider).
            // We overwrite it.
            
            unsafe
            {
                for (int ch = 0; ch < e.OutputBuffers.Length; ch++)
                {
                    if (e.AsioSampleType == AsioSampleType.Int32LSB)
                    {
                        int* outPtr = (int*)e.OutputBuffers[ch];
                        for (int i = 0; i < samples; i++)
                        {
                            float val = _processBuffer[i];
                            // Hard clip to safety
                            if (val > 1.0f) val = 1.0f;
                            if (val < -1.0f) val = -1.0f;
                            outPtr[i] = (int)(val * int.MaxValue);
                        }
                    }
                    else if (e.AsioSampleType == AsioSampleType.Float32LSB)
                    {
                        float* outPtr = (float*)e.OutputBuffers[ch];
                        for (int i = 0; i < samples; i++)
                        {
                            outPtr[i] = _processBuffer[i];
                        }
                    }
                }
            }
            
            // Note: WrittenToOutputBuffers property does not exist in NAudio 2.2.1
            // The method simply returns void. NAudio doesn't check a flag.
            // It just exposes the pointers. Modifying the memory at the pointers is sufficient.
        }

        public void Stop()
        {
            if (_asioOut != null)
            {
                _asioOut.Stop();
                _asioOut.Dispose();
                _asioOut = null;
            }
        }

        public void SetEffectChain(IEnumerable<IAudioEffect> effects)
        {
            lock (_effects)
            {
                _effects.Clear();
                _effects.AddRange(effects);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
