using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>
    /// The exact easing set the UX spec calls for: modals slide up on EaseOutBack, the puzzle
    /// overlay pops with EaseOutElastic, settings cross-fade on EaseInOutQuad.
    /// </summary>
    public enum Ease
    {
        Linear,
        InQuad, OutQuad, InOutQuad,
        InCubic, OutCubic, InOutCubic,
        OutQuint,
        InBack, OutBack, InOutBack,
        OutElastic,
        OutBounce,
        InOutSine
    }

    public static class Easing
    {
        const float Back = 1.70158f;

        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.InCubic: return t * t * t;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutCubic: return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                case Ease.OutQuint: return 1f - Mathf.Pow(1f - t, 5f);
                case Ease.InBack: return (Back + 1f) * t * t * t - Back * t * t;
                case Ease.OutBack:
                {
                    float p = t - 1f;
                    return 1f + (Back + 1f) * p * p * p + Back * p * p;
                }
                case Ease.InOutBack:
                {
                    const float c2 = Back * 1.525f;
                    return t < 0.5f
                        ? Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) * 0.5f;
                }
                case Ease.OutElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c4 = 2f * Mathf.PI / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case Ease.OutBounce: return OutBounce(t);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                default: return t;
            }
        }

        static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        /// <summary>Frame-rate independent exponential smoothing.</summary>
        public static float Damp(float current, float target, float sharpness, float deltaTime)
            => Mathf.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));

        public static Vector3 Damp(Vector3 current, Vector3 target, float sharpness, float deltaTime)
            => Vector3.Lerp(current, target, 1f - Mathf.Exp(-sharpness * deltaTime));

        /// <summary>Quadratic Bezier used for the cash bills arcing toward a pad.</summary>
        public static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }
    }
}
