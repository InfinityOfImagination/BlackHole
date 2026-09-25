using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoidMart.Data;

namespace VoidMart.UI
{
    /// <summary>
    /// Void Mart's text renderer.  It draws glyph quads from the procedurally generated
    /// <see cref="VMFontAsset"/> atlas, so the game ships a bespoke display face with zero font
    /// imports and no TextMeshPro setup step — and the weight/rounding of that face stays editable
    /// from the Master Tool.
    /// </summary>
    [AddComponentMenu("Void Mart/VM Text")]
    [RequireComponent(typeof(CanvasRenderer))]
    public class VMText : MaskableGraphic
    {
        [SerializeField] VMFontAsset m_Font;
        [SerializeField, TextArea(1, 4)] string m_Text = "VOID MART";
        [SerializeField] float m_FontSize = 48f;
        [SerializeField] float m_Tracking = 0.04f;
        [SerializeField] float m_LineSpacing = 1f;
        [SerializeField] TextAnchor m_Alignment = TextAnchor.MiddleCenter;
        [SerializeField] bool m_Uppercase = true;
        [SerializeField] bool m_AutoShrink = true;
        [SerializeField] float m_MinFontSize = 12f;
        [SerializeField] Vector2 m_ShadowOffset = Vector2.zero;
        [SerializeField] Color m_ShadowColor = new Color(0f, 0f, 0f, 0.35f);

        static readonly UIVertex[] s_Quad = new UIVertex[4];
        static readonly List<string> s_Lines = new List<string>(4);

        public VMFontAsset Font
        {
            get => m_Font;
            set { m_Font = value; SetVerticesDirty(); SetMaterialDirty(); }
        }

        public string Text
        {
            get => m_Text;
            set
            {
                if (m_Text == value) return;
                m_Text = value;
                SetVerticesDirty();
            }
        }

        public float FontSize
        {
            get => m_FontSize;
            set { m_FontSize = value; SetVerticesDirty(); }
        }

        public TextAnchor Alignment
        {
            get => m_Alignment;
            set { m_Alignment = value; SetVerticesDirty(); }
        }

        public override Texture mainTexture => m_Font != null && m_Font.atlas != null ? m_Font.atlas : whiteTexture;

        public void SetText(string value) => Text = value;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (m_Font == null || string.IsNullOrEmpty(m_Text)) return;

            string source = m_Uppercase ? m_Text.ToUpperInvariant() : m_Text;
            SplitLines(source);

            var rect = GetPixelAdjustedRect();
            float size = m_FontSize;

            if (m_AutoShrink && rect.width > 1f)
            {
                float widest = 0f;
                for (int i = 0; i < s_Lines.Count; i++)
                    widest = Mathf.Max(widest, m_Font.Measure(s_Lines[i], m_Tracking));
                if (widest > 0.0001f)
                {
                    float maxSize = rect.width / widest;
                    if (maxSize < size) size = Mathf.Max(m_MinFontSize, maxSize);
                }
                float lineBlock = m_Font.lineHeight * m_LineSpacing * s_Lines.Count;
                if (lineBlock > 0.0001f && rect.height > 1f)
                {
                    float maxByHeight = rect.height / lineBlock;
                    if (maxByHeight < size) size = Mathf.Max(m_MinFontSize, maxByHeight);
                }
            }

            float lineStep = m_Font.lineHeight * m_LineSpacing * size;
            float blockHeight = lineStep * s_Lines.Count;

            float startY;
            switch (m_Alignment)
            {
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    startY = rect.yMax - m_Font.ascender * size;
                    break;
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    startY = rect.yMin + blockHeight - m_Font.ascender * size;
                    break;
                default:
                    startY = rect.center.y + blockHeight * 0.5f - m_Font.ascender * size;
                    break;
            }

            bool hasShadow = m_ShadowOffset.sqrMagnitude > 0.0001f && m_ShadowColor.a > 0.003f;

            for (int pass = hasShadow ? 0 : 1; pass < 2; pass++)
            {
                Vector2 offset = pass == 0 ? m_ShadowOffset : Vector2.zero;
                Color32 tint = pass == 0 ? m_ShadowColor : color;

                for (int line = 0; line < s_Lines.Count; line++)
                {
                    string content = s_Lines[line];
                    float lineWidth = m_Font.Measure(content, m_Tracking) * size;

                    float penX;
                    switch (m_Alignment)
                    {
                        case TextAnchor.UpperRight:
                        case TextAnchor.MiddleRight:
                        case TextAnchor.LowerRight:
                            penX = rect.xMax - lineWidth;
                            break;
                        case TextAnchor.UpperLeft:
                        case TextAnchor.MiddleLeft:
                        case TextAnchor.LowerLeft:
                            penX = rect.xMin;
                            break;
                        default:
                            penX = rect.center.x - lineWidth * 0.5f;
                            break;
                    }

                    float baseline = startY - line * lineStep;
                    EmitLine(vh, content, penX + offset.x, baseline + offset.y, size, tint);
                }
            }
        }

        void EmitLine(VertexHelper vh, string line, float penX, float baseline, float size, Color32 tint)
        {
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == ' ')
                {
                    penX += (m_Font.spaceAdvance + m_Tracking) * size;
                    continue;
                }

                if (!m_Font.TryGetGlyph(c, out var glyph))
                {
                    penX += (m_Font.spaceAdvance + m_Tracking) * size;
                    continue;
                }

                float x0 = penX + glyph.bearing.x * size;
                float y0 = baseline + glyph.bearing.y * size;
                float x1 = x0 + glyph.size.x * size;
                float y1 = y0 + glyph.size.y * size;

                var uv = glyph.uv;
                s_Quad[0].position = new Vector3(x0, y0);
                s_Quad[1].position = new Vector3(x0, y1);
                s_Quad[2].position = new Vector3(x1, y1);
                s_Quad[3].position = new Vector3(x1, y0);
                s_Quad[0].uv0 = new Vector4(uv.xMin, uv.yMin);
                s_Quad[1].uv0 = new Vector4(uv.xMin, uv.yMax);
                s_Quad[2].uv0 = new Vector4(uv.xMax, uv.yMax);
                s_Quad[3].uv0 = new Vector4(uv.xMax, uv.yMin);
                for (int v = 0; v < 4; v++) s_Quad[v].color = tint;

                vh.AddUIVertexQuad(s_Quad);
                penX += (glyph.advance + m_Tracking) * size;
            }
        }

        static void SplitLines(string source)
        {
            s_Lines.Clear();
            int start = 0;
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] != '\n') continue;
                s_Lines.Add(source.Substring(start, i - start));
                start = i + 1;
            }
            s_Lines.Add(source.Substring(start));
        }

        /// <summary>Width of the current string in pixels at the current size.</summary>
        public float PreferredWidth => m_Font == null ? 0f : m_Font.Measure(m_Uppercase ? m_Text.ToUpperInvariant() : m_Text, m_Tracking) * m_FontSize;

        public void SetShadow(Vector2 offset, Color shadowColor)
        {
            m_ShadowOffset = offset;
            m_ShadowColor = shadowColor;
            SetVerticesDirty();
        }

        public void Setup(VMFontAsset font, string content, float fontSize, Color textColor, TextAnchor alignment)
        {
            m_Font = font;
            m_Text = content;
            m_FontSize = fontSize;
            color = textColor;
            m_Alignment = alignment;
            SetAllDirty();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetAllDirty();
        }
#endif
    }
}
