using System;

namespace PedalBoard.Core
{
    public class ChorusEffect : AudioEffect
    {
        private DelayLine _delayLine;
        private float _lfoPhase;
        
        public float Rate { get; set; } = 1.0f; // Hz
        public float Depth { get; set; } = 0.5f; // 0 to 1
        public float Mix { get; set; } = 0.5f;

        // Base delay in samples (e.g. 20ms)
        private const float BaseDelayMs = 20.0f;

        public ChorusEffect() : base("Chorus") 
        {
            // Allocate enough buffer for max delay swing
            // 48kHz * 0.05s = 2400 samples
            _delayLine = new DelayLine(4800); 
        }

        protected override void ProcessEffect(float[] buffer, int offset, int count, int sampleRate)
        {
            float baseDelaySamples = (BaseDelayMs / 1000.0f) * sampleRate;
            float lfoStep = (2.0f * (float)Math.PI * Rate) / sampleRate;

            for (int i = 0; i < count; i++)
            {
                float input = buffer[offset + i];
                
                // Write input to delay line
                _delayLine.Write(input);

                // Calculate LFO
                _lfoPhase += lfoStep;
                if (_lfoPhase > 2.0f * Math.PI) _lfoPhase -= 2.0f * (float)Math.PI;

                float lfoVal = (float)Math.Sin(_lfoPhase);
                
                // Modulate delay time
                // Depth determines how many samples we swing away from base
                float swing = baseDelaySamples * Depth * 0.5f; 
                float currentDelay = baseDelaySamples + (lfoVal * swing);

                float delayedSignal = _delayLine.Read(currentDelay);

                // Mix dry and wet
                buffer[offset + i] = (input * (1.0f - Mix)) + (delayedSignal * Mix);
            }
        }
    }
}
