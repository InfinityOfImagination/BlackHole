using System;
using System.Collections;
using UnityEngine;
using VoidMart.Core;

namespace VoidMart.UI
{
    /// <summary>Coroutine tweens for menu motion — the spec's EaseOutBack / EaseOutElastic / EaseInOutQuad set.</summary>
    public static class UITween
    {
        public static IEnumerator Value(float from, float to, float duration, Ease ease, Action<float> apply, Action done = null)
        {
            if (duration <= 0f)
            {
                apply?.Invoke(to);
                done?.Invoke();
                yield break;
            }

            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                apply?.Invoke(Mathf.LerpUnclamped(from, to, Easing.Evaluate(ease, time / duration)));
                yield return null;
            }
            apply?.Invoke(to);
            done?.Invoke();
        }

        public static IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration, Ease ease, Action done = null)
        {
            if (target == null) yield break;
            float time = 0f;
            target.anchoredPosition = from;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Easing.Evaluate(ease, time / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                yield return null;
            }
            target.anchoredPosition = to;
            done?.Invoke();
        }

        public static IEnumerator Scale(Transform target, float from, float to, float duration, Ease ease, Action done = null)
        {
            if (target == null) yield break;
            float time = 0f;
            target.localScale = Vector3.one * from;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Easing.Evaluate(ease, time / duration);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, t);
                yield return null;
            }
            target.localScale = Vector3.one * to;
            done?.Invoke();
        }

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration, Ease ease, Action done = null)
        {
            if (group == null) yield break;
            float time = 0f;
            group.alpha = from;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                group.alpha = Mathf.LerpUnclamped(from, to, Easing.Evaluate(ease, time / duration));
                yield return null;
            }
            group.alpha = to;
            done?.Invoke();
        }

        /// <summary>Quick squash-and-stretch punch used on HUD counters when they change.</summary>
        public static IEnumerator Punch(Transform target, float amount, float duration)
        {
            if (target == null) yield break;
            float time = 0f;
            Vector3 baseScale = Vector3.one;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                float wave = Mathf.Sin(t * Mathf.PI) * amount;
                target.localScale = baseScale * (1f + wave);
                yield return null;
            }
            target.localScale = baseScale;
        }
    }
}
