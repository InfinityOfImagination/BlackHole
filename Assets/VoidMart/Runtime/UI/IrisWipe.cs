using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.UI
{
    /// <summary>
    /// Screen-space circular iris wipe centred on the player's hole, following the spec curve
    /// CutoutRadius(t) = R_max * (1 - t^3).
    /// </summary>
    public class IrisWipe : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] Image m_Mask;
        [SerializeField] RectTransform m_MaskRect;
        [SerializeField] CanvasGroup m_Group;
        [SerializeField] Canvas m_Canvas;

        float m_MaxRadius = 1600f;

        public bool Busy { get; private set; }

        public void Bind(GameConfig config, Canvas canvas, Image mask, CanvasGroup group)
        {
            m_Config = config;
            m_Canvas = canvas;
            m_Mask = mask;
            m_MaskRect = mask != null ? mask.rectTransform : null;
            m_Group = group;
        }

        void Awake()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            if (m_MaskRect == null && m_Mask != null) m_MaskRect = m_Mask.rectTransform;
            if (m_Group != null) m_Group.alpha = 0f;
        }

        float ComputeMaxRadius()
        {
            var ui = m_Config != null ? m_Config.ui : null;
            float reference = ui != null ? Mathf.Max(ui.referenceWidth, ui.referenceHeight) : 1920f;
            float scale = ui != null ? ui.irisMaxRadiusScale : 1.45f;
            return reference * scale;
        }

        /// <summary>Closes the iris over the player, runs an action, then opens it again.</summary>
        public void Transition(Vector2 screenCentre, Action onCovered, float holdSeconds = 0f)
        {
            if (Busy) return;
            StartCoroutine(Run(screenCentre, onCovered, holdSeconds));
        }

        public IEnumerator Run(Vector2 screenCentre, Action onCovered, float holdSeconds)
        {
            Busy = true;
            m_MaxRadius = ComputeMaxRadius();

            if (m_Group != null) m_Group.alpha = 1f;
            if (m_MaskRect != null)
            {
                var camera = m_Canvas != null && m_Canvas.renderMode != RenderMode.ScreenSpaceOverlay ? m_Canvas.worldCamera : null;
                var parent = m_MaskRect.parent as RectTransform;
                if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenCentre, camera, out var local))
                    m_MaskRect.anchoredPosition = local;
            }

            float duration = m_Config != null ? m_Config.ui.irisSeconds : 0.75f;
            yield return Animate(1f, 0f, duration);

            onCovered?.Invoke();
            if (holdSeconds > 0f) yield return new WaitForSecondsRealtime(holdSeconds);

            yield return Animate(0f, 1f, duration);
            if (m_Group != null) m_Group.alpha = 0f;
            Busy = false;
        }

        IEnumerator Animate(float fromOpen, float toOpen, float duration)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                float openness = Mathf.Lerp(fromOpen, toOpen, t);
                // Radius(t) = Rmax * (1 - t^3): fast at first, easing into the close.
                float radius = m_MaxRadius * (1f - Mathf.Pow(1f - openness, 3f));
                SetRadius(radius);
                yield return null;
            }
            SetRadius(m_MaxRadius * (1f - Mathf.Pow(1f - toOpen, 3f)));
        }

        void SetRadius(float radius)
        {
            if (m_MaskRect == null) return;
            m_MaskRect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
        }
    }
}
