using UnityEngine;
using VoidMart.Data;

namespace VoidMart.Core
{
    public enum HapticStrength { Light, Medium, Heavy }

    /// <summary>Light wrapper so gameplay can ask for a buzz without platform ifdefs everywhere.</summary>
    public static class Haptics
    {
        static float s_LastTime;
        const float MinInterval = 0.04f;

        public static bool Enabled { get; set; } = true;

        public static void Play(HapticStrength strength = HapticStrength.Light)
        {
            if (!Enabled) return;
            var save = ServiceLocator.Get<Services.SaveService>();
            if (save?.Data != null && !save.Data.settings.hapticsEnabled) return;
            if (Time.unscaledTime - s_LastTime < MinInterval) return;
            s_LastTime = Time.unscaledTime;

#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }

        public static void ApplyFrom(UIConfig ui)
        {
            if (ui != null) Enabled = ui.hapticsEnabled;
        }
    }
}
