using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Data;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// The soundtrack and the sound effects, composed in code.  Music is written as chord
    /// progressions with bass / pad / pluck / drum layers, then crossfaded into a seamless loop;
    /// SFX are short synthesised one-shots matching the identifiers in the audio spec.
    /// </summary>
    public static class AudioLibrary
    {
        public class Clip
        {
            public string Key;
            public float[] Samples;
            public int SampleRate;
            public int Channels;
            public bool Loop;
        }

        enum Style { Street, Store, Puzzle }

        public static List<Clip> BuildAll(AudioConfig config)
        {
            int rate = Mathf.Clamp(config.sampleRate, 8000, 48000);
            var clips = new List<Clip>(20)
            {
                Music("music_street", Style.Street, config, rate),
                Music("music_store", Style.Store, config, rate),
                Music("music_puzzle", Style.Puzzle, config, rate)
            };
            clips.AddRange(Effects(rate));
            return clips;
        }

        // ----------------------------------------------------------------- music

        static readonly int[][] StreetProgression =
        {
            new[] { 0, 3, 7 },      // Am
            new[] { -4, 0, 3 },     // F
            new[] { 3, 7, 10 },     // C
            new[] { -2, 2, 5 }      // G
        };

        static readonly int[][] StoreProgression =
        {
            new[] { 3, 7, 10 },     // C
            new[] { -2, 2, 5 },     // G
            new[] { 0, 3, 7 },      // Am
            new[] { -4, 0, 3 }      // F
        };

        static readonly int[][] PuzzleProgression =
        {
            new[] { 0, 3, 7 },      // Am
            new[] { 7, 11, 14 },    // E
            new[] { 0, 3, 7 },      // Am
            new[] { 7, 11, 14 }     // E
        };

        static Clip Music(string key, Style style, AudioConfig config, int rate)
        {
            float bpm = Mathf.Clamp(config.musicBpm * (style == Style.Puzzle ? 1.22f : 1f), 40f, 220f);
            int bars = Mathf.Clamp(style == Style.Puzzle ? Mathf.Max(4, config.musicBars / 2) : config.musicBars, 2, 64);
            double beat = 60.0 / bpm;
            double bar = beat * 4.0;
            double crossfade = Math.Min(1.2, bar * 0.5);
            double length = bars * bar + crossfade;

            var synth = new AudioSynth(length, rate, 2, key.GetHashCode());
            var progression = style == Style.Street ? StreetProgression : style == Style.Store ? StoreProgression : PuzzleProgression;
            int root = 45 + Mathf.Clamp(config.musicRootNote, 0, 11);   // A2-ish
            double swing = Mathf.Clamp01(config.musicSwing) * 0.5;

            for (int barIndex = 0; barIndex < bars; barIndex++)
            {
                double barStart = barIndex * bar;
                var chord = progression[barIndex % progression.Length];
                bool fill = barIndex % 4 == 3;

                RenderBass(synth, barStart, beat, root, chord, style, swing);
                RenderPad(synth, barStart, bar, root, chord, style);
                RenderPluck(synth, barStart, beat, root, chord, style, swing, barIndex);
                RenderDrums(synth, barStart, beat, style, fill, swing, barIndex);
            }

            if (style != Style.Puzzle) synth.Delay(beat * 0.75, 0.32, 0.5);
            synth.Reverb(Mathf.Clamp01(config.reverbAmount) * (style == Style.Puzzle ? 0.5f : 1f));
            synth.MakeSeamless(crossfade);
            var trimmed = synth.TrimTail(crossfade);
            synth.Normalize(0.86f, trimmed);

            return new Clip { Key = key, Samples = trimmed, SampleRate = rate, Channels = 2, Loop = true };
        }

        static void RenderBass(AudioSynth synth, double barStart, double beat, int root, int[] chord, Style style, double swing)
        {
            double amp = style == Style.Puzzle ? 0.16 : 0.22;
            int note = root + chord[0];
            for (int eighth = 0; eighth < 8; eighth++)
            {
                if (style == Style.Store && eighth % 2 == 1 && eighth != 3) continue;
                double offset = eighth % 2 == 1 ? swing * beat * 0.5 : 0.0;
                double start = barStart + eighth * beat * 0.5 + offset;
                int pitch = note + (eighth == 6 ? 12 : 0);
                synth.Note(start, beat * 0.42, AudioSynth.MidiToFrequency(pitch), AudioSynth.Wave.Saw, amp,
                    0.004, 0.09, 0.55, 0.08, 0.0, 380.0, 0.0, 6.0);
                synth.Note(start, beat * 0.42, AudioSynth.MidiToFrequency(pitch - 12), AudioSynth.Wave.Sine, amp * 0.8,
                    0.004, 0.12, 0.5, 0.1);
            }
        }

        static void RenderPad(AudioSynth synth, double barStart, double bar, int root, int[] chord, Style style)
        {
            double amp = style == Style.Puzzle ? 0.05 : 0.085;
            for (int i = 0; i < chord.Length; i++)
            {
                int pitch = root + 12 + chord[i];
                double pan = (i - 1) * 0.35;
                synth.Note(barStart, bar * 0.92, AudioSynth.MidiToFrequency(pitch), AudioSynth.Wave.Saw, amp,
                    bar * 0.18, bar * 0.3, 0.7, bar * 0.25, pan, style == Style.Store ? 1700.0 : 1100.0, 0.0, 9.0);
            }
        }

        static void RenderPluck(AudioSynth synth, double barStart, double beat, int root, int[] chord, Style style, double swing, int barIndex)
        {
            var wave = style == Style.Store ? AudioSynth.Wave.Triangle : AudioSynth.Wave.Square;
            double amp = style == Style.Puzzle ? 0.14 : 0.11;
            int steps = style == Style.Puzzle ? 16 : 8;
            double step = beat * 4.0 / steps;

            for (int i = 0; i < steps; i++)
            {
                if (style == Style.Street && (i == 2 || i == 5)) continue;
                double offset = i % 2 == 1 ? swing * step * 0.5 : 0.0;
                int degree = chord[(i + barIndex) % chord.Length];
                int octave = (i % 4 == 3) ? 24 : 12;
                int pitch = root + 12 + octave + degree;
                synth.Note(barStart + i * step + offset, step * 0.55, AudioSynth.MidiToFrequency(pitch), wave, amp,
                    0.003, 0.06, 0.25, 0.14, Math.Sin(i * 1.1) * 0.45, 2600.0, 0.0, 4.0, 0.32);
            }
        }

        static void RenderDrums(AudioSynth synth, double barStart, double beat, Style style, bool fill, double swing, int barIndex)
        {
            // Kick
            foreach (double b in new[] { 0.0, 1.5, 2.0, 3.5 })
            {
                if (style == Style.Store && b == 3.5 && !fill) continue;
                synth.Note(barStart + b * beat, 0.11, 130.0, AudioSynth.Wave.Sine, 0.5, 0.001, 0.09, 0.0, 0.03, 0.0, 0.0, 46.0);
            }

            // Snare / clap
            foreach (double b in new[] { 1.0, 3.0 })
            {
                synth.NoiseBurst(barStart + b * beat, 0.14, 0.24, 6500.0, 900.0, 0.0, 2600.0);
                synth.Note(barStart + b * beat, 0.05, 210.0, AudioSynth.Wave.Triangle, 0.14, 0.001, 0.05, 0.0, 0.02);
            }

            // Hats
            int hats = style == Style.Puzzle ? 16 : 8;
            for (int i = 0; i < hats; i++)
            {
                double offset = i % 2 == 1 ? swing * beat * 0.5 : 0.0;
                double amp = i % 2 == 0 ? 0.09 : 0.055;
                synth.NoiseBurst(barStart + i * beat * 4.0 / hats + offset, 0.035, amp, 12000.0, 7000.0, (i % 2 == 0 ? -0.2 : 0.25));
            }

            if (!fill) return;
            for (int i = 0; i < 4; i++)
                synth.NoiseBurst(barStart + (3.0 + i * 0.25) * beat, 0.07, 0.16, 5200.0, 1200.0, (i - 1.5) * 0.3);
        }

        // ------------------------------------------------------------------ SFX

        static IEnumerable<Clip> Effects(int rate)
        {
            yield return SuctionPop(rate);
            yield return Unload(rate);
            yield return Cash(rate);
            yield return Upgrade(rate);
            yield return Jam(rate);
            yield return Repair(rate);
            yield return Place(rate);
            yield return ClearLine(rate);
            yield return Tap(rate);
            yield return LevelUp(rate);
            yield return Poof(rate);
            yield return Sell(rate);
        }

        static Clip Wrap(string key, AudioSynth synth, int rate, bool loop = false, int channels = 2, float peak = 0.9f)
        {
            synth.Normalize(peak);
            return new Clip { Key = key, Samples = synth.Buffer, SampleRate = rate, Channels = channels, Loop = loop };
        }

        /// <summary>Crisp rubber "plop" as a prop drops into the hole.</summary>
        static Clip SuctionPop(int rate)
        {
            var synth = new AudioSynth(0.26, rate, 1, 11);
            synth.Note(0.0, 0.07, 620.0, AudioSynth.Wave.Sine, 0.55, 0.002, 0.05, 0.0, 0.05, 0.0, 0.0, 180.0);
            synth.NoiseBurst(0.0, 0.05, 0.22, 4200.0, 700.0, 0.0, 1200.0);
            synth.Note(0.02, 0.10, 240.0, AudioSynth.Wave.Triangle, 0.25, 0.002, 0.07, 0.1, 0.08, 0.0, 900.0, 90.0);
            return Wrap("sfx_suction_pop", synth, rate, false, 1);
        }

        /// <summary>Looping pneumatic swoosh while the hole empties into a hopper.</summary>
        static Clip Unload(int rate)
        {
            var synth = new AudioSynth(1.0, rate, 1, 22);
            for (int i = 0; i < 24; i++)
            {
                double t = i / 24.0;
                synth.NoiseBurst(t, 0.09, 0.2 + 0.05 * Math.Sin(t * Math.PI * 4.0), 3400.0, 420.0, 0.0, 5200.0, 0.02);
            }
            synth.Note(0.0, 1.0, 92.0, AudioSynth.Wave.Saw, 0.08, 0.05, 0.2, 0.8, 0.05, 0.0, 300.0);
            synth.MakeSeamless(0.12);
            var trimmed = synth.TrimTail(0.12);
            synth.Normalize(0.72f, trimmed);
            return new Clip { Key = "sfx_unload", Samples = trimmed, SampleRate = rate, Channels = 1, Loop = true };
        }

        /// <summary>Paper rustle plus a register ding.</summary>
        static Clip Cash(int rate)
        {
            var synth = new AudioSynth(0.42, rate, 1, 33);
            for (int i = 0; i < 5; i++)
                synth.NoiseBurst(i * 0.028, 0.05, 0.16, 9000.0, 2600.0, 0.0, 5200.0);
            synth.Note(0.02, 0.22, 1568.0, AudioSynth.Wave.Sine, 0.32, 0.002, 0.2, 0.0, 0.14);
            synth.Note(0.02, 0.22, 2093.0, AudioSynth.Wave.Sine, 0.18, 0.002, 0.18, 0.0, 0.12);
            return Wrap("sfx_cash", synth, rate, false, 1);
        }

        static Clip Upgrade(int rate)
        {
            var synth = new AudioSynth(0.75, rate, 2, 44);
            int[] notes = { 60, 64, 67, 72, 76 };
            for (int i = 0; i < notes.Length; i++)
                synth.Note(i * 0.055, 0.28, AudioSynth.MidiToFrequency(notes[i]), AudioSynth.Wave.Triangle, 0.34,
                    0.004, 0.12, 0.35, 0.26, (i - 2) * 0.22, 5200.0);
            synth.Note(0.0, 0.5, AudioSynth.MidiToFrequency(48), AudioSynth.Wave.Sine, 0.22, 0.01, 0.2, 0.3, 0.2);
            synth.Reverb(0.3);
            return Wrap("sfx_upgrade", synth, rate);
        }

        static Clip Jam(int rate)
        {
            var synth = new AudioSynth(0.6, rate, 1, 55);
            synth.Note(0.0, 0.3, 240.0, AudioSynth.Wave.Square, 0.3, 0.005, 0.1, 0.6, 0.15, 0.0, 1400.0, 90.0);
            synth.NoiseBurst(0.0, 0.18, 0.26, 2200.0, 180.0, 0.0, 700.0);
            synth.Note(0.24, 0.2, 110.0, AudioSynth.Wave.Triangle, 0.3, 0.002, 0.16, 0.0, 0.1, 0.0, 800.0, 62.0);
            return Wrap("sfx_jam", synth, rate, false, 1);
        }

        static Clip Repair(int rate)
        {
            var synth = new AudioSynth(0.7, rate, 2, 66);
            int[] notes = { 64, 71, 76, 83 };
            for (int i = 0; i < notes.Length; i++)
                synth.Note(i * 0.07, 0.32, AudioSynth.MidiToFrequency(notes[i]), AudioSynth.Wave.Sine, 0.3,
                    0.003, 0.18, 0.25, 0.3, (i % 2 == 0 ? -0.25 : 0.25), 7000.0);
            synth.Reverb(0.35);
            return Wrap("sfx_repair", synth, rate);
        }

        static Clip Place(int rate)
        {
            var synth = new AudioSynth(0.16, rate, 1, 77);
            synth.Note(0.0, 0.05, 880.0, AudioSynth.Wave.Triangle, 0.34, 0.001, 0.04, 0.0, 0.05, 0.0, 6000.0, 620.0);
            synth.NoiseBurst(0.0, 0.03, 0.12, 8000.0, 2200.0);
            return Wrap("sfx_puzzle_place", synth, rate, false, 1);
        }

        static Clip ClearLine(int rate)
        {
            var synth = new AudioSynth(0.62, rate, 2, 88);
            int[] notes = { 72, 76, 79, 84, 88 };
            for (int i = 0; i < notes.Length; i++)
                synth.Note(i * 0.042, 0.2, AudioSynth.MidiToFrequency(notes[i]), AudioSynth.Wave.Sine, 0.3,
                    0.002, 0.12, 0.2, 0.2, Math.Sin(i) * 0.4, 9000.0);
            synth.NoiseBurst(0.0, 0.16, 0.14, 11000.0, 4200.0);
            synth.Reverb(0.32);
            return Wrap("sfx_puzzle_clear", synth, rate);
        }

        static Clip Tap(int rate)
        {
            var synth = new AudioSynth(0.1, rate, 1, 99);
            synth.Note(0.0, 0.03, 1320.0, AudioSynth.Wave.Sine, 0.3, 0.001, 0.025, 0.0, 0.03);
            synth.NoiseBurst(0.0, 0.02, 0.08, 9000.0, 3000.0);
            return Wrap("sfx_ui_tap", synth, rate, false, 1, 0.7f);
        }

        static Clip LevelUp(int rate)
        {
            var synth = new AudioSynth(1.1, rate, 2, 101);
            int[] notes = { 60, 67, 72, 76, 79, 84 };
            for (int i = 0; i < notes.Length; i++)
                synth.Note(i * 0.075, 0.4, AudioSynth.MidiToFrequency(notes[i]), AudioSynth.Wave.Triangle, 0.3,
                    0.004, 0.18, 0.4, 0.35, (i - 2.5) * 0.15, 6000.0, 0.0, 5.0);
            synth.Note(0.0, 0.8, AudioSynth.MidiToFrequency(36), AudioSynth.Wave.Sine, 0.22, 0.01, 0.3, 0.4, 0.3);
            synth.NoiseBurst(0.36, 0.3, 0.1, 12000.0, 5000.0);
            synth.Reverb(0.4);
            return Wrap("sfx_level_up", synth, rate);
        }

        static Clip Poof(int rate)
        {
            var synth = new AudioSynth(0.45, rate, 2, 111);
            synth.NoiseBurst(0.0, 0.32, 0.4, 7000.0, 300.0, 0.0, 900.0, 0.004);
            synth.Note(0.0, 0.2, 320.0, AudioSynth.Wave.Sine, 0.18, 0.002, 0.15, 0.0, 0.12, 0.0, 0.0, 120.0);
            return Wrap("sfx_poof", synth, rate);
        }

        static Clip Sell(int rate)
        {
            var synth = new AudioSynth(0.34, rate, 1, 121);
            synth.Note(0.0, 0.16, 1046.0, AudioSynth.Wave.Sine, 0.3, 0.002, 0.12, 0.1, 0.12);
            synth.Note(0.05, 0.16, 1568.0, AudioSynth.Wave.Sine, 0.22, 0.002, 0.12, 0.1, 0.12);
            return Wrap("sfx_sell", synth, rate, false, 1);
        }
    }
}
