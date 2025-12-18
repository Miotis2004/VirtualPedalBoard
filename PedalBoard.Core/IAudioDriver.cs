using System.Collections.Generic;

namespace PedalBoard.Core
{
    public interface IAudioDriver : System.IDisposable
    {
        string Name { get; }
        IEnumerable<string> GetInputDevices();
        void Start(string inputDeviceName, int sampleRate, int bufferSize);
        void Stop();
        void SetEffectChain(IEnumerable<IAudioEffect> effects);
    }
}
