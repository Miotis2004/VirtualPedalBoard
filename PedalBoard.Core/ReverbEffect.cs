using System;

namespace PedalBoard.Core
{
    // A simple Schroeder Reverb implementation using Comb Filters and All-Pass Filters
    public class ReverbEffect : AudioEffect
    {
        // Parallel Comb Filters
        private DelayLine[] _combFilters;
        private float[] _combGains;
        
        // Series All-Pass Filters
        private DelayLine[] _allPassFilters;
        private float _allPassGain = 0.7f;

        public float Mix { get; set; } = 0.3f;
        public float Decay { get; set; } = 0.5f; // Controls feedback of comb filters

        private bool _initialized = false;
        private int _sampleRate = 44100;

        public ReverbEffect() : base("Reverb") 
        {
        }

        private void Initialize(int sampleRate)
        {
            if (_initialized && _sampleRate == sampleRate) return;

            _sampleRate = sampleRate;

            // Tuning values from Schroeder's original paper or Freeverb
            // Scaled by sample rate
            int[] combDelays = { 
                (int)(0.0297 * sampleRate), 
                (int)(0.0371 * sampleRate), 
                (int)(0.0411 * sampleRate), 
                (int)(0.0437 * sampleRate) 
            };
            
            int[] allPassDelays = { 
                (int)(0.005 * sampleRate), 
                (int)(0.0017 * sampleRate) 
            };

            _combFilters = new DelayLine[combDelays.Length];
            for (int i = 0; i < combDelays.Length; i++) _combFilters[i] = new DelayLine(combDelays[i] + 100);

            _allPassFilters = new DelayLine[allPassDelays.Length];
            for (int i = 0; i < allPassDelays.Length; i++) _allPassFilters[i] = new DelayLine(allPassDelays[i] + 100);

            _initialized = true;
        }

        protected override void ProcessEffect(float[] buffer, int offset, int count, int sampleRate)
        {
            Initialize(sampleRate);

            // Comb delays are fixed, but feedback gain is controlled by Decay
            float feedback = 0.7f + (0.28f * Decay); 

            // Hardcoded delay lengths for the filters (in samples, effectively)
            // But DelayLine Read takes float, so we cast.
            // Actually, for comb filters, we read at the max delay of the line essentially.
            // Let's keep it simple: Read at specific offsets.
            
            // Standard Schroeder tunings approx
            float[] combDelayTimes = { 
                0.0297f * sampleRate, 
                0.0371f * sampleRate, 
                0.0411f * sampleRate, 
                0.0437f * sampleRate 
            };
            
             float[] allPassDelayTimes = { 
                0.005f * sampleRate, 
                0.0017f * sampleRate 
            };

            for (int i = 0; i < count; i++)
            {
                float input = buffer[offset + i];
                float wetAccumulator = 0.0f;

                // Process parallel comb filters
                for(int c = 0; c < _combFilters.Length; c++)
                {
                    float delayed = _combFilters[c].Read(combDelayTimes[c]);
                    float newValue = input + (delayed * feedback);
                    _combFilters[c].Write(newValue);
                    wetAccumulator += delayed;
                }

                // Process series all-pass filters
                // y[n] = -g * x[n] + x[n-D] + g * y[n-D]
                // Canonical form: 
                // w[n] = x[n] + g * w[n-D]
                // y[n] = -g * w[n] + w[n-D]
                float inputAp = wetAccumulator * 0.25f; // Normalize gain a bit
                for (int a = 0; a < _allPassFilters.Length; a++)
                {
                    float delayed = _allPassFilters[a].Read(allPassDelayTimes[a]);
                    float w = inputAp + (delayed * _allPassGain);
                    _allPassFilters[a].Write(w);
                    
                    float y = -_allPassGain * w + delayed;
                    inputAp = y;
                }

                float wet = inputAp;
                
                // Mix
                buffer[offset + i] = (input * (1.0f - Mix)) + (wet * Mix);
            }
        }
    }
}
