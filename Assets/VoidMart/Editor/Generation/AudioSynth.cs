using System;
using UnityEngine;

namespace VoidMart.EditorTools
{
    /// <summary>Minimal software synthesiser: oscillators, envelopes, a one-pole filter and a
    /// Schroeder reverb.  Enough to write the whole soundtrack from code.</summary>
    public class AudioSynth
    {
        public enum Wave { Sine, Saw, Square, Triangle, Noise }

        public readonly int SampleRate;
        public readonly int Channels;
        public readonly float[] Buffer;
        readonly System.Random m_Random;

        public int FrameCount => Buffer.Length / Channels;

        public AudioSynth(double seconds, int sampleRate = 44100, int channels = 2, int seed = 1234)
        {
            SampleRate = sampleRate;
            Channels = Mathf.Clamp(channels, 1, 2);
            Buffer = new float[Mathf.Max(1, (int)(seconds * sampleRate)) * Channels];
            m_Random = new System.Random(seed);
        }

        public static double MidiToFrequency(double midi) => 440.0 * Math.Pow(2.0, (midi - 69.0) / 12.0);

        double Oscillator(Wave wave, double phase, ref double noiseState)
        {
            switch (wave)
            {
                case Wave.Sine: return Math.Sin(phase * Math.PI * 2.0);
                case Wave.Saw: return 2.0 * (phase - Math.Floor(phase + 0.5));
                case Wave.Square: return phase % 1.0 < 0.5 ? 1.0 : -1.0;
                case Wave.Triangle: return 4.0 * Math.Abs(phase - Math.Floor(phase + 0.75) + 0.25) - 1.0;
                default:
                    noiseState = m_Random.NextDouble() * 2.0 - 1.0;
                    return noiseState;
            }
        }

        public void Mix(int frame, double left, double right)
        {
            if (frame < 0 || frame >= FrameCount) return;
            if (Channels == 1)
            {
                Buffer[frame] += (float)((left + right) * 0.5);
                return;
            }
            Buffer[frame * 2] += (float)left;
            Buffer[frame * 2 + 1] += (float)right;
        }

        /// <summary>Renders one note with an ADSR envelope, optional pitch sweep and lowpass.</summary>
        public void Note(double start, double duration, double frequency, Wave wave, double amplitude,
            double attack = 0.005, double decay = 0.08, double sustain = 0.6, double release = 0.12,
            double pan = 0.0, double lowpassHz = 0.0, double endFrequency = 0.0, double detuneCents = 0.0,
            double pulseWidth = 0.5)
        {
            int startFrame = (int)(start * SampleRate);
            double total = duration + release;
            int frames = (int)(total * SampleRate);
            if (frames <= 0) return;

            double phase = 0.0, phaseB = 0.0, noise = 0.0;
            double lpState = 0.0;
            double lpCoefficient = lowpassHz > 0.0 ? 1.0 - Math.Exp(-2.0 * Math.PI * lowpassHz / SampleRate) : 1.0;
            double detune = Math.Pow(2.0, detuneCents / 1200.0);
            double leftGain = Math.Sqrt(Math.Max(0.0, 0.5 * (1.0 - pan)));
            double rightGain = Math.Sqrt(Math.Max(0.0, 0.5 * (1.0 + pan)));

            for (int i = 0; i < frames; i++)
            {
                double time = i / (double)SampleRate;
                double env = Envelope(time, duration, attack, decay, sustain, release);
                if (env <= 0.00001) continue;

                double f = endFrequency > 0.0
                    ? frequency * Math.Pow(endFrequency / frequency, Math.Min(1.0, time / Math.Max(0.0001, duration)))
                    : frequency;

                phase += f / SampleRate;
                phaseB += f * detune / SampleRate;

                double sample = Oscillator(wave, phase, ref noise);
                if (detuneCents != 0.0) sample = (sample + Oscillator(wave, phaseB, ref noise)) * 0.5;
                if (wave == Wave.Square && Math.Abs(pulseWidth - 0.5) > 0.001)
                    sample = (phase % 1.0) < pulseWidth ? 1.0 : -1.0;

                if (lowpassHz > 0.0)
                {
                    lpState += lpCoefficient * (sample - lpState);
                    sample = lpState;
                }

                double value = sample * env * amplitude;
                Mix(startFrame + i, value * leftGain * 1.4142, value * rightGain * 1.4142);
            }
        }

        static double Envelope(double time, double duration, double attack, double decay, double sustain, double release)
        {
            if (time < 0.0) return 0.0;
            if (time < attack) return time / Math.Max(0.0001, attack);
            if (time < attack + decay)
            {
                double t = (time - attack) / Math.Max(0.0001, decay);
                return 1.0 + (sustain - 1.0) * t;
            }
            if (time < duration) return sustain;
            double r = (time - duration) / Math.Max(0.0001, release);
            return r >= 1.0 ? 0.0 : sustain * (1.0 - r);
        }

        /// <summary>Filtered noise burst - the backbone of every percussion hit and whoosh.</summary>
        public void NoiseBurst(double start, double duration, double amplitude, double lowpassHz,
            double highpassHz = 0.0, double pan = 0.0, double sweepToHz = 0.0, double attack = 0.001)
        {
            int startFrame = (int)(start * SampleRate);
            int frames = (int)(duration * SampleRate);
            double lpState = 0.0, hpState = 0.0;
            double leftGain = Math.Sqrt(Math.Max(0.0, 0.5 * (1.0 - pan)));
            double rightGain = Math.Sqrt(Math.Max(0.0, 0.5 * (1.0 + pan)));

            for (int i = 0; i < frames; i++)
            {
                double t = i / (double)frames;
                double env = t < attack / Math.Max(0.0001, duration)
                    ? t / Math.Max(0.0001, attack / duration)
                    : Math.Pow(1.0 - t, 2.2);

                double cutoff = sweepToHz > 0.0 ? Mathf.Lerp((float)lowpassHz, (float)sweepToHz, (float)t) : lowpassHz;
                double lpCoefficient = 1.0 - Math.Exp(-2.0 * Math.PI * cutoff / SampleRate);
                double sample = m_Random.NextDouble() * 2.0 - 1.0;

                lpState += lpCoefficient * (sample - lpState);
                sample = lpState;

                if (highpassHz > 0.0)
                {
                    double hpCoefficient = 1.0 - Math.Exp(-2.0 * Math.PI * highpassHz / SampleRate);
                    hpState += hpCoefficient * (sample - hpState);
                    sample -= hpState;
                }

                double value = sample * env * amplitude;
                Mix(startFrame + i, value * leftGain * 1.4142, value * rightGain * 1.4142);
            }
        }

        /// <summary>Simple feedback delay for plucks and arps.</summary>
        public void Delay(double timeSeconds, double feedback, double mix)
        {
            int offset = (int)(timeSeconds * SampleRate) * Channels;
            if (offset <= 0 || offset >= Buffer.Length) return;
            for (int i = offset; i < Buffer.Length; i++)
                Buffer[i] += (float)(Buffer[i - offset] * feedback * mix);
        }

        /// <summary>Schroeder reverb: three combs into one allpass. Cheap, and plenty for a loop.</summary>
        public void Reverb(double amount)
        {
            if (amount <= 0.001) return;
            int[] combDelays = { 1557, 1617, 1491 };
            double[] combGains = { 0.78, 0.76, 0.74 };
            var wet = new float[Buffer.Length];

            for (int c = 0; c < combDelays.Length; c++)
            {
                int delay = combDelays[c] * Channels;
                if (delay >= Buffer.Length) continue;
                var line = new float[Buffer.Length];
                for (int i = 0; i < Buffer.Length; i++)
                {
                    float delayed = i >= delay ? line[i - delay] : 0f;
                    line[i] = Buffer[i] + delayed * (float)combGains[c];
                    wet[i] += line[i] * 0.33f;
                }
            }

            int allpass = 225 * Channels;
            for (int i = allpass; i < wet.Length; i++)
                wet[i] += wet[i - allpass] * -0.7f;

            for (int i = 0; i < Buffer.Length; i++)
                Buffer[i] = (float)(Buffer[i] * (1.0 - amount * 0.5) + wet[i] * amount * 0.22);
        }

        /// <summary>Crossfades the tail into the head so the clip loops seamlessly.</summary>
        public void MakeSeamless(double crossfadeSeconds)
        {
            int fadeFrames = (int)(crossfadeSeconds * SampleRate);
            if (fadeFrames <= 0 || fadeFrames * 2 >= FrameCount) return;

            for (int i = 0; i < fadeFrames; i++)
            {
                float t = i / (float)fadeFrames;
                int head = i * Channels;
                int tail = (FrameCount - fadeFrames + i) * Channels;
                for (int c = 0; c < Channels; c++)
                {
                    float mixed = Buffer[head + c] * t + Buffer[tail + c] * (1f - t);
                    Buffer[head + c] = mixed;
                }
            }
        }

        /// <summary>Removes the crossfaded tail so the loop point is exact.</summary>
        public float[] TrimTail(double seconds)
        {
            int keep = Math.Max(1, FrameCount - (int)(seconds * SampleRate)) * Channels;
            keep = Math.Min(keep, Buffer.Length);
            var trimmed = new float[keep];
            Array.Copy(Buffer, trimmed, keep);
            return trimmed;
        }

        public void Normalize(float peak = 0.92f, float[] target = null)
        {
            var data = target ?? Buffer;
            float max = 0f;
            for (int i = 0; i < data.Length; i++) max = Mathf.Max(max, Mathf.Abs(data[i]));
            if (max <= 0.0001f) return;
            float gain = peak / max;
            for (int i = 0; i < data.Length; i++)
            {
                float v = data[i] * gain;
                // gentle soft clip keeps transients from sounding brittle
                data[i] = Mathf.Clamp(v - (v * v * v) / 3f * 0.25f, -1f, 1f);
            }
        }

        // ------------------------------------------------------------------ WAV

        public static byte[] EncodeWav(float[] samples, int sampleRate, int channels)
        {
            int byteCount = samples.Length * 2;
            var bytes = new byte[44 + byteCount];

            void WriteAscii(int offset, string text)
            {
                for (int i = 0; i < text.Length; i++) bytes[offset + i] = (byte)text[i];
            }
            void WriteInt(int offset, int value)
            {
                bytes[offset] = (byte)(value & 0xFF);
                bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
                bytes[offset + 2] = (byte)((value >> 16) & 0xFF);
                bytes[offset + 3] = (byte)((value >> 24) & 0xFF);
            }
            void WriteShort(int offset, int value)
            {
                bytes[offset] = (byte)(value & 0xFF);
                bytes[offset + 1] = (byte)((value >> 8) & 0xFF);
            }

            WriteAscii(0, "RIFF");
            WriteInt(4, 36 + byteCount);
            WriteAscii(8, "WAVE");
            WriteAscii(12, "fmt ");
            WriteInt(16, 16);
            WriteShort(20, 1);                       // PCM
            WriteShort(22, channels);
            WriteInt(24, sampleRate);
            WriteInt(28, sampleRate * channels * 2); // byte rate
            WriteShort(32, channels * 2);            // block align
            WriteShort(34, 16);                      // bits per sample
            WriteAscii(36, "data");
            WriteInt(40, byteCount);

            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                bytes[44 + i * 2] = (byte)(value & 0xFF);
                bytes[44 + i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }
            return bytes;
        }
    }
}
