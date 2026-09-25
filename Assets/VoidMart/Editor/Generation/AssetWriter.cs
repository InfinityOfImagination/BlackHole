using System.IO;
using UnityEditor;
using UnityEngine;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Disk side of the generators.  Everything is written in place so re-running the build keeps
    /// existing GUIDs - scenes, prefabs and configs never lose their references when the art is
    /// regenerated with new colours.
    /// </summary>
    public static class AssetWriter
    {
        public const string Root = "Assets/VoidMart/Generated";
        public const string ConfigFolder = Root + "/Config";
        public const string MeshFolder = Root + "/Meshes";
        public const string MaterialFolder = Root + "/Materials";
        public const string TextureFolder = Root + "/Textures";
        public const string SpriteFolder = Root + "/Sprites";
        public const string AudioFolder = Root + "/Audio";
        public const string PrefabFolder = Root + "/Prefabs";
        public const string SceneFolder = Root + "/Scenes";

        public static void EnsureFolders()
        {
            EnsureFolder(Root);
            EnsureFolder(ConfigFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(TextureFolder);
            EnsureFolder(SpriteFolder);
            EnsureFolder(AudioFolder);
            EnsureFolder(PrefabFolder);
            EnsureFolder(SceneFolder);
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ------------------------------------------------------------- textures

        public static Sprite WriteSprite(string key, TexturePainter painter, Vector4 border, float pixelsPerUnit = 100f)
        {
            string path = SpriteFolder + "/" + key + ".png";
            File.WriteAllBytes(path, painter.EncodeToPng(key));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = border;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = Mathf.Max(painter.Width, painter.Height) <= 512 ? 512 : 2048;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static Texture2D WriteTexture(string key, TexturePainter painter, bool tileable, bool mipmaps = true)
        {
            string path = TextureFolder + "/" + key + ".png";
            File.WriteAllBytes(path, painter.EncodeToPng(key));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = mipmaps;
                importer.wrapMode = tileable ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.anisoLevel = tileable ? 4 : 1;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Font atlases stay uncompressed and unfiltered-clamped so glyph edges stay crisp.</summary>
        public static Texture2D WriteFontAtlas(string key, Texture2D atlas)
        {
            string path = TextureFolder + "/" + key + ".png";
            File.WriteAllBytes(path, atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 4096;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // --------------------------------------------------------------- meshes

        public static Mesh WriteMesh(string key, Mesh source)
        {
            string path = MeshFolder + "/" + key + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                source.name = key;
                AssetDatabase.CreateAsset(source, path);
                return source;
            }

            existing.Clear();
            existing.indexFormat = source.indexFormat;
            existing.vertices = source.vertices;
            existing.normals = source.normals;
            existing.uv = source.uv;
            existing.colors = source.colors;
            existing.triangles = source.triangles;
            existing.RecalculateBounds();
            existing.RecalculateTangents();
            existing.name = key;
            Object.DestroyImmediate(source);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // ------------------------------------------------------------ materials

        public static Material WriteMaterial(string key, Shader shader)
        {
            string path = MaterialFolder + "/" + key + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                if (shader != null && existing.shader != shader) existing.shader = shader;
                return existing;
            }

            if (shader == null)
            {
                Debug.LogError($"[VoidMart] Missing shader for material '{key}'.");
                return null;
            }
            var material = new Material(shader) { name = key };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------------------------------------------------------------- audio

        public static AudioClip WriteAudio(AudioLibrary.Clip clip)
        {
            string path = AudioFolder + "/" + clip.Key + ".wav";
            File.WriteAllBytes(path, AudioSynth.EncodeWav(clip.Samples, clip.SampleRate, clip.Channels));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(path) is AudioImporter importer)
            {
                var settings = importer.defaultSampleSettings;
                bool isMusic = clip.Key.StartsWith("music", System.StringComparison.Ordinal);
                settings.loadType = isMusic ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = isMusic ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                settings.quality = isMusic ? 0.7f : 1f;
                settings.preloadAudioData = !isMusic;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = clip.Channels == 1;
                importer.loadInBackground = isMusic;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ------------------------------------------------- scriptable objects

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        public static GameObject WritePrefab(GameObject instance, string key)
        {
            string path = PrefabFolder + "/" + key + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        public static void Save()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
