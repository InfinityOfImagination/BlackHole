using System;
using UnityEngine;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// A tiny signed-distance-field rasteriser.  Every sprite, icon and glyph in Void Mart is
    /// drawn with it, which is why the UI can be regenerated in any palette without shipping a
    /// single PNG.  Distance fields give clean anti-aliasing at any size for free.
    /// </summary>
    public class TexturePainter
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color[] Pixels;

        public TexturePainter(int width, int height, Color? fill = null)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            Pixels = new Color[Width * Height];
            Clear(fill ?? Color.clear);
        }

        public void Clear(Color color)
        {
            for (int i = 0; i < Pixels.Length; i++) Pixels[i] = color;
        }

        // ------------------------------------------------------------- blending

        public void Blend(int x, int y, Color color, float coverage)
        {
            if (coverage <= 0.0005f || x < 0 || y < 0 || x >= Width || y >= Height) return;
            float alpha = Mathf.Clamp01(color.a * coverage);
            if (alpha <= 0.0005f) return;

            int index = y * Width + x;
            Color dst = Pixels[index];
            float outAlpha = alpha + dst.a * (1f - alpha);
            if (outAlpha <= 0.0001f)
            {
                Pixels[index] = Color.clear;
                return;
            }
            float r = (color.r * alpha + dst.r * dst.a * (1f - alpha)) / outAlpha;
            float g = (color.g * alpha + dst.g * dst.a * (1f - alpha)) / outAlpha;
            float b = (color.b * alpha + dst.b * dst.a * (1f - alpha)) / outAlpha;
            Pixels[index] = new Color(r, g, b, outAlpha);
        }

        public Color Get(int x, int y) => Pixels[Mathf.Clamp(y, 0, Height - 1) * Width + Mathf.Clamp(x, 0, Width - 1)];

        /// <summary>Rasterises any SDF: negative inside, positive outside, in pixels.</summary>
        public void Shade(Func<float, float, float> distance, Color color, float feather = 1f, Rect? bounds = null)
        {
            int x0 = 0, y0 = 0, x1 = Width, y1 = Height;
            if (bounds.HasValue)
            {
                var r = bounds.Value;
                x0 = Mathf.Max(0, Mathf.FloorToInt(r.xMin));
                y0 = Mathf.Max(0, Mathf.FloorToInt(r.yMin));
                x1 = Mathf.Min(Width, Mathf.CeilToInt(r.xMax));
                y1 = Mathf.Min(Height, Mathf.CeilToInt(r.yMax));
            }

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    float d = distance(x + 0.5f, y + 0.5f);
                    float coverage = Mathf.Clamp01(0.5f - d / Mathf.Max(0.0001f, feather));
                    if (coverage > 0f) Blend(x, y, color, coverage);
                }
            }
        }

        /// <summary>Same as Shade but the colour varies per pixel (gradients inside a shape).</summary>
        public void ShadeGradient(Func<float, float, float> distance, Func<float, float, Color> colorAt, float feather = 1f, Rect? bounds = null)
        {
            int x0 = 0, y0 = 0, x1 = Width, y1 = Height;
            if (bounds.HasValue)
            {
                var r = bounds.Value;
                x0 = Mathf.Max(0, Mathf.FloorToInt(r.xMin));
                y0 = Mathf.Max(0, Mathf.FloorToInt(r.yMin));
                x1 = Mathf.Min(Width, Mathf.CeilToInt(r.xMax));
                y1 = Mathf.Min(Height, Mathf.CeilToInt(r.yMax));
            }

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    float d = distance(x + 0.5f, y + 0.5f);
                    float coverage = Mathf.Clamp01(0.5f - d / Mathf.Max(0.0001f, feather));
                    if (coverage > 0f) Blend(x, y, colorAt(x + 0.5f, y + 0.5f), coverage);
                }
            }
        }

        /// <summary>Punches a shape out of whatever has been drawn so far (icon cut-outs).</summary>
        public void Erase(Func<float, float, float> distance, float feather = 1.35f)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float d = distance(x + 0.5f, y + 0.5f);
                    float coverage = Mathf.Clamp01(0.5f - d / Mathf.Max(0.0001f, feather));
                    if (coverage <= 0.0005f) continue;
                    int index = y * Width + x;
                    var color = Pixels[index];
                    color.a *= 1f - coverage;
                    Pixels[index] = color;
                }
            }
        }

        public void EraseCircle(Vector2 centre, float radius) => Erase(CircleSdf(centre, radius));

        /// <summary>Subtracts another painter's coverage - used to knock lettering out of shapes.</summary>
        public void EraseFrom(TexturePainter mask, int offsetX = 0, int offsetY = 0)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int sx = x - offsetX, sy = y - offsetY;
                    if (sx < 0 || sy < 0 || sx >= mask.Width || sy >= mask.Height) continue;
                    float coverage = mask.Pixels[sy * mask.Width + sx].a;
                    if (coverage <= 0.002f) continue;
                    int index = y * Width + x;
                    var color = Pixels[index];
                    color.a *= 1f - coverage;
                    Pixels[index] = color;
                }
            }
        }

        public void EraseRect(Rect rect, float radius) => Erase(RoundedRectSdf(rect, radius));

        // ---------------------------------------------------------------- SDFs

        public static Func<float, float, float> RoundedRectSdf(Rect rect, float radius)
        {
            float halfW = rect.width * 0.5f;
            float halfH = rect.height * 0.5f;
            float cx = rect.center.x;
            float cy = rect.center.y;
            float r = Mathf.Min(radius, Mathf.Min(halfW, halfH));
            return (x, y) =>
            {
                float dx = Mathf.Abs(x - cx) - (halfW - r);
                float dy = Mathf.Abs(y - cy) - (halfH - r);
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                return outside + inside - r;
            };
        }

        public static Func<float, float, float> CircleSdf(Vector2 centre, float radius)
            => (x, y) => Mathf.Sqrt((x - centre.x) * (x - centre.x) + (y - centre.y) * (y - centre.y)) - radius;

        public static Func<float, float, float> RingSdf(Vector2 centre, float radius, float thickness)
        {
            var circle = CircleSdf(centre, radius);
            return (x, y) => Mathf.Abs(circle(x, y)) - thickness * 0.5f;
        }

        public static Func<float, float, float> CapsuleSdf(Vector2 a, Vector2 b, float radius)
        {
            return (x, y) =>
            {
                Vector2 p = new Vector2(x, y);
                Vector2 pa = p - a;
                Vector2 ba = b - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(0.0001f, Vector2.Dot(ba, ba)));
                return (pa - ba * h).magnitude - radius;
            };
        }

        public static Func<float, float, float> PolygonSdf(Vector2[] points)
        {
            return (x, y) =>
            {
                var p = new Vector2(x, y);
                float d = float.MaxValue;
                bool inside = false;
                int count = points.Length;
                for (int i = 0, j = count - 1; i < count; j = i++)
                {
                    Vector2 a = points[i];
                    Vector2 b = points[j];
                    Vector2 e = b - a;
                    Vector2 w = p - a;
                    float h = Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(0.0001f, Vector2.Dot(e, e)));
                    d = Mathf.Min(d, (w - e * h).sqrMagnitude);
                    bool crosses = (a.y > p.y) != (b.y > p.y) &&
                                   p.x < (b.x - a.x) * (p.y - a.y) / Mathf.Max(0.0001f, b.y - a.y) + a.x;
                    if (crosses) inside = !inside;
                }
                return (inside ? -1f : 1f) * Mathf.Sqrt(d);
            };
        }

        public static Func<float, float, float> Union(Func<float, float, float> a, Func<float, float, float> b)
            => (x, y) => Mathf.Min(a(x, y), b(x, y));

        public static Func<float, float, float> Subtract(Func<float, float, float> a, Func<float, float, float> b)
            => (x, y) => Mathf.Max(a(x, y), -b(x, y));

        public static Func<float, float, float> Intersect(Func<float, float, float> a, Func<float, float, float> b)
            => (x, y) => Mathf.Max(a(x, y), b(x, y));

        // -------------------------------------------------------------- helpers

        public void RoundedRect(Rect rect, float radius, Color color, float feather = 1.35f)
            => Shade(RoundedRectSdf(rect, radius), color, feather, Grow(rect, feather + 2f));

        public void RoundedRectGradient(Rect rect, float radius, Color top, Color bottom, float feather = 1.35f)
        {
            float y0 = rect.yMin, y1 = rect.yMax;
            ShadeGradient(RoundedRectSdf(rect, radius),
                (x, y) => Color.Lerp(bottom, top, Mathf.InverseLerp(y0, y1, y)),
                feather, Grow(rect, feather + 2f));
        }

        public void RoundedRectOutline(Rect rect, float radius, float thickness, Color color, float feather = 1.2f)
        {
            var sdf = RoundedRectSdf(rect, radius);
            Shade((x, y) => Mathf.Abs(sdf(x, y)) - thickness * 0.5f, color, feather, Grow(rect, thickness + feather + 2f));
        }

        public void Circle(Vector2 centre, float radius, Color color, float feather = 1.35f)
            => Shade(CircleSdf(centre, radius), color, feather, new Rect(centre.x - radius - 3f, centre.y - radius - 3f, radius * 2f + 6f, radius * 2f + 6f));

        public void Ring(Vector2 centre, float radius, float thickness, Color color, float feather = 1.35f)
            => Shade(RingSdf(centre, radius, thickness), color, feather,
                new Rect(centre.x - radius - thickness, centre.y - radius - thickness, (radius + thickness) * 2f, (radius + thickness) * 2f));

        public void Capsule(Vector2 a, Vector2 b, float radius, Color color, float feather = 1.35f)
            => Shade(CapsuleSdf(a, b, radius), color, feather);

        public void Polygon(Vector2[] points, Color color, float feather = 1.35f)
            => Shade(PolygonSdf(points), color, feather);

        public void RadialGlow(Vector2 centre, float radius, Color inner, Color outer)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(centre.x - radius));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(centre.y - radius));
            int x1 = Mathf.Min(Width, Mathf.CeilToInt(centre.x + radius));
            int y1 = Mathf.Min(Height, Mathf.CeilToInt(centre.y + radius));
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centre) / radius;
                    if (d > 1f) continue;
                    Blend(x, y, Color.Lerp(inner, outer, d * d), 1f);
                }
            }
        }

        public static Vector2[] StarPoints(Vector2 centre, float outerRadius, float innerRadius, int points, float rotationDegrees = 0f)
        {
            var result = new Vector2[points * 2];
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < points * 2; i++)
            {
                float angle = rotation + Mathf.PI * i / points;
                float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                result[i] = centre + new Vector2(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius);
            }
            return result;
        }

        public static Vector2[] RegularPolygon(Vector2 centre, float radius, int sides, float rotationDegrees = 0f)
        {
            var result = new Vector2[sides];
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < sides; i++)
            {
                float angle = rotation + Mathf.PI * 2f * i / sides;
                result[i] = centre + new Vector2(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius);
            }
            return result;
        }

        static Rect Grow(Rect rect, float amount) =>
            new Rect(rect.xMin - amount, rect.yMin - amount, rect.width + amount * 2f, rect.height + amount * 2f);

        // --------------------------------------------------------------- effects

        /// <summary>Renders <paramref name="draw"/> twice: a blurred dark copy first, then the real thing.</summary>
        public void WithShadow(Action<TexturePainter> draw, Vector2 offset, float blurRadius, Color shadowColor)
        {
            var shadow = new TexturePainter(Width, Height);
            draw(shadow);
            shadow.BlurAlpha(Mathf.Max(1, Mathf.RoundToInt(blurRadius)));

            int dx = Mathf.RoundToInt(offset.x);
            int dy = Mathf.RoundToInt(offset.y);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int sx = x - dx, sy = y - dy;
                    if (sx < 0 || sy < 0 || sx >= Width || sy >= Height) continue;
                    float alpha = shadow.Pixels[sy * Width + sx].a;
                    if (alpha <= 0.002f) continue;
                    Blend(x, y, shadowColor, alpha * shadowColor.a);
                }
            }
            draw(this);
        }

        public void BlurAlpha(int radius)
        {
            if (radius <= 0) return;
            var source = new float[Pixels.Length];
            for (int i = 0; i < Pixels.Length; i++) source[i] = Pixels[i].a;
            var temp = new float[Pixels.Length];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sx = Mathf.Clamp(x + k, 0, Width - 1);
                        sum += source[y * Width + sx];
                        count++;
                    }
                    temp[y * Width + x] = sum / count;
                }
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int k = -radius; k <= radius; k++)
                    {
                        int sy = Mathf.Clamp(y + k, 0, Height - 1);
                        sum += temp[sy * Width + x];
                        count++;
                    }
                    int index = y * Width + x;
                    var color = Pixels[index];
                    color.a = sum / count;
                    Pixels[index] = color;
                }
            }
        }

        public void Grain(float amount, int seed)
        {
            if (amount <= 0.0001f) return;
            var random = new System.Random(seed);
            for (int i = 0; i < Pixels.Length; i++)
            {
                if (Pixels[i].a <= 0.002f) continue;
                float n = (float)random.NextDouble() - 0.5f;
                var color = Pixels[i];
                color.r = Mathf.Clamp01(color.r + n * amount);
                color.g = Mathf.Clamp01(color.g + n * amount);
                color.b = Mathf.Clamp01(color.b + n * amount);
                Pixels[i] = color;
            }
        }

        /// <summary>Copies another painter's pixels in at an offset (used for atlas packing).</summary>
        public void Blit(TexturePainter source, int destinationX, int destinationY)
        {
            for (int y = 0; y < source.Height; y++)
            {
                int dy = destinationY + y;
                if (dy < 0 || dy >= Height) continue;
                for (int x = 0; x < source.Width; x++)
                {
                    int dx = destinationX + x;
                    if (dx < 0 || dx >= Width) continue;
                    var color = source.Pixels[y * source.Width + x];
                    if (color.a <= 0.002f) continue;
                    Pixels[dy * Width + dx] = color;
                }
            }
        }

        public Texture2D ToTexture(string textureName, bool mipmaps = false)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, mipmaps, false) { name = textureName };
            texture.SetPixels(Pixels);
            texture.Apply(mipmaps, false);
            return texture;
        }

        public byte[] EncodeToPng(string textureName = "tmp")
        {
            var texture = ToTexture(textureName);
            byte[] bytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            return bytes;
        }
    }
}
