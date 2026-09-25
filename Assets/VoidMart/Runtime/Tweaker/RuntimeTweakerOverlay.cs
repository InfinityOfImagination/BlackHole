using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;

namespace VoidMart.Tweaker
{
    /// <summary>
    /// The in-game half of the universal value tweaker.  Opened with a 3-finger double tap on
    /// device (or F1 in the editor), it lists every registered [Tweakable] grouped by category and
    /// writes changes straight into the live objects.
    /// </summary>
    public class RuntimeTweakerOverlay : MonoBehaviour
    {
        [SerializeField] bool m_EnableInReleaseBuilds;
        [SerializeField] float m_DoubleTapWindow = 0.45f;

        bool m_Visible;
        string m_ActiveCategory;
        Vector2 m_Scroll;
        float m_LastThreeFingerTime = -10f;
        bool m_ThreeFingersDown;

        GUIStyle m_Panel, m_Header, m_Label, m_Button, m_ActiveButton;
        bool m_StylesReady;

        bool Allowed => m_EnableInReleaseBuilds || Debug.isDebugBuild || Application.isEditor;

        void Update()
        {
            if (!Allowed) return;

            int touches = InputProxy.TouchCount;
            if (touches >= 3)
            {
                if (!m_ThreeFingersDown)
                {
                    m_ThreeFingersDown = true;
                    float now = Time.unscaledTime;
                    if (now - m_LastThreeFingerTime <= m_DoubleTapWindow)
                    {
                        Toggle();
                        m_LastThreeFingerTime = -10f;
                    }
                    else
                    {
                        m_LastThreeFingerTime = now;
                    }
                }
            }
            else if (touches == 0)
            {
                m_ThreeFingersDown = false;
            }

            if (InputProxy.FunctionKeyDown(1)) Toggle();
        }

        public void Toggle()
        {
            m_Visible = !m_Visible;
            if (m_Visible && string.IsNullOrEmpty(m_ActiveCategory))
            {
                foreach (var kv in RuntimeTweakerService.Categories) { m_ActiveCategory = kv.Key; break; }
            }
        }

        void BuildStyles()
        {
            if (m_StylesReady) return;
            m_StylesReady = true;

            m_Panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 12, 12) };
            m_Panel.normal.background = SolidTexture(new Color(0.043f, 0.039f, 0.078f, 0.94f));

            m_Header = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            m_Header.normal.textColor = new Color(0f, 0.898f, 1f);

            m_Label = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            m_Label.normal.textColor = new Color(0.9f, 0.92f, 0.96f);

            m_Button = new GUIStyle(GUI.skin.button) { fontSize = 12, fixedHeight = 26 };
            m_ActiveButton = new GUIStyle(m_Button);
            m_ActiveButton.normal.background = SolidTexture(new Color(0.263f, 0.220f, 0.792f, 1f));
            m_ActiveButton.normal.textColor = Color.white;
        }

        static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        void OnGUI()
        {
            if (!m_Visible || !Allowed) return;
            BuildStyles();

            float width = Mathf.Min(Screen.width * 0.92f, 720f);
            float height = Mathf.Min(Screen.height * 0.8f, 900f);
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            GUILayout.BeginArea(rect, m_Panel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("VOID MART — RUNTIME TWEAKER", m_Header);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset", m_Button, GUILayout.Width(70))) RuntimeTweakerService.ResetAll();
            if (GUILayout.Button("Close", m_Button, GUILayout.Width(70))) m_Visible = false;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            DrawCategoryTabs();
            GUILayout.Space(6);

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);
            if (RuntimeTweakerService.Categories.TryGetValue(m_ActiveCategory ?? "", out var entries))
            {
                for (int i = 0; i < entries.Count; i++) DrawEntry(entries[i]);
            }
            else
            {
                GUILayout.Label("No tweakables registered yet.", m_Label);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DrawCategoryTabs()
        {
            GUILayout.BeginHorizontal();
            int column = 0;
            foreach (var kv in RuntimeTweakerService.Categories)
            {
                if (column > 0 && column % 4 == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
                var style = kv.Key == m_ActiveCategory ? m_ActiveButton : m_Button;
                if (GUILayout.Button(kv.Key + " (" + kv.Value.Count + ")", style)) m_ActiveCategory = kv.Key;
                column++;
            }
            GUILayout.EndHorizontal();
        }

        void DrawEntry(RuntimeTweakerService.Entry entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(entry.DisplayName, m_Label, GUILayout.Width(190));

            if (entry.IsBool)
            {
                bool current = entry.GetFloat() > 0.5f;
                bool next = GUILayout.Toggle(current, current ? " on" : " off", GUILayout.Width(80));
                if (next != current) entry.SetFloat(next ? 1f : 0f);
            }
            else if (entry.IsNumeric)
            {
                float value = entry.GetFloat();
                float next = GUILayout.HorizontalSlider(value, entry.Attribute.Min, entry.Attribute.Max);
                if (!Mathf.Approximately(next, value)) entry.SetFloat(next);
                GUILayout.Label(next.ToString("0.###"), m_Label, GUILayout.Width(70));
            }
            else
            {
                GUILayout.Label(entry.GetValue()?.ToString() ?? "null", m_Label);
            }
            GUILayout.EndHorizontal();
        }
    }
}
