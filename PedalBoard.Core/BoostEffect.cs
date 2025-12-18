using System;

namespace PedalBoard.Core
{
    public class BoostEffect : AudioEffect
    {
        public float Gain { get; set; } = 1.0f; // 1.0 is unity gain

        public BoostEffect() : base("Boost") { }

        protected override void ProcessEffect(float[] buffer, int offset, int count, int sampleRate)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] *= Gain;
            }
        }
    }
}
