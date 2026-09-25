using System.Collections.Generic;
using UnityEngine;
using VoidMart.Data;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Tileable ground textures.  One street tile covers exactly one city block plus its road, so
    /// the whole district is a single quad with the texture repeated blocksX x blocksZ times -
    /// one draw call for the entire street.
    /// </summary>
    public static class WorldTextureLibrary
    {
        public class Generated
        {
            public string Key;
            public TexturePainter Painter;
            public bool Tileable = true;
        }

        public static List<Generated> BuildAll(ThemeConfig theme, CityConfig city, ArtConfig art)
        {
            return new List<Generated>
            {
                new Generated { Key = "tex_street", Painter = Street(theme, city, art) },
                new Generated { Key = "tex_store_floor", Painter = StoreFloor(theme, art) },
                new Generated { Key = "tex_store_wall", Painter = StoreWall(theme, art) }
            };
        }

        static TexturePainter Street(ThemeConfig theme, CityConfig city, ArtConfig art)
        {
            int size = Mathf.Clamp(art.groundTextureSize, 128, 2048);
            var painter = new TexturePainter(size, size, theme.asphalt);

            float pitch = Mathf.Max(1f, city.blockSize + city.roadWidth);
            float roadFraction = Mathf.Clamp01(city.roadWidth / pitch);
            float roadPixels = roadFraction * size;
            float kerb = size * 0.012f;
            float pavementDepth = size * 0.055f;

            var random = new System.Random(city.randomSeed ^ 0x5EED);

            // Block interior: pavement border, then plaza.
            var blockRect = new Rect(roadPixels, roadPixels, size - roadPixels, size - roadPixels);
            painter.Shade(TexturePainter.RoundedRectSdf(blockRect, 4f), theme.sidewalk, 1.2f);
            var plaza = new Rect(blockRect.xMin + pavementDepth, blockRect.yMin + pavementDepth,
                blockRect.width - pavementDepth * 2f, blockRect.height - pavementDepth * 2f);
            painter.Shade(TexturePainter.RoundedRectSdf(plaza, 6f), Shade(theme.sidewalk, -0.12f), 1.2f);

            // Kerb highlight where pavement meets road.
            painter.Shade(TexturePainter.RoundedRectSdf(new Rect(blockRect.xMin, blockRect.yMin, blockRect.width, kerb), 1f), Shade(theme.sidewalk, 0.22f), 1f);
            painter.Shade(TexturePainter.RoundedRectSdf(new Rect(blockRect.xMin, blockRect.yMin, kerb, blockRect.height), 1f), Shade(theme.sidewalk, 0.22f), 1f);

            // Pavement slab seams.
            int slabs = 9;
            for (int i = 1; i < slabs; i++)
            {
                float t = blockRect.xMin + blockRect.width * i / slabs;
                painter.Capsule(new Vector2(t, blockRect.yMin), new Vector2(t, blockRect.yMin + pavementDepth), 1f, new Color(0f, 0f, 0f, 0.12f));
                painter.Capsule(new Vector2(blockRect.xMin, t), new Vector2(blockRect.xMin + pavementDepth, t), 1f, new Color(0f, 0f, 0f, 0.12f));
            }

            // Lane dashes down the middle of both roads.
            float laneX = roadPixels * 0.5f;
            int dashes = 8;
            for (int i = 0; i < dashes; i++)
            {
                float y0 = size * (i + 0.15f) / dashes;
                float y1 = size * (i + 0.65f) / dashes;
                painter.Capsule(new Vector2(laneX, y0), new Vector2(laneX, y1), size * 0.006f, theme.roadLine);
                painter.Capsule(new Vector2(y0, laneX), new Vector2(y1, laneX), size * 0.006f, theme.roadLine);
            }

            // Stop lines at the junction.
            painter.Shade(TexturePainter.RoundedRectSdf(new Rect(roadPixels + size * 0.01f, roadPixels * 0.06f, size * 0.10f, roadPixels * 0.88f), 2f),
                new Color(theme.roadLine.r, theme.roadLine.g, theme.roadLine.b, 0.5f), 1.2f);
            painter.Shade(TexturePainter.RoundedRectSdf(new Rect(roadPixels * 0.06f, roadPixels + size * 0.01f, roadPixels * 0.88f, size * 0.10f), 2f),
                new Color(theme.roadLine.r, theme.roadLine.g, theme.roadLine.b, 0.5f), 1.2f);

            // Manholes, patches and stains keep the asphalt from looking flat.
            for (int i = 0; i < 5; i++)
            {
                float x = (float)random.NextDouble() * roadPixels;
                float y = (float)random.NextDouble() * size;
                painter.Circle(new Vector2(x, y), size * 0.018f, Shade(theme.asphalt, 0.35f));
                painter.Ring(new Vector2(x, y), size * 0.018f, 2f, Shade(theme.asphalt, -0.3f));
            }
            for (int i = 0; i < 16; i++)
            {
                float x = (float)random.NextDouble() * size;
                float y = (float)random.NextDouble() * size;
                float r = size * (0.01f + (float)random.NextDouble() * 0.03f);
                painter.Circle(new Vector2(x, y), r, new Color(0f, 0f, 0f, 0.05f));
            }

            painter.Grain(art.textureGrain, city.randomSeed);
            return painter;
        }

        static TexturePainter StoreFloor(ThemeConfig theme, ArtConfig art)
        {
            int size = Mathf.Clamp(art.groundTextureSize, 128, 2048);
            var painter = new TexturePainter(size, size, theme.storeFloorA);
            int tiles = 4;
            float cell = size / (float)tiles;

            for (int y = 0; y < tiles; y++)
            {
                for (int x = 0; x < tiles; x++)
                {
                    bool alternate = (x + y) % 2 == 1;
                    var color = alternate ? theme.storeFloorB : theme.storeFloorA;
                    var rect = new Rect(x * cell + 2f, y * cell + 2f, cell - 4f, cell - 4f);
                    painter.Shade(TexturePainter.RoundedRectSdf(rect, cell * 0.06f), color, 1.4f);
                    painter.Shade(TexturePainter.RoundedRectSdf(new Rect(rect.xMin, rect.yMax - cell * 0.1f, rect.width, cell * 0.1f), cell * 0.05f),
                        new Color(1f, 1f, 1f, 0.16f), 1.4f);
                }
            }

            painter.Grain(art.textureGrain * 0.6f, 9812);
            return painter;
        }

        static TexturePainter StoreWall(ThemeConfig theme, ArtConfig art)
        {
            int size = Mathf.Clamp(art.groundTextureSize / 2, 64, 1024);
            var painter = new TexturePainter(size, size, theme.storeWall);
            for (int i = 0; i < 6; i++)
            {
                float y = size * (i + 0.5f) / 6f;
                painter.Capsule(new Vector2(0f, y), new Vector2(size, y), 1f, new Color(0f, 0f, 0f, 0.05f));
            }
            painter.Shade(TexturePainter.RoundedRectSdf(new Rect(0f, 0f, size, size * 0.12f), 0f), Shade(theme.storeWall, -0.12f), 1.2f);
            painter.Grain(art.textureGrain * 0.4f, 5512);
            return painter;
        }

        static Color Shade(Color color, float amount) => new Color(
            Mathf.Clamp01(color.r * (1f + amount)),
            Mathf.Clamp01(color.g * (1f + amount)),
            Mathf.Clamp01(color.b * (1f + amount)), color.a);
    }
}
