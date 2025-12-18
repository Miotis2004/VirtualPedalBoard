namespace PedalBoard.Core
{
    public abstract class AudioEffect : IAudioEffect
    {
        public string Name { get; protected set; }
        public bool IsEnabled { get; set; } = true;

        protected AudioEffect(string name)
        {
            Name = name;
        }

        public void Process(float[] buffer, int offset, int count, int sampleRate)
        {
            if (!IsEnabled) return;
            ProcessEffect(buffer, offset, count, sampleRate);
        }

        protected abstract void ProcessEffect(float[] buffer, int offset, int count, int sampleRate);
    }
}
