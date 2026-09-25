using System;
using System.Text;

namespace VoidMart.Core
{
    /// <summary>
    /// Idle-game number formatting ("12.4K", "3.09M", "1.25aa") without per-frame garbage:
    /// results are produced through a shared <see cref="StringBuilder"/> and short values are
    /// served from a small cache because the HUD re-formats the same integers constantly.
    /// </summary>
    public static class NumberFormat
    {
        static readonly string[] s_Suffixes =
        {
            "", "K", "M", "B", "T", "aa", "ab", "ac", "ad", "ae", "af", "ag", "ah", "ai", "aj"
        };

        static readonly StringBuilder s_Builder = new StringBuilder(24);
        static readonly string[] s_SmallCache = new string[1024];

        public static string Compact(double value)
        {
            bool negative = value < 0d;
            if (negative) value = -value;

            if (value < 1000d)
            {
                int whole = (int)value;
                if (!negative && whole < s_SmallCache.Length && Math.Abs(value - whole) < 0.001d)
                {
                    return s_SmallCache[whole] ?? (s_SmallCache[whole] = whole.ToString());
                }
                return Build(negative, value < 10d ? Round(value, 1) : Math.Floor(value), "");
            }

            int tier = 0;
            while (value >= 1000d && tier < s_Suffixes.Length - 1)
            {
                value /= 1000d;
                tier++;
            }

            double rounded = value >= 100d ? Round(value, 1) : Round(value, 2);
            return Build(negative, rounded, s_Suffixes[tier]);
        }

        static double Round(double v, int digits) => Math.Round(v, digits, MidpointRounding.AwayFromZero);

        static string Build(bool negative, double value, string suffix)
        {
            lock (s_Builder)
            {
                s_Builder.Length = 0;
                if (negative) s_Builder.Append('-');
                if (Math.Abs(value - Math.Floor(value)) < 0.0001d)
                    s_Builder.Append(((long)value).ToString());
                else
                    s_Builder.Append(value.ToString("0.##"));
                s_Builder.Append(suffix);
                return s_Builder.ToString();
            }
        }

        /// <summary>"$12.4K" style label for money.</summary>
        public static string Money(double value) => "$" + Compact(value);

        /// <summary>mm:ss for timers.</summary>
        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = (int)seconds;
            int m = total / 60;
            int s = total % 60;
            lock (s_Builder)
            {
                s_Builder.Length = 0;
                if (m < 10) s_Builder.Append('0');
                s_Builder.Append(m).Append(':');
                if (s < 10) s_Builder.Append('0');
                s_Builder.Append(s);
                return s_Builder.ToString();
            }
        }

        public static string Percent(float normalized) => Math.Round(normalized * 100f) + "%";
    }
}
