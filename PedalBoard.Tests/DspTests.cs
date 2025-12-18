using Xunit;
using PedalBoard.Core;
using System.Linq;
using System;

namespace PedalBoard.Tests
{
    public class DspTests
    {
        [Fact]
        public void BoostEffect_IncreasesSignal()
        {
            var boost = new BoostEffect { Gain = 2.0f };
            float[] buffer = { 0.1f, 0.2f, -0.1f };
            
            boost.Process(buffer, 0, 3, 44100);

            Assert.Equal(0.2f, buffer[0], 4f);
            Assert.Equal(0.4f, buffer[1], 4f);
            Assert.Equal(-0.2f, buffer[2], 4f);
        }

        [Fact]
        public void DistortionEffect_ClipsSignal()
        {
            var distortion = new DistortionEffect { Drive = 1.0f, HardClipping = true };
            // A very loud signal that should clip
            // Input 0.5 * (1 + 10) = 5.5 -> Clipped to 1.0
            float[] buffer = { 0.5f, -0.5f };
            
            distortion.Process(buffer, 0, 2, 44100);

            Assert.Equal(1.0f, buffer[0], 4f);
            Assert.Equal(-1.0f, buffer[1], 4f);
        }

        [Fact]
        public void OverdriveEffect_SoftClipsSignal()
        {
            var od = new OverdriveEffect { Drive = 0.5f }; // (1 + 5) = x6 gain
            float[] buffer = { 0.1f }; 
            // 0.1 * 6 = 0.6
            // Tanh(0.6) = 0.537
            
            od.Process(buffer, 0, 1, 44100);

            Assert.True(Math.Abs(buffer[0]) < 0.6f); // Should be compressed
            Assert.True(buffer[0] > 0.1f); // Should still be amplified
        }
        
        [Fact]
        public void DelayLine_WritesAndReadsCorrectly()
        {
            var dl = new DelayLine(100);
            dl.Write(1.0f);
            dl.Write(0.5f);
            dl.Write(0.25f);

            // Buffer now: [1.0, 0.5, 0.25, 0, 0...]
            // WriteIndex is at 3
            
            // Read 0 samples ago (current write pos - 1 effectively?)
            // Implementation: Read(delaySamples). readPos = writeIndex - delaySamples.
            
            // If I read 1 sample delay: 3 - 1 = 2 -> 0.25
            Assert.Equal(0.25f, dl.Read(1.0f));
            
            // If I read 2 samples delay: 3 - 2 = 1 -> 0.5
            Assert.Equal(0.5f, dl.Read(2.0f));
            
            // If I read 3 samples delay: 3 - 3 = 0 -> 1.0
            Assert.Equal(1.0f, dl.Read(3.0f));
        }
    }
}
