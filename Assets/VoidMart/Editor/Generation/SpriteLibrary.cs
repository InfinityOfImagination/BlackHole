using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Data;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Every 2D asset: panels, candy buttons, gauges, the joystick, the wordmark and the icon set.
    /// Icons are drawn as white silhouettes so the UI can tint them from the theme palette.
    /// </summary>
    public static class SpriteLibrary
    {
        public class Generated
        {
            public string Key;
            public TexturePainter Painter;
            public Vector4 Border;      // left, bottom, right, top (9-slice)
            public float PixelsPerUnit = 100f;
        }

        public static List<Generated> BuildAll(ThemeConfig theme, ArtConfig art)
        {
            var list = new List<Generated>(48);

            list.Add(White());
            list.Add(Panel("ui_panel", 160, 160, 34, theme.paper, Shade(theme.paper, -0.06f), art));
            list.Add(Panel("ui_panel_dark", 160, 160, 34, Shade(theme.voidIndigo, 0.18f), theme.voidIndigo, art));
            list.Add(Panel("ui_card", 160, 120, 26, Color.white, new Color(0.94f, 0.95f, 0.98f), art));
            list.Add(Panel("ui_sheet", 200, 200, 44, theme.paper, Shade(theme.paper, -0.08f), art));

            list.Add(CandyButton("ui_button_primary", theme.electricViolet, theme.neonCyan, art));
            list.Add(CandyButton("ui_button_action", theme.mint, theme.neonCyan, art));
            list.Add(CandyButton("ui_button_warn", theme.coral, theme.sunsetAmber, art));
            list.Add(CandyButton("ui_button_neutral", new Color(0.52f, 0.55f, 0.66f), new Color(0.72f, 0.76f, 0.85f), art));

            list.Add(Pill("ui_pill", theme.voidIndigo, 0.86f));
            list.Add(Pill("ui_pill_light", theme.paper, 0.95f));
            list.Add(Circle("ui_circle", Color.white));
            list.Add(SoftCircle("ui_glow", Color.white));
            list.Add(RingSprite("ui_ring", 0.16f));
            list.Add(RingSprite("ui_ring_thin", 0.075f));
            list.Add(BarSprite("ui_bar_bg", new Color(1f, 1f, 1f, 0.22f)));
            list.Add(BarSprite("ui_bar_fill", Color.white));
            list.Add(JoystickBase(theme));
            list.Add(JoystickKnob(theme));
            list.Add(PuzzleCellSprite());
            list.Add(Vignette(theme));
            list.Add(Shadow());
            list.Add(Wordmark(theme, art));
            list.Add(HoleBadge(theme));

            AddIcons(list, art);
            return list;
        }

        static Color Shade(Color color, float amount) => new Color(
            Mathf.Clamp01(color.r * (1f + amount)),
            Mathf.Clamp01(color.g * (1f + amount)),
            Mathf.Clamp01(color.b * (1f + amount)), color.a);

        // ---------------------------------------------------------------- panels

        static Generated White()
        {
            var painter = new TexturePainter(8, 8, Color.white);
            return new Generated { Key = "ui_white", Painter = painter, Border = Vector4.zero };
        }

        static Generated Panel(string key, int width, int height, int radius, Color top, Color bottom, ArtConfig art)
        {
            var painter = new TexturePainter(width, height);
            var rect = new Rect(6f, 6f, width - 12f, height - 12f);

            painter.WithShadow(p => p.RoundedRect(rect, radius, Color.white),
                new Vector2(0f, -3f), Mathf.Max(1f, art.panelShadow * 0.35f), new Color(0.05f, 0.04f, 0.12f, 0.35f));
            painter.RoundedRectGradient(rect, radius, top, bottom);
            painter.RoundedRectOutline(rect, radius, Mathf.Max(1.5f, art.panelOutline * 0.5f), new Color(0f, 0f, 0f, 0.12f));
            painter.Grain(art.textureGrain * 0.5f, key.GetHashCode());

            int border = radius + 8;
            return new Generated { Key = key, Painter = painter, Border = new Vector4(border, border, border, border) };
        }

        /// <summary>Chunky mobile-game button: dark lip behind, gradient face, glossy top.</summary>
        static Generated CandyButton(string key, Color baseColor, Color highlight, ArtConfig art)
        {
            const int width = 180, height = 110;
            var painter = new TexturePainter(width, height);

            var lip = new Rect(8f, 6f, width - 16f, height - 22f);
            var face = new Rect(8f, 16f, width - 16f, height - 30f);
            float radius = 26f;

            painter.RoundedRect(lip, radius, Shade(baseColor, -0.45f));
            painter.RoundedRectGradient(face, radius, Color.Lerp(baseColor, highlight, 0.45f), baseColor);
            painter.RoundedRect(new Rect(face.xMin + 12f, face.yMax - 26f, face.width - 24f, 16f), 8f,
                new Color(1f, 1f, 1f, 0.28f));
            painter.RoundedRectOutline(face, radius, 3f, new Color(0f, 0f, 0f, 0.18f));

            int border = 34;
            return new Generated { Key = key, Painter = painter, Border = new Vector4(border, border, border, border) };
        }

        static Generated Pill(string key, Color color, float alpha)
        {
            const int width = 120, height = 64;
            var painter = new TexturePainter(width, height);
            var rect = new Rect(4f, 4f, width - 8f, height - 8f);
            var tinted = color;
            tinted.a = alpha;
            painter.RoundedRect(rect, height * 0.5f - 4f, tinted);
            painter.RoundedRectOutline(rect, height * 0.5f - 4f, 2.5f, new Color(1f, 1f, 1f, 0.16f));
            int border = 30;
            return new Generated { Key = key, Painter = painter, Border = new Vector4(border, border, border, border) };
        }

        static Generated Circle(string key, Color color)
        {
            const int size = 128;
            var painter = new TexturePainter(size, size);
            painter.Circle(new Vector2(size * 0.5f, size * 0.5f), size * 0.5f - 2f, color);
            return new Generated { Key = key, Painter = painter, Border = Vector4.zero };
        }

        static Generated SoftCircle(string key, Color color)
        {
            const int size = 256;
            var painter = new TexturePainter(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            painter.RadialGlow(centre, size * 0.5f, color, new Color(color.r, color.g, color.b, 0f));
            return new Generated { Key = key, Painter = painter, Border = Vector4.zero };
        }

        static Generated RingSprite(string key, float thicknessRatio)
        {
            const int size = 256;
            var painter = new TexturePainter(size, size);
            float radius = size * 0.5f - size * thicknessRatio * 0.5f - 2f;
            painter.Ring(new Vector2(size * 0.5f, size * 0.5f), radius, size * thicknessRatio, Color.white);
            return new Generated { Key = key, Painter = painter, Border = Vector4.zero };
        }

        static Generated BarSprite(string key, Color color)
        {
            const int width = 96, height = 32;
            var painter = new TexturePainter(width, height);
            painter.RoundedRect(new Rect(2f, 2f, width - 4f, height - 4f), height * 0.5f - 2f, color);
            int border = 15;
            return new Generated { Key = key, Painter = painter, Border = new Vector4(border, 0f, border, 0f) };
        }

        static Generated JoystickBase(ThemeConfig theme)
        {
            const int size = 256;
            var painter = new TexturePainter(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            painter.Circle(centre, size * 0.48f, new Color(1f, 1f, 1f, 0.10f));
            painter.Ring(centre, size * 0.44f, 6f, new Color(1f, 1f, 1f, 0.55f));
            painter.Ring(centre, size * 0.30f, 2.5f, new Color(1f, 1f, 1f, 0.22f));
            for (int i = 0; i < 4; i++)
            {
                float angle = Mathf.PI * 0.5f * i + Mathf.PI * 0.25f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                painter.Capsule(centre + direction * size * 0.36f, centre + direction * size * 0.42f, 3f, new Color(1f, 1f, 1f, 0.4f));
            }
            return new Generated { Key = "ui_joystick_base", Painter = painter, Border = Vector4.zero };
        }

        static Generated JoystickKnob(ThemeConfig theme)
        {
            const int size = 160;
            var painter = new TexturePainter(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            painter.Circle(centre + new Vector2(0f, -4f), size * 0.42f, new Color(0f, 0f, 0f, 0.28f));
            painter.Circle(centre, size * 0.42f, new Color(1f, 1f, 1f, 0.92f));
            painter.Circle(centre + new Vector2(0f, size * 0.09f), size * 0.27f, new Color(1f, 1f, 1f, 0.6f));
            painter.Ring(centre, size * 0.42f, 4f, new Color(0f, 0f, 0f, 0.18f));
            return new Generated { Key = "ui_joystick_knob", Painter = painter, Border = Vector4.zero };
        }

        static Generated PuzzleCellSprite()
        {
            const int size = 96;
            var painter = new TexturePainter(size, size);
            var rect = new Rect(3f, 3f, size - 6f, size - 6f);
            painter.RoundedRect(rect, 18f, Color.white);
            painter.RoundedRect(new Rect(rect.xMin + 8f, rect.yMax - 22f, rect.width - 16f, 12f), 6f, new Color(1f, 1f, 1f, 0.35f));
            painter.RoundedRectOutline(rect, 18f, 3f, new Color(0f, 0f, 0f, 0.18f));
            int border = 22;
            return new Generated { Key = "ui_puzzle_cell", Painter = painter, Border = new Vector4(border, border, border, border) };
        }

        static Generated Vignette(ThemeConfig theme)
        {
            const int size = 256;
            var painter = new TexturePainter(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), centre) / (size * 0.5f);
                    float alpha = Mathf.Clamp01((d - 0.55f) / 0.5f);
                    painter.Blend(x, y, new Color(theme.voidInk.r, theme.voidInk.g, theme.voidInk.b, alpha * 0.55f), 1f);
                }
            }
            return new Generated { Key = "ui_vignette", Painter = painter, Border = Vector4.zero };
        }

        static Generated Shadow()
        {
            const int size = 128;
            var painter = new TexturePainter(size, size);
            painter.Circle(new Vector2(size * 0.5f, size * 0.5f), size * 0.42f, new Color(0f, 0f, 0f, 0.5f));
            painter.BlurAlpha(10);
            return new Generated { Key = "ui_shadow", Painter = painter, Border = Vector4.zero };
        }

        static Generated HoleBadge(ThemeConfig theme)
        {
            const int size = 256;
            var painter = new TexturePainter(size, size);
            var centre = new Vector2(size * 0.5f, size * 0.5f);
            painter.Circle(centre, size * 0.44f, theme.voidInk);
            painter.Ring(centre, size * 0.44f, 10f, theme.neonCyan);
            painter.Ring(centre, size * 0.32f, 5f, new Color(theme.hotMagenta.r, theme.hotMagenta.g, theme.hotMagenta.b, 0.8f));
            painter.Circle(centre, size * 0.16f, theme.voidInk);
            return new Generated { Key = "ui_hole_badge", Painter = painter, Border = Vector4.zero };
        }

        static Generated Wordmark(ThemeConfig theme, ArtConfig art)
        {
            const int width = 1024, height = 360;
            var painter = new TexturePainter(width, height);

            float cap = 132f;
            float stroke = cap * 0.26f;
            float topWidth = FontGenerator.MeasureText("VOID", cap, stroke, 0.1f);
            float bottomWidth = FontGenerator.MeasureText("MART", cap, stroke, 0.1f);

            // Shadow pass then the face, so the logo has the same lip as the buttons.
            FontGenerator.DrawText(painter, "VOID", new Vector2((width - topWidth) * 0.5f, height - cap - 46f), cap, stroke,
                new Color(0f, 0f, 0f, 0.35f), 0.1f, art.fontRounding);
            FontGenerator.DrawText(painter, "MART", new Vector2((width - bottomWidth) * 0.5f, 44f), cap, stroke,
                new Color(0f, 0f, 0f, 0.35f), 0.1f, art.fontRounding);

            FontGenerator.DrawText(painter, "VOID", new Vector2((width - topWidth) * 0.5f, height - cap - 38f), cap, stroke,
                theme.paper, 0.1f, art.fontRounding);
            FontGenerator.DrawText(painter, "MART", new Vector2((width - bottomWidth) * 0.5f, 52f), cap, stroke,
                theme.neonCyan, 0.1f, art.fontRounding);

            return new Generated { Key = "ui_wordmark", Painter = painter, Border = Vector4.zero };
        }

        // ----------------------------------------------------------------- icons

        static void AddIcons(List<Generated> list, ArtConfig art)
        {
            int size = Mathf.Clamp(art.iconSize, 64, 512);
            list.Add(Icon("icon_cash", size, IconCash));
            list.Add(Icon("icon_gem", size, IconGem));
            list.Add(Icon("icon_bolt", size, IconBolt));
            list.Add(Icon("icon_gear", size, IconGear));
            list.Add(Icon("icon_close", size, IconClose));
            list.Add(Icon("icon_lock", size, IconLock));
            list.Add(Icon("icon_star", size, IconStar));
            list.Add(Icon("icon_speed", size, IconSpeed));
            list.Add(Icon("icon_capacity", size, IconCapacity));
            list.Add(Icon("icon_magnet", size, IconMagnet));
            list.Add(Icon("icon_robot", size, IconRobot));
            list.Add(Icon("icon_price", size, IconPrice));
            list.Add(Icon("icon_machine", size, IconMachine));
            list.Add(Icon("icon_shopper", size, IconShopper));
            list.Add(Icon("icon_offline", size, IconOffline));
            list.Add(Icon("icon_box", size, IconBox));
            list.Add(Icon("icon_wrench", size, IconWrench));
            list.Add(Icon("icon_plus", size, IconPlus));
            list.Add(Icon("icon_check", size, IconCheck));
            list.Add(Icon("icon_play", size, IconPlay));
            list.Add(Icon("icon_music", size, IconMusic));
            list.Add(Icon("icon_hole", size, IconHole));
        }

        static Generated Icon(string key, int size, Action<TexturePainter, float> draw)
        {
            var painter = new TexturePainter(size, size);
            draw(painter, size);
            return new Generated { Key = key, Painter = painter, Border = Vector4.zero };
        }

        static readonly Color Ink = Color.white;

        static void IconCash(TexturePainter p, float s)
        {
            // A banknote with the display font's own dollar sign knocked out of it.
            p.RoundedRect(new Rect(s * 0.08f, s * 0.26f, s * 0.84f, s * 0.48f), s * 0.08f, Ink);

            var cut = new TexturePainter(p.Width, p.Height);
            float cap = s * 0.30f;
            float stroke = cap * 0.30f;
            float width = FontGenerator.MeasureText("$", cap, stroke, 0f);
            FontGenerator.DrawText(cut, "$", new Vector2((s - width) * 0.5f, s * 0.38f), cap, stroke, Color.white, 0f, 0.2f);
            cut.BlurAlpha(1);
            p.EraseFrom(cut);

            p.RoundedRectOutline(new Rect(s * 0.14f, s * 0.31f, s * 0.72f, s * 0.38f), s * 0.05f, s * 0.022f, Ink);
        }

        static void IconGem(TexturePainter p, float s)
        {
            var points = new[]
            {
                new Vector2(s * 0.5f, s * 0.88f), new Vector2(s * 0.88f, s * 0.58f),
                new Vector2(s * 0.68f, s * 0.14f), new Vector2(s * 0.32f, s * 0.14f),
                new Vector2(s * 0.12f, s * 0.58f)
            };
            p.Polygon(points, Ink);
            // Facets are cut out rather than shaded so the icon reads at any tint.
            p.Erase(TexturePainter.CapsuleSdf(new Vector2(s * 0.5f, s * 0.86f), new Vector2(s * 0.5f, s * 0.16f), s * 0.016f));
            p.Erase(TexturePainter.CapsuleSdf(new Vector2(s * 0.14f, s * 0.58f), new Vector2(s * 0.86f, s * 0.58f), s * 0.016f));
        }

        static void IconBolt(TexturePainter p, float s)
        {
            var points = new[]
            {
                new Vector2(s * 0.58f, s * 0.92f), new Vector2(s * 0.24f, s * 0.48f),
                new Vector2(s * 0.46f, s * 0.48f), new Vector2(s * 0.40f, s * 0.08f),
                new Vector2(s * 0.76f, s * 0.54f), new Vector2(s * 0.54f, s * 0.54f)
            };
            p.Polygon(points, Ink);
        }

        static void IconGear(TexturePainter p, float s)
        {
            var centre = new Vector2(s * 0.5f, s * 0.5f);
            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.PI * 2f * i / 8f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                p.Capsule(centre + direction * s * 0.23f, centre + direction * s * 0.4f, s * 0.085f, Ink);
            }
            p.Circle(centre, s * 0.3f, Ink);
            p.EraseCircle(centre, s * 0.13f);
        }

        static void IconClose(TexturePainter p, float s)
        {
            p.Capsule(new Vector2(s * 0.28f, s * 0.28f), new Vector2(s * 0.72f, s * 0.72f), s * 0.075f, Ink);
            p.Capsule(new Vector2(s * 0.28f, s * 0.72f), new Vector2(s * 0.72f, s * 0.28f), s * 0.075f, Ink);
        }

        static void IconLock(TexturePainter p, float s)
        {
            p.RoundedRect(new Rect(s * 0.22f, s * 0.12f, s * 0.56f, s * 0.44f), s * 0.09f, Ink);
            p.Ring(new Vector2(s * 0.5f, s * 0.6f), s * 0.18f, s * 0.09f, Ink);
            p.EraseRect(new Rect(s * 0.24f, s * 0.42f, s * 0.52f, s * 0.16f), 2f);
            p.RoundedRect(new Rect(s * 0.22f, s * 0.12f, s * 0.56f, s * 0.44f), s * 0.09f, Ink);
            p.EraseCircle(new Vector2(s * 0.5f, s * 0.36f), s * 0.07f);
            p.Erase(TexturePainter.RoundedRectSdf(new Rect(s * 0.47f, s * 0.2f, s * 0.06f, s * 0.16f), s * 0.03f));
        }

        static void IconStar(TexturePainter p, float s)
            => p.Polygon(TexturePainter.StarPoints(new Vector2(s * 0.5f, s * 0.5f), s * 0.42f, s * 0.19f, 5), Ink);

        static void IconSpeed(TexturePainter p, float s)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = s * (0.24f + i * 0.2f);
                p.Capsule(new Vector2(x, s * 0.72f), new Vector2(x + s * 0.14f, s * 0.5f), s * 0.065f, Ink);
                p.Capsule(new Vector2(x + s * 0.14f, s * 0.5f), new Vector2(x, s * 0.28f), s * 0.065f, Ink);
            }
        }

        static void IconCapacity(TexturePainter p, float s)
        {
            p.Ring(new Vector2(s * 0.5f, s * 0.5f), s * 0.36f, s * 0.1f, Ink);
            p.Circle(new Vector2(s * 0.5f, s * 0.5f), s * 0.2f, Ink);
        }

        static void IconMagnet(TexturePainter p, float s)
        {
            var centre = new Vector2(s * 0.5f, s * 0.42f);
            int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(200f, 340f, i / (float)steps) * Mathf.Deg2Rad;
                float nextAngle = Mathf.Lerp(200f, 340f, Mathf.Min(1f, (i + 1) / (float)steps)) * Mathf.Deg2Rad;
                var a = centre + new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * s * 0.3f;
                var b = centre + new Vector2(Mathf.Cos(nextAngle), -Mathf.Sin(nextAngle)) * s * 0.3f;
                p.Capsule(a, b, s * 0.09f, Ink);
            }
            // Poles reach up to meet the arc ends so the horseshoe reads as one piece.
            p.RoundedRect(new Rect(s * 0.13f, s * 0.10f, s * 0.18f, s * 0.46f), s * 0.04f, Ink);
            p.RoundedRect(new Rect(s * 0.69f, s * 0.10f, s * 0.18f, s * 0.46f), s * 0.04f, Ink);
            p.Erase(TexturePainter.RoundedRectSdf(new Rect(s * 0.13f, s * 0.10f, s * 0.18f, s * 0.13f), s * 0.03f));
            p.Erase(TexturePainter.RoundedRectSdf(new Rect(s * 0.69f, s * 0.10f, s * 0.18f, s * 0.13f), s * 0.03f));
        }

        static void IconRobot(TexturePainter p, float s)
        {
            p.RoundedRect(new Rect(s * 0.24f, s * 0.24f, s * 0.52f, s * 0.44f), s * 0.12f, Ink);
            p.EraseCircle(new Vector2(s * 0.38f, s * 0.46f), s * 0.06f);
            p.EraseCircle(new Vector2(s * 0.62f, s * 0.46f), s * 0.06f);
            p.Capsule(new Vector2(s * 0.5f, s * 0.68f), new Vector2(s * 0.5f, s * 0.82f), s * 0.03f, Ink);
            p.Circle(new Vector2(s * 0.5f, s * 0.86f), s * 0.07f, Ink);
            p.RoundedRect(new Rect(s * 0.1f, s * 0.36f, s * 0.1f, s * 0.22f), s * 0.04f, Ink);
            p.RoundedRect(new Rect(s * 0.8f, s * 0.36f, s * 0.1f, s * 0.22f), s * 0.04f, Ink);
        }

        static void IconPrice(TexturePainter p, float s)
        {
            var points = new[]
            {
                new Vector2(s * 0.12f, s * 0.52f), new Vector2(s * 0.52f, s * 0.9f),
                new Vector2(s * 0.9f, s * 0.52f), new Vector2(s * 0.52f, s * 0.12f)
            };
            p.Polygon(points, Ink);
            p.EraseCircle(new Vector2(s * 0.62f, s * 0.62f), s * 0.08f);
        }

        static void IconMachine(TexturePainter p, float s)
        {
            p.RoundedRect(new Rect(s * 0.16f, s * 0.2f, s * 0.68f, s * 0.5f), s * 0.08f, Ink);
            p.EraseRect(new Rect(s * 0.3f, s * 0.34f, s * 0.4f, s * 0.24f), s * 0.04f);
            p.RoundedRect(new Rect(s * 0.3f, s * 0.7f, s * 0.16f, s * 0.16f), s * 0.05f, Ink);
            p.RoundedRect(new Rect(s * 0.56f, s * 0.7f, s * 0.14f, s * 0.1f), s * 0.04f, Ink);
        }

        static void IconShopper(TexturePainter p, float s)
        {
            p.Circle(new Vector2(s * 0.5f, s * 0.74f), s * 0.14f, Ink);
            var points = new[]
            {
                new Vector2(s * 0.24f, s * 0.12f), new Vector2(s * 0.3f, s * 0.56f),
                new Vector2(s * 0.7f, s * 0.56f), new Vector2(s * 0.76f, s * 0.12f)
            };
            p.Polygon(points, Ink);
        }

        static void IconOffline(TexturePainter p, float s)
        {
            p.Circle(new Vector2(s * 0.5f, s * 0.5f), s * 0.38f, Ink);
            p.EraseCircle(new Vector2(s * 0.64f, s * 0.62f), s * 0.33f);
        }

        static void IconBox(TexturePainter p, float s)
        {
            p.RoundedRect(new Rect(s * 0.18f, s * 0.18f, s * 0.64f, s * 0.6f), s * 0.07f, Ink);
            p.Erase(TexturePainter.RoundedRectSdf(new Rect(s * 0.455f, s * 0.18f, s * 0.09f, s * 0.6f), 2f));
            p.Erase(TexturePainter.RoundedRectSdf(new Rect(s * 0.18f, s * 0.655f, s * 0.64f, s * 0.05f), 2f));
        }

        static void IconWrench(TexturePainter p, float s)
        {
            p.Capsule(new Vector2(s * 0.3f, s * 0.3f), new Vector2(s * 0.68f, s * 0.68f), s * 0.085f, Ink);
            p.Circle(new Vector2(s * 0.74f, s * 0.74f), s * 0.16f, Ink);
            p.EraseCircle(new Vector2(s * 0.79f, s * 0.79f), s * 0.08f);
            p.Circle(new Vector2(s * 0.26f, s * 0.26f), s * 0.13f, Ink);
        }

        static void IconPlus(TexturePainter p, float s)
        {
            p.Capsule(new Vector2(s * 0.5f, s * 0.24f), new Vector2(s * 0.5f, s * 0.76f), s * 0.085f, Ink);
            p.Capsule(new Vector2(s * 0.24f, s * 0.5f), new Vector2(s * 0.76f, s * 0.5f), s * 0.085f, Ink);
        }

        static void IconCheck(TexturePainter p, float s)
        {
            p.Capsule(new Vector2(s * 0.22f, s * 0.52f), new Vector2(s * 0.44f, s * 0.28f), s * 0.085f, Ink);
            p.Capsule(new Vector2(s * 0.44f, s * 0.28f), new Vector2(s * 0.8f, s * 0.74f), s * 0.085f, Ink);
        }

        static void IconPlay(TexturePainter p, float s)
        {
            var points = new[]
            {
                new Vector2(s * 0.32f, s * 0.2f), new Vector2(s * 0.32f, s * 0.8f), new Vector2(s * 0.82f, s * 0.5f)
            };
            p.Polygon(points, Ink);
        }

        static void IconMusic(TexturePainter p, float s)
        {
            p.Circle(new Vector2(s * 0.34f, s * 0.26f), s * 0.13f, Ink);
            p.Circle(new Vector2(s * 0.7f, s * 0.2f), s * 0.11f, Ink);
            p.Capsule(new Vector2(s * 0.46f, s * 0.26f), new Vector2(s * 0.46f, s * 0.82f), s * 0.05f, Ink);
            p.Capsule(new Vector2(s * 0.8f, s * 0.2f), new Vector2(s * 0.8f, s * 0.72f), s * 0.05f, Ink);
            p.Capsule(new Vector2(s * 0.46f, s * 0.82f), new Vector2(s * 0.8f, s * 0.72f), s * 0.05f, Ink);
        }

        static void IconHole(TexturePainter p, float s)
        {
            p.Ring(new Vector2(s * 0.5f, s * 0.5f), s * 0.36f, s * 0.11f, Ink);
            p.Circle(new Vector2(s * 0.5f, s * 0.5f), s * 0.17f, Ink);
        }
    }
}
