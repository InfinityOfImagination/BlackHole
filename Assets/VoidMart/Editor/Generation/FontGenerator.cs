using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Data;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Draws Void Mart's display typeface from scratch.  Each glyph is a short list of strokes on
    /// a cap-height grid, rasterised with rounded caps, which yields a chunky geometric sans in the
    /// spirit of the casual-mobile look - and lets the Master Tool restyle the whole UI by moving
    /// two sliders (weight and rounding) instead of importing a new font.
    /// </summary>
    public static class FontGenerator
    {
        struct Seg
        {
            public bool IsArc;
            public Vector2 A, B;                 // line endpoints
            public Vector2 Centre;               // arc centre
            public float Rx, Ry, Start, End;     // arc geometry (degrees)
        }

        class Glyph
        {
            public float Width;                  // in cap-height units
            public Seg[] Segments;
            public bool Dot;                     // draw a round dot at baseline (i, j, punctuation)
        }

        static Seg L(float x0, float y0, float x1, float y1) =>
            new Seg { IsArc = false, A = new Vector2(x0, y0), B = new Vector2(x1, y1) };

        static Seg A(float cx, float cy, float rx, float ry, float start, float end) =>
            new Seg { IsArc = true, Centre = new Vector2(cx, cy), Rx = rx, Ry = ry, Start = start, End = end };

        static Glyph G(float width, params Seg[] segments) => new Glyph { Width = width, Segments = segments };

        // ----------------------------------------------------------- glyph table

        static Dictionary<char, Glyph> BuildGlyphs()
        {
            const float w = 0.66f;   // default letter width in cap units
            var glyphs = new Dictionary<char, Glyph>(128);

            glyphs['A'] = G(w, L(0f, 0f, w * 0.5f, 1f), L(w * 0.5f, 1f, w, 0f), L(w * 0.16f, 0.33f, w * 0.84f, 0.33f));
            glyphs['B'] = G(w, L(0f, 0f, 0f, 1f), A(0f, 0.75f, w * 0.86f, 0.25f, 90f, -90f), A(0f, 0.25f, w * 0.95f, 0.25f, 90f, -90f));
            glyphs['C'] = G(w, A(w * 0.5f, 0.5f, w * 0.5f, 0.5f, 55f, 305f));
            glyphs['D'] = G(w, L(0f, 0f, 0f, 1f), A(0f, 0.5f, w, 0.5f, 90f, -90f));
            glyphs['E'] = G(w * 0.88f, L(0f, 0f, 0f, 1f), L(0f, 1f, w * 0.82f, 1f), L(0f, 0.5f, w * 0.7f, 0.5f), L(0f, 0f, w * 0.82f, 0f));
            glyphs['F'] = G(w * 0.85f, L(0f, 0f, 0f, 1f), L(0f, 1f, w * 0.8f, 1f), L(0f, 0.52f, w * 0.66f, 0.52f));
            glyphs['G'] = G(w, A(w * 0.5f, 0.5f, w * 0.5f, 0.5f, 55f, 315f), L(w * 0.98f, 0.42f, w * 0.55f, 0.42f), L(w * 0.98f, 0.42f, w * 0.98f, 0.12f));
            glyphs['H'] = G(w, L(0f, 0f, 0f, 1f), L(w, 0f, w, 1f), L(0f, 0.5f, w, 0.5f));
            glyphs['I'] = G(0.14f, L(0.07f, 0f, 0.07f, 1f));
            glyphs['J'] = G(w * 0.8f, L(w * 0.72f, 1f, w * 0.72f, 0.28f), A(w * 0.36f, 0.28f, w * 0.36f, 0.28f, 0f, -180f));
            glyphs['K'] = G(w, L(0f, 0f, 0f, 1f), L(0f, 0.42f, w * 0.92f, 1f), L(0f, 0.42f, w, 0f));
            glyphs['L'] = G(w * 0.82f, L(0f, 0f, 0f, 1f), L(0f, 0f, w * 0.78f, 0f));
            glyphs['M'] = G(w * 1.18f, L(0f, 0f, 0f, 1f), L(0f, 1f, w * 0.59f, 0.33f), L(w * 0.59f, 0.33f, w * 1.18f, 1f), L(w * 1.18f, 1f, w * 1.18f, 0f));
            glyphs['N'] = G(w, L(0f, 0f, 0f, 1f), L(0f, 1f, w, 0f), L(w, 0f, w, 1f));
            glyphs['O'] = G(w * 1.05f, A(w * 0.525f, 0.5f, w * 0.525f, 0.5f, 0f, 360f));
            glyphs['P'] = G(w, L(0f, 0f, 0f, 1f), A(0f, 0.72f, w * 0.92f, 0.28f, 90f, -90f));
            glyphs['Q'] = G(w * 1.05f, A(w * 0.525f, 0.5f, w * 0.525f, 0.5f, 0f, 360f), L(w * 0.62f, 0.3f, w * 1.02f, -0.06f));
            glyphs['R'] = G(w, L(0f, 0f, 0f, 1f), A(0f, 0.72f, w * 0.88f, 0.28f, 90f, -90f), L(w * 0.34f, 0.44f, w, 0f));
            // S is three strokes: the top bowl sweeping left, the spine crossing the middle, and
            // the bottom bowl sweeping right. Getting the spine wrong turns it into a C.
            glyphs['S'] = G(w,
                A(w * 0.5f, 0.70f, w * 0.5f, 0.28f, 20f, 200f),
                L(w * 0.03f, 0.61f, w * 0.97f, 0.39f),
                A(w * 0.5f, 0.30f, w * 0.5f, 0.28f, 20f, -160f));
            glyphs['T'] = G(w, L(0f, 1f, w, 1f), L(w * 0.5f, 0f, w * 0.5f, 1f));
            glyphs['U'] = G(w, L(0f, 1f, 0f, 0.3f), A(w * 0.5f, 0.3f, w * 0.5f, 0.3f, 180f, 360f), L(w, 0.3f, w, 1f));
            glyphs['V'] = G(w, L(0f, 1f, w * 0.5f, 0f), L(w * 0.5f, 0f, w, 1f));
            glyphs['W'] = G(w * 1.3f, L(0f, 1f, w * 0.32f, 0f), L(w * 0.32f, 0f, w * 0.65f, 0.66f), L(w * 0.65f, 0.66f, w * 0.98f, 0f), L(w * 0.98f, 0f, w * 1.3f, 1f));
            glyphs['X'] = G(w, L(0f, 0f, w, 1f), L(0f, 1f, w, 0f));
            glyphs['Y'] = G(w, L(0f, 1f, w * 0.5f, 0.48f), L(w, 1f, w * 0.5f, 0.48f), L(w * 0.5f, 0.48f, w * 0.5f, 0f));
            glyphs['Z'] = G(w, L(0f, 1f, w, 1f), L(w, 1f, 0f, 0f), L(0f, 0f, w, 0f));

            glyphs['0'] = G(w, A(w * 0.5f, 0.5f, w * 0.5f, 0.5f, 0f, 360f), L(w * 0.18f, 0.2f, w * 0.82f, 0.8f));
            glyphs['1'] = G(w * 0.5f, L(w * 0.25f, 0f, w * 0.25f, 1f), L(0f, 0.78f, w * 0.25f, 1f));
            glyphs['2'] = G(w, A(w * 0.5f, 0.72f, w * 0.5f, 0.28f, 180f, 0f), L(w, 0.72f, 0f, 0f), L(0f, 0f, w, 0f));
            glyphs['3'] = G(w, A(w * 0.44f, 0.75f, w * 0.44f, 0.25f, 150f, -70f), A(w * 0.44f, 0.25f, w * 0.5f, 0.25f, 70f, -150f));
            glyphs['4'] = G(w, L(w * 0.72f, 0f, w * 0.72f, 1f), L(w * 0.72f, 1f, 0f, 0.3f), L(0f, 0.3f, w, 0.3f));
            glyphs['5'] = G(w, L(w * 0.92f, 1f, w * 0.06f, 1f), L(w * 0.06f, 1f, w * 0.02f, 0.56f), A(w * 0.48f, 0.3f, w * 0.52f, 0.3f, 110f, -140f));
            glyphs['6'] = G(w, A(w * 0.5f, 0.3f, w * 0.5f, 0.3f, 0f, 360f), A(w * 0.5f, 0.55f, w * 0.5f, 0.45f, 90f, 175f));
            glyphs['7'] = G(w, L(0f, 1f, w, 1f), L(w, 1f, w * 0.26f, 0f));
            glyphs['8'] = G(w, A(w * 0.5f, 0.74f, w * 0.44f, 0.26f, 0f, 360f), A(w * 0.5f, 0.26f, w * 0.5f, 0.26f, 0f, 360f));
            glyphs['9'] = G(w, A(w * 0.5f, 0.7f, w * 0.5f, 0.3f, 0f, 360f), A(w * 0.5f, 0.45f, w * 0.5f, 0.45f, -90f, -5f));

            glyphs['.'] = new Glyph { Width = 0.1f, Segments = new[] { L(0.05f, 0.02f, 0.05f, 0.02f) } };
            glyphs[','] = G(0.14f, L(0.08f, 0.04f, 0.0f, -0.16f));
            glyphs[':'] = G(0.1f, L(0.05f, 0.05f, 0.05f, 0.05f), L(0.05f, 0.58f, 0.05f, 0.58f));
            glyphs[';'] = G(0.14f, L(0.08f, 0.58f, 0.08f, 0.58f), L(0.08f, 0.04f, 0.0f, -0.16f));
            glyphs['!'] = G(0.12f, L(0.06f, 0.3f, 0.06f, 1f), L(0.06f, 0.04f, 0.06f, 0.04f));
            glyphs['?'] = G(w * 0.8f, A(w * 0.4f, 0.74f, w * 0.38f, 0.26f, 200f, -20f), L(w * 0.4f, 0.48f, w * 0.4f, 0.3f), L(w * 0.4f, 0.04f, w * 0.4f, 0.04f));
            glyphs['\''] = G(0.1f, L(0.05f, 0.72f, 0.05f, 1f));
            glyphs['"'] = G(0.24f, L(0.06f, 0.72f, 0.06f, 1f), L(0.18f, 0.72f, 0.18f, 1f));
            glyphs['-'] = G(0.38f, L(0.04f, 0.5f, 0.34f, 0.5f));
            glyphs['_'] = G(0.5f, L(0f, -0.08f, 0.5f, -0.08f));
            glyphs['+'] = G(0.5f, L(0.05f, 0.5f, 0.45f, 0.5f), L(0.25f, 0.3f, 0.25f, 0.7f));
            glyphs['='] = G(0.5f, L(0.05f, 0.36f, 0.45f, 0.36f), L(0.05f, 0.62f, 0.45f, 0.62f));
            glyphs['/'] = G(0.44f, L(0.02f, -0.04f, 0.42f, 1f));
            glyphs['\\'] = G(0.44f, L(0.02f, 1f, 0.42f, -0.04f));
            glyphs['%'] = G(w * 1.05f,
                A(w * 0.24f, 0.75f, w * 0.23f, 0.23f, 0f, 360f),
                A(w * 0.81f, 0.25f, w * 0.23f, 0.23f, 0f, 360f),
                L(w * 0.04f, 0.03f, w * 1.01f, 0.97f));
            glyphs['$'] = G(w,
                A(w * 0.5f, 0.68f, w * 0.46f, 0.25f, 20f, 200f),
                L(w * 0.06f, 0.60f, w * 0.94f, 0.42f),
                A(w * 0.5f, 0.33f, w * 0.46f, 0.25f, 20f, -160f),
                L(w * 0.5f, -0.06f, w * 0.5f, 1.06f));
            glyphs['('] = G(0.3f, A(0.3f, 0.48f, 0.3f, 0.62f, 130f, 230f));
            glyphs[')'] = G(0.3f, A(0f, 0.48f, 0.3f, 0.62f, 50f, -50f));
            glyphs['['] = G(0.28f, L(0.24f, 1.06f, 0.04f, 1.06f), L(0.04f, 1.06f, 0.04f, -0.08f), L(0.04f, -0.08f, 0.24f, -0.08f));
            glyphs[']'] = G(0.28f, L(0.04f, 1.06f, 0.24f, 1.06f), L(0.24f, 1.06f, 0.24f, -0.08f), L(0.24f, -0.08f, 0.04f, -0.08f));
            glyphs['<'] = G(0.44f, L(0.4f, 0.82f, 0.04f, 0.46f), L(0.04f, 0.46f, 0.4f, 0.1f));
            glyphs['>'] = G(0.44f, L(0.04f, 0.82f, 0.4f, 0.46f), L(0.4f, 0.46f, 0.04f, 0.1f));
            glyphs['*'] = G(0.42f, L(0.21f, 0.5f, 0.21f, 0.94f), L(0.03f, 0.61f, 0.39f, 0.83f), L(0.03f, 0.83f, 0.39f, 0.61f));
            glyphs['#'] = G(w, L(w * 0.22f, 0f, w * 0.32f, 1f), L(w * 0.62f, 0f, w * 0.72f, 1f), L(0f, 0.32f, w * 0.9f, 0.32f), L(0f, 0.68f, w * 0.9f, 0.68f));
            glyphs['@'] = G(w * 1.15f, A(w * 0.575f, 0.5f, w * 0.575f, 0.5f, 300f, 20f), A(w * 0.575f, 0.45f, w * 0.24f, 0.24f, 0f, 360f), L(w * 1.1f, 0.45f, w * 1.1f, 0.3f));
            glyphs['&'] = G(w * 1.15f,
                A(w * 0.42f, 0.80f, w * 0.32f, 0.20f, 0f, 360f),   // upper bowl
                A(w * 0.46f, 0.28f, w * 0.46f, 0.28f, 45f, 305f),  // lower bowl, open to the right
                L(w * 0.12f, 0.56f, w * 0.80f, 0.04f),             // crossing stroke
                L(w * 0.79f, 0.36f, w * 1.15f, 0.02f));            // tail
            glyphs['~'] = G(0.5f, A(0.15f, 0.5f, 0.14f, 0.1f, 180f, 0f), A(0.4f, 0.5f, 0.12f, 0.1f, 180f, 360f));
            glyphs['^'] = G(0.44f, L(0.04f, 0.72f, 0.22f, 0.98f), L(0.22f, 0.98f, 0.4f, 0.72f));
            glyphs['|'] = G(0.14f, L(0.07f, -0.08f, 0.07f, 1.06f));

            return glyphs;
        }

        // ------------------------------------------------------------- rasterise

        struct Tile
        {
            public char Character;
            public TexturePainter Painter;
            public float Advance;
            public Vector2 Bearing;
        }

        public static (Texture2D atlas, VMFontAsset.Glyph[] glyphs, float ascender, float descender, float spaceAdvance, float capHeight)
            Build(ArtConfig art)
        {
            var table = BuildGlyphs();
            int pixelHeight = Mathf.Clamp(art.fontPixelHeight, 16, 160);
            float capHeight = pixelHeight * 0.72f;
            float stroke = Mathf.Max(2f, pixelHeight * art.fontStrokeWeight);
            float descent = capHeight * 0.24f;
            const int pad = 3;

            var tiles = new List<Tile>(table.Count);

            foreach (var kv in table)
            {
                var glyph = kv.Value;
                float inkWidth = glyph.Width * capHeight + stroke;
                int tileW = Mathf.CeilToInt(inkWidth + pad * 2f);
                int tileH = Mathf.CeilToInt(capHeight + stroke + descent + pad * 2f + capHeight * 0.1f);

                var painter = new TexturePainter(tileW, tileH);
                float originX = pad + stroke * 0.5f;
                float originY = pad + descent + stroke * 0.5f;

                foreach (var segment in glyph.Segments)
                    DrawSegment(painter, segment, originX, originY, capHeight, stroke * 0.5f, art.fontRounding);

                tiles.Add(new Tile
                {
                    Character = kv.Key,
                    Painter = painter,
                    Advance = (inkWidth + capHeight * 0.1f) / pixelHeight,
                    Bearing = new Vector2(-pad / (float)pixelHeight, -(pad + descent + stroke * 0.5f) / pixelHeight)
                });
            }

            // Shelf-pack the tiles into a square atlas.
            tiles.Sort((a, b) => b.Painter.Height.CompareTo(a.Painter.Height));
            int atlasSize = 256;
            while (!TryPack(tiles, atlasSize, out _) && atlasSize < 4096) atlasSize *= 2;
            if (!TryPack(tiles, atlasSize, out var placements))
                Debug.LogWarning("[VoidMart] Font atlas is too small for the glyph set; some characters were dropped.");

            var atlasPainter = new TexturePainter(atlasSize, atlasSize, Color.clear);
            var results = new List<VMFontAsset.Glyph>(tiles.Count * 2);

            int packed = Mathf.Min(tiles.Count, placements.Count);
            for (int i = 0; i < packed; i++)
            {
                var tile = tiles[i];
                var position = placements[i];
                atlasPainter.Blit(tile.Painter, position.x, position.y);

                var uv = new Rect(
                    position.x / (float)atlasSize,
                    position.y / (float)atlasSize,
                    tile.Painter.Width / (float)atlasSize,
                    tile.Painter.Height / (float)atlasSize);

                var record = new VMFontAsset.Glyph
                {
                    codePoint = tile.Character,
                    uv = uv,
                    size = new Vector2(tile.Painter.Width / (float)pixelHeight, tile.Painter.Height / (float)pixelHeight),
                    bearing = tile.Bearing,
                    advance = tile.Advance
                };
                results.Add(record);

                // Lower-case maps onto the same drawing so any string renders.
                if (tile.Character >= 'A' && tile.Character <= 'Z')
                {
                    var lower = record;
                    lower.codePoint = char.ToLowerInvariant(tile.Character);
                    results.Add(lower);
                }
            }

            var atlas = atlasPainter.ToTexture("VMFontAtlas");
            float ascender = (capHeight + stroke * 0.5f) / pixelHeight;
            float descender = -(descent + stroke * 0.5f) / pixelHeight;
            float spaceAdvance = capHeight * 0.34f / pixelHeight;
            return (atlas, results.ToArray(), ascender, descender, spaceAdvance, capHeight / pixelHeight);
        }

        /// <summary>
        /// Draws a string straight into a painter using the same stroke table the atlas is built
        /// from - used for the wordmark and any baked-in lettering.
        /// </summary>
        public static void DrawText(TexturePainter painter, string text, Vector2 baselineStart,
            float capHeight, float stroke, Color color, float tracking = 0.08f, float rounding = 0.2f)
        {
            var table = BuildGlyphs();
            float penX = baselineStart.x;
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if (c == ' ')
                {
                    penX += capHeight * (0.34f + tracking);
                    continue;
                }
                if (!table.TryGetValue(c, out var glyph))
                {
                    penX += capHeight * (0.34f + tracking);
                    continue;
                }

                // Draw into a glyph-sized tile and composite it, rather than allocating a
                // full-canvas scratch buffer per character.
                const int pad = 2;
                float descentRoom = capHeight * 0.38f;
                int tileW = Mathf.CeilToInt(glyph.Width * capHeight + stroke + pad * 2);
                int tileH = Mathf.CeilToInt(capHeight * 1.18f + stroke + descentRoom + pad * 2);
                var scratch = new TexturePainter(tileW, tileH);

                foreach (var segment in glyph.Segments)
                    DrawSegment(scratch, segment, pad + stroke * 0.5f, pad + descentRoom, capHeight, stroke * 0.5f, rounding);

                int offsetX = Mathf.RoundToInt(penX - pad);
                int offsetY = Mathf.RoundToInt(baselineStart.y - pad - descentRoom);
                for (int y = 0; y < tileH; y++)
                {
                    for (int x = 0; x < tileW; x++)
                    {
                        float alpha = scratch.Pixels[y * tileW + x].a;
                        if (alpha > 0.002f) painter.Blend(offsetX + x, offsetY + y, color, alpha);
                    }
                }

                penX += glyph.Width * capHeight + stroke + capHeight * tracking;
            }
        }

        /// <summary>Width in pixels the same call to <see cref="DrawText"/> would occupy.</summary>
        public static float MeasureText(string text, float capHeight, float stroke, float tracking = 0.08f)
        {
            var table = BuildGlyphs();
            float width = 0f;
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if (c == ' ' || !table.TryGetValue(c, out var glyph)) { width += capHeight * (0.34f + tracking); continue; }
                width += glyph.Width * capHeight + stroke + capHeight * tracking;
            }
            return Mathf.Max(0f, width - capHeight * tracking);
        }

        static bool TryPack(List<Tile> tiles, int size, out List<Vector2Int> placements)
        {
            placements = new List<Vector2Int>(tiles.Count);
            int cursorX = 1, cursorY = 1, shelfHeight = 0;
            for (int i = 0; i < tiles.Count; i++)
            {
                int tw = tiles[i].Painter.Width + 2;
                int th = tiles[i].Painter.Height + 2;
                if (tw > size || th > size) return false;

                if (cursorX + tw > size)
                {
                    cursorX = 1;
                    cursorY += shelfHeight;
                    shelfHeight = 0;
                }
                if (cursorY + th > size) return false;

                placements.Add(new Vector2Int(cursorX, cursorY));
                cursorX += tw;
                shelfHeight = Mathf.Max(shelfHeight, th);
            }
            return true;
        }

        static void DrawSegment(TexturePainter painter, Seg segment, float originX, float originY, float scale, float radius, float rounding)
        {
            var white = Color.white;
            float capRadius = radius * (1f + rounding * 0.25f);

            if (!segment.IsArc)
            {
                Vector2 a = new Vector2(originX + segment.A.x * scale, originY + segment.A.y * scale);
                Vector2 b = new Vector2(originX + segment.B.x * scale, originY + segment.B.y * scale);
                if ((a - b).sqrMagnitude < 0.01f) painter.Circle(a, capRadius, white);
                else painter.Capsule(a, b, capRadius, white);
                return;
            }

            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(segment.End - segment.Start) / 12f), 3, 48);
            Vector2 previous = ArcPoint(segment, 0f, originX, originY, scale);
            for (int i = 1; i <= steps; i++)
            {
                Vector2 current = ArcPoint(segment, i / (float)steps, originX, originY, scale);
                painter.Capsule(previous, current, capRadius, white);
                previous = current;
            }
        }

        static Vector2 ArcPoint(Seg segment, float t, float originX, float originY, float scale)
        {
            float angle = Mathf.Lerp(segment.Start, segment.End, t) * Mathf.Deg2Rad;
            float x = segment.Centre.x + Mathf.Cos(angle) * segment.Rx;
            float y = segment.Centre.y + Mathf.Sin(angle) * segment.Ry;
            return new Vector2(originX + x * scale, originY + y * scale);
        }
    }
}
