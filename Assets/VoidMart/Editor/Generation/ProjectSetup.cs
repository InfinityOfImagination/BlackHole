using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using VoidMart.Data;
using VoidMart.Gameplay;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Project-level plumbing the game needs before anything else can be generated: layers, tags,
    /// build settings, player settings and the URP tweaks the stencil hole depends on.
    /// </summary>
    public static class ProjectSetup
    {
        public static readonly string[] RequiredLayers =
        {
            Layers.GroundName,
            Layers.SwallowableName,
            Layers.SubGroundName,
            Layers.PlayerName,
            Layers.StoreName
        };

        public static readonly string[] RequiredTags = { "VoidMartPlayer", "VoidMartStore" };

        public static void ApplyAll(GameConfig config)
        {
            EnsureLayers(RequiredLayers);
            EnsureTags(RequiredTags);
            ApplyPlayerSettings(config);
            ConfigureUniversalRenderPipeline();
            Layers.Invalidate();
        }

        // --------------------------------------------------------------- layers

        public static void EnsureLayers(IEnumerable<string> layerNames)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0)
            {
                Debug.LogWarning("[VoidMart] Could not open TagManager.asset - layers must be added manually.");
                return;
            }

            var serialized = new SerializedObject(asset[0]);
            var layers = serialized.FindProperty("layers");
            if (layers == null) return;

            foreach (string name in layerNames)
            {
                if (string.IsNullOrEmpty(name)) continue;

                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                {
                    if (layers.GetArrayElementAtIndex(i).stringValue != name) continue;
                    exists = true;
                    break;
                }
                if (exists) continue;

                // User layers start at index 8; 3, 6 and 7 are reserved but unnamed.
                bool placed = false;
                for (int i = 8; i < layers.arraySize; i++)
                {
                    var element = layers.GetArrayElementAtIndex(i);
                    if (!string.IsNullOrEmpty(element.stringValue)) continue;
                    element.stringValue = name;
                    placed = true;
                    break;
                }
                if (!placed) Debug.LogWarning($"[VoidMart] No free user layer slot for '{name}'.");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureTags(IEnumerable<string> tagNames)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0) return;

            var serialized = new SerializedObject(asset[0]);
            var tags = serialized.FindProperty("tags");
            if (tags == null) return;

            foreach (string name in tagNames)
            {
                bool exists = false;
                for (int i = 0; i < tags.arraySize; i++)
                {
                    if (tags.GetArrayElementAtIndex(i).stringValue != name) continue;
                    exists = true;
                    break;
                }
                if (exists) continue;

                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = name;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // -------------------------------------------------------- player settings

        public static void ApplyPlayerSettings(GameConfig config)
        {
            PlayerSettings.productName = string.IsNullOrEmpty(config.gameTitle) ? "Void Mart" : ToTitle(config.gameTitle);
            PlayerSettings.bundleVersion = config.buildVersion;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            try
            {
                var named = NamedBuildTarget.Android;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
                {
                    UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                    UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
                });
                PlayerSettings.SetScriptingBackend(named, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[VoidMart] Android player settings skipped: " + e.Message);
            }
        }

        static string ToTitle(string raw)
        {
            raw = raw.ToLowerInvariant().Replace('_', ' ');
            var builder = new System.Text.StringBuilder(raw.Length);
            bool capitalise = true;
            foreach (char c in raw)
            {
                builder.Append(capitalise ? char.ToUpperInvariant(c) : c);
                capitalise = c == ' ';
            }
            return builder.ToString();
        }

        // ------------------------------------------------------------------ URP

        /// <summary>
        /// Depth priming runs a depth prepass that can swallow the stencil writes the hole mask
        /// relies on, so it is switched off on every URP renderer in the project.
        /// </summary>
        public static void ConfigureUniversalRenderPipeline()
        {
            // Search by concrete type name - no compile-time reference to URP needed.
            string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
            int patched = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null || asset.GetType().Name != "UniversalRendererData") continue;

                var serialized = new SerializedObject(asset);
                var priming = serialized.FindProperty("m_DepthPrimingMode");
                if (priming != null && priming.enumValueIndex != 0)
                {
                    priming.enumValueIndex = 0;  // Disabled
                    patched++;
                }

                var copyDepth = serialized.FindProperty("m_CopyDepthMode");
                if (copyDepth != null && copyDepth.enumValueIndex == 0) copyDepth.enumValueIndex = 1; // after opaques

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            if (patched > 0) Debug.Log($"[VoidMart] Disabled depth priming on {patched} URP renderer(s) so the stencil hole renders correctly.");
        }

        // -------------------------------------------------------- build settings

        public static void ApplyBuildSettings(params string[] scenePaths)
        {
            var scenes = new List<EditorBuildSettingsScene>(scenePaths.Length);
            foreach (string path in scenePaths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            if (scenes.Count > 0) EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
