using System;

namespace PedalBoard.Core
{
    public class DistortionEffect : AudioEffect
    {
        public float Drive { get; set; } = 0.5f; // 0.0 to 1.0+
        public float OutputLevel { get; set; } = 1.0f;
        public bool HardClipping { get; set; } = false;

        public DistortionEffect(string name = "Distortion") : base(name) { }

        protected override void ProcessEffect(float[] buffer, int offset, int count, int sampleRate)
        {
            float threshold = 1.0f;
            
            for (int i = 0; i < count; i++)
            {
                float sample = buffer[offset + i];
                
                // Apply input gain (Drive)
                sample *= (1.0f + Drive * 10.0f);

                if (HardClipping)
                {
                    // Hard clipping
                    if (sample > threshold) sample = threshold;
                    else if (sample < -threshold) sample = -threshold;
                }
                else
                {
                    // Soft clipping (ArcTan approximate)
                    // Common approximation: 2/pi * atan(x * pi/2)
                    // Or simple tanh: Math.Tanh(sample)
                    sample = (float)Math.Tanh(sample);
                }

                buffer[offset + i] = sample * OutputLevel;
            }
        }
    }
}
