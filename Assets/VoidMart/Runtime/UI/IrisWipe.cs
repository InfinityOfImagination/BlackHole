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
        static readonly int CenterId = Shader.PropertyToID("_Center");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");

        [SerializeField] GameConfig m_Config;
        [SerializeField] Image m_Mask;
        [SerializeField] RectTransform m_MaskRect;
        [SerializeField] CanvasGroup m_Group;
        [SerializeField] Canvas m_Canvas;

        float m_MaxRadius = 1600f;
        Material m_Material;
        Vector2 m_CentrePixels;

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

            // The wipe is a full-screen quad with a shader-driven cut-out, so the hole can be
            // any size without the covering geometry ever running out.
            if (m_Mask != null && m_Mask.material != null)
            {
                m_Material = new Material(m_Mask.material);
                m_Mask.material = m_Material;
            }
        }

        void OnDestroy()
        {
            if (m_Material != null) Destroy(m_Material);
        }

        float ComputeMaxRadius()
        {
            var ui = m_Config != null ? m_Config.ui : null;
            float scale = ui != null ? ui.irisMaxRadiusScale : 1.45f;
            return Mathf.Max(Screen.width, Screen.height) * scale;
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
            m_CentrePixels = screenCentre;
            if (m_Material != null) m_Material.SetVector(CenterId, new Vector4(screenCentre.x, screenCentre.y, 0f, 0f));

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
            if (m_Material != null)
            {
                m_Material.SetFloat(RadiusId, radius);
                m_Material.SetVector(CenterId, new Vector4(m_CentrePixels.x, m_CentrePixels.y, 0f, 0f));
            }
            else if (m_MaskRect != null)
            {
                m_MaskRect.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            }
        }

        /// <summary>Convenience for callers that just want the wipe centred on the player.</summary>
        public Vector2 PlayerScreenPoint()
        {
            var hole = ServiceLocator.Get<Gameplay.PlayerHoleController>();
            var camera = Camera.main;
            if (hole == null || camera == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector3 point = camera.WorldToScreenPoint(hole.transform.position);
            return new Vector2(point.x, point.y);
        }
    }
}
