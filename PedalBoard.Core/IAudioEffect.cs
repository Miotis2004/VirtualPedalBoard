namespace PedalBoard.Core
{
    public interface IAudioEffect
    {
        string Name { get; }
        bool IsEnabled { get; set; }
        void Process(float[] buffer, int offset, int count, int sampleRate);
    }
}
