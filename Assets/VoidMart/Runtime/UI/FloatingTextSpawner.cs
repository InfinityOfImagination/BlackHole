using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.UI
{
    /// <summary>
    /// Screen-space "+$12" pops anchored to world positions.  Uses a fixed ring of pre-created
    /// labels so nothing is instantiated while the game is running.
    /// </summary>
    public class FloatingTextSpawner : MonoBehaviour
    {
        struct Entry
        {
            public VMText Label;
            public RectTransform Rect;
            public Vector3 WorldPosition;
            public float Timer;
        }

        [SerializeField] GameConfig m_Config;
        [SerializeField] RectTransform m_Root;
        [SerializeField] Canvas m_Canvas;
        [SerializeField] int m_Capacity = 12;

        Entry[] m_Entries;
        int m_Cursor;
        Camera m_Camera;

        public void Bind(GameConfig config, RectTransform root, Canvas canvas, VMText[] labels)
        {
            m_Config = config;
            m_Root = root;
            m_Canvas = canvas;
            m_Entries = new Entry[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                m_Entries[i] = new Entry
                {
                    Label = labels[i],
                    Rect = labels[i].rectTransform,
                    Timer = 0f
                };
                labels[i].gameObject.SetActive(false);
            }
            m_Capacity = labels.Length;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            var bus = m_Config != null ? m_Config.events : null;
            if (bus != null)
            {
                bus.productSold?.Register(OnSale);
                bus.propSwallowed?.Register(OnSwallow);
            }
        }

        void OnDestroy()
        {
            var bus = m_Config != null ? m_Config.events : null;
            if (bus != null)
            {
                bus.productSold?.Unregister(OnSale);
                bus.propSwallowed?.Unregister(OnSwallow);
            }
        }

        void OnSale(SaleInfo info) => Spawn("+" + NumberFormat.Money(info.revenue), info.worldPosition, m_Config.theme.mint);

        void OnSwallow(SwallowInfo info)
        {
            // Only celebrate real chains, and only every fifth bite, or the screen fills up.
            if (info.streak < 5 || info.streak % 5 != 0) return;
            Spawn("x" + info.streak, info.worldPosition, m_Config.theme.neonCyan);
        }

        public void Spawn(string text, Vector3 worldPosition, Color tint)
        {
            if (m_Entries == null || m_Entries.Length == 0) return;
            int index = m_Cursor;
            m_Cursor = (m_Cursor + 1) % m_Entries.Length;

            ref var entry = ref m_Entries[index];
            entry.WorldPosition = worldPosition;
            entry.Timer = m_Config != null ? m_Config.ui.floatingTextSeconds : 1.1f;
            entry.Label.SetText(text);
            entry.Label.color = tint;
            entry.Label.gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (m_Entries == null) return;
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Camera == null) return;

            float lifetime = m_Config != null ? m_Config.ui.floatingTextSeconds : 1.1f;
            float rise = m_Config != null ? m_Config.ui.floatingTextRise : 2.4f;
            float deltaTime = Time.unscaledDeltaTime;

            for (int i = 0; i < m_Entries.Length; i++)
            {
                ref var entry = ref m_Entries[i];
                if (entry.Timer <= 0f) continue;

                entry.Timer -= deltaTime;
                if (entry.Timer <= 0f)
                {
                    entry.Label.gameObject.SetActive(false);
                    continue;
                }

                float t = 1f - entry.Timer / Mathf.Max(0.01f, lifetime);
                Vector3 world = entry.WorldPosition + Vector3.up * (rise * t);
                Vector3 screen = m_Camera.WorldToScreenPoint(world);
                if (screen.z < 0f)
                {
                    entry.Label.gameObject.SetActive(false);
                    entry.Timer = 0f;
                    continue;
                }

                var camera = m_Canvas != null && m_Canvas.renderMode != RenderMode.ScreenSpaceOverlay ? m_Canvas.worldCamera : null;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Root, screen, camera, out var local))
                    entry.Rect.anchoredPosition = local;

                var color = entry.Label.color;
                color.a = Mathf.Clamp01(1f - Mathf.Pow(t, 3f));
                entry.Label.color = color;
                entry.Rect.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.05f, Easing.Evaluate(Ease.OutBack, Mathf.Clamp01(t * 4f)));
            }
        }
    }
}
