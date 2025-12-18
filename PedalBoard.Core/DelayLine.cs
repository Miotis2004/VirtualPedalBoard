using System;

namespace PedalBoard.Core
{
    // A circular buffer delay line
    public class DelayLine
    {
        private float[] _buffer;
        private int _writeIndex;
        private int _length;

        public DelayLine(int maxDelaySamples)
        {
            _length = maxDelaySamples;
            _buffer = new float[_length];
            _writeIndex = 0;
        }

        public void Write(float sample)
        {
            _buffer[_writeIndex] = sample;
            _writeIndex++;
            if (_writeIndex >= _length) _writeIndex = 0;
        }

        public float Read(float delaySamples)
        {
            // Linear interpolation for fractional delay
            float readPos = _writeIndex - delaySamples;
            while (readPos < 0) readPos += _length;
            while (readPos >= _length) readPos -= _length;

            int indexA = (int)readPos;
            int indexB = indexA + 1;
            if (indexB >= _length) indexB = 0;

            float fraction = readPos - indexA;

            return _buffer[indexA] * (1.0f - fraction) + _buffer[indexB] * fraction;
        }

        public float ReadValues(int offset)
        {
            int index = _writeIndex - offset;
            while(index < 0) index += _length;
            return _buffer[index];
        }
    }
}
