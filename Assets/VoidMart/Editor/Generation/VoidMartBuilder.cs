using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Puzzle;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// The one-click setup.  Runs every generator in dependency order and leaves the project in a
    /// playable state: configuration assets, art, audio, prefabs, both scenes and the build list.
    /// Safe to re-run - assets are written in place so nothing loses its references.
    /// </summary>
    public static class VoidMartBuilder
    {
        public const string ConfigPath = AssetWriter.ConfigFolder + "/GameConfig.asset";
        public const string AssetsPath = AssetWriter.ConfigFolder + "/GameAssets.asset";
        public const string FontPath = AssetWriter.ConfigFolder + "/VMDisplayFont.asset";
        public const string EventFolder = AssetWriter.ConfigFolder + "/Events";
        public const string NodeFolder = AssetWriter.ConfigFolder + "/Nodes";

        [MenuItem("Tools/Void Mart/Build Everything (One-Click Setup)", false, 0)]
        public static void BuildEverythingMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildEverything(true);
        }

        public static GameConfig BuildEverything(bool interactive)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            GameConfig config = null;

            try
            {
                AssetDatabase.StartAssetEditing();
                AssetWriter.EnsureFolders();
                AssetWriter.EnsureFolder(EventFolder);
                AssetWriter.EnsureFolder(NodeFolder);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            try
            {
                Step(interactive, "Configuration", 0.02f);
                config = EnsureConfig();

                Step(interactive, "Project settings", 0.08f);
                ProjectSetup.ApplyAll(config);

                Step(interactive, "Content tables", 0.14f);
                BuildContent(config);

                Step(interactive, "Meshes", 0.22f);
                BuildMeshes(config);

                Step(interactive, "Textures, sprites and font", 0.38f);
                BuildArt(config);

                Step(interactive, "Materials", 0.55f);
                MaterialFactory.BuildAll(config, config.assets);
                AttachGroundTextures(config);

                Step(interactive, "Audio", 0.62f);
                BuildAudio(config);

                Step(interactive, "Prefabs", 0.78f);
                PrefabFactory.BuildAll(config, config.assets);

                Step(interactive, "Store nodes", 0.86f);
                BuildNodes(config);

                Step(interactive, "Scenes", 0.92f);
                SceneFactory.BuildBoot(config, config.assets);
                SceneFactory.BuildGame(config, config.assets);
                ProjectSetup.ApplyBuildSettings(SceneFactory.BootScenePath, SceneFactory.GameScenePath);

                EditorUtility.SetDirty(config);
                EditorUtility.SetDirty(config.assets);
                AssetWriter.Save();

                stopwatch.Stop();
                Debug.Log($"[VoidMart] Build complete in {stopwatch.Elapsed.TotalSeconds:0.0}s. " +
                          $"{config.props.Count} props, {config.products.Count} products, " +
                          $"{config.furnishingNodes.Count} store nodes, {config.assets.meshes.Count} meshes, " +
                          $"{config.assets.sprites.Count} sprites, {config.assets.clips.Count} audio clips.");

                if (interactive)
                {
                    EditorSceneManager.OpenScene(SceneFactory.GameScenePath, OpenSceneMode.Single);
                    Selection.activeObject = config;
                    EditorGUIUtility.PingObject(config);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (interactive) EditorUtility.DisplayDialog("Void Mart", "Build failed:\n" + e.Message + "\n\nSee the Console for details.", "OK");
            }
            finally
            {
                if (interactive) EditorUtility.ClearProgressBar();
            }

            return config;
        }

        static void Step(bool interactive, string label, float progress)
        {
            if (interactive) EditorUtility.DisplayProgressBar("Void Mart — building", label, progress);
        }

        // ------------------------------------------------------------- config

        public static GameConfig EnsureConfig()
        {
            var config = AssetWriter.LoadOrCreate<GameConfig>(ConfigPath);
            var assets = AssetWriter.LoadOrCreate<GameAssets>(AssetsPath);
            var font = AssetWriter.LoadOrCreate<VMFontAsset>(FontPath);

            assets.displayFont = font;
            config.assets = assets;
            config.events = EnsureEventBus();

            EditorUtility.SetDirty(config);
            EditorUtility.SetDirty(assets);
            return config;
        }

        static GameEventBus EnsureEventBus()
        {
            var bus = AssetWriter.LoadOrCreate<GameEventBus>(AssetWriter.ConfigFolder + "/GameEventBus.asset");

            bus.cashChanged = Channel<DoubleChannel>("Evt_CashChanged", "Soft currency total changed.");
            bus.gemsChanged = Channel<IntChannel>("Evt_GemsChanged", "Hard currency total changed.");
            bus.xpChanged = Channel<FloatChannel>("Evt_XpChanged", "Normalised XP toward the next level.");
            bus.playerLevelUp = Channel<IntChannel>("Evt_LevelUp", "Player reached a new level.");
            bus.productSold = Channel<SaleChannel>("Evt_ProductSold", "A customer paid at the register.");

            bus.propSwallowed = Channel<SwallowChannel>("Evt_PropSwallowed", "A city prop went down the hole.");
            bus.holeFillChanged = Channel<FloatChannel>("Evt_HoleFill", "Hole capacity 0..1.");
            bus.holeTierChanged = Channel<IntChannel>("Evt_HoleTier", "Hole crossed a size tier.");
            bus.holeFull = Channel<SignalChannel>("Evt_HoleFull", "Hole cannot hold any more.");
            bus.unloadRate = Channel<FloatChannel>("Evt_UnloadRate", "Normalised transfer rate into a hopper.");
            bus.unloadFinished = Channel<SignalChannel>("Evt_UnloadFinished", "Hole emptied into a hopper.");
            bus.feverStarted = Channel<SignalChannel>("Evt_FeverStarted", "Rewarded fever mode began.");
            bus.feverEnded = Channel<SignalChannel>("Evt_FeverEnded", "Fever mode expired.");

            bus.nodePurchased = Channel<StringChannel>("Evt_NodePurchased", "A furnishing node was bought.");
            bus.machineJammed = Channel<StringChannel>("Evt_MachineJammed", "A crafter jammed.");
            bus.machineRepaired = Channel<StringChannel>("Evt_MachineRepaired", "A crafter was unjammed.");
            bus.areaChanged = Channel<IntChannel>("Evt_AreaChanged", "Player moved between street and store.");

            bus.puzzleRequested = Channel<StringChannel>("Evt_PuzzleRequested", "Open the unjam minigame.");
            bus.puzzleCompleted = Channel<PuzzleChannel>("Evt_PuzzleCompleted", "Unjam minigame result.");

            bus.stateChanged = Channel<GameStateChannel>("Evt_StateChanged", "High level game state.");
            bus.toast = Channel<ToastChannel>("Evt_Toast", "Show a banner message.");
            bus.saveRequested = Channel<SignalChannel>("Evt_SaveRequested", "Force an immediate save.");
            bus.dataReloaded = Channel<SignalChannel>("Evt_DataReloaded", "Save data was replaced.");

            EditorUtility.SetDirty(bus);
            return bus;
        }

        static T Channel<T>(string assetName, string description) where T : GameEventChannelBase
        {
            var channel = AssetWriter.LoadOrCreate<T>(EventFolder + "/" + assetName + ".asset");
            channel.description = description;
            EditorUtility.SetDirty(channel);
            return channel;
        }

        // ------------------------------------------------------------- content

        public static void BuildContent(GameConfig config)
        {
            config.props = ContentFactory.BuildProps(config.theme);
            config.products = ContentFactory.BuildProducts(config.theme);
            config.upgrades = ContentFactory.BuildUpgrades(config.theme);
            EditorUtility.SetDirty(config);
        }

        // -------------------------------------------------------------- meshes

        public static void BuildMeshes(GameConfig config)
        {
            var assets = config.assets;
            var builder = new MeshBuilder();
            var recipes = MeshLibrary.Recipes();

            foreach (var kv in recipes)
            {
                builder.Clear();
                try
                {
                    kv.Value(builder, config.theme, config.art);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[VoidMart] Mesh '{kv.Key}' failed: {e.Message}");
                    continue;
                }

                var mesh = AssetWriter.WriteMesh(kv.Key, builder.ToMesh(kv.Key));
                assets.SetMesh(kv.Key, mesh);
            }

            assets.InvalidateCaches();
            EditorUtility.SetDirty(assets);
        }

        // ----------------------------------------------------------------- art

        public static void BuildArt(GameConfig config)
        {
            var assets = config.assets;

            // Display font first: the wordmark sprite is drawn with it.
            var fontResult = FontGenerator.Build(config.art);
            var atlas = AssetWriter.WriteFontAtlas("font_display", fontResult.atlas);
            var font = assets.displayFont != null ? assets.displayFont : AssetWriter.LoadOrCreate<VMFontAsset>(FontPath);
            font.atlas = atlas;
            font.pixelHeight = config.art.fontPixelHeight;
            font.glyphs = fontResult.glyphs;
            font.ascender = fontResult.ascender;
            font.descender = fontResult.descender;
            font.spaceAdvance = fontResult.spaceAdvance;
            font.lineHeight = 1.24f;
            font.InvalidateCache();
            assets.displayFont = font;
            EditorUtility.SetDirty(font);

            foreach (var generated in SpriteLibrary.BuildAll(config.theme, config.art))
            {
                var sprite = AssetWriter.WriteSprite(generated.Key, generated.Painter, generated.Border, generated.PixelsPerUnit);
                assets.SetSprite(generated.Key, sprite);
            }

            foreach (var generated in WorldTextureLibrary.BuildAll(config.theme, config.city, config.art))
            {
                var texture = AssetWriter.WriteTexture(generated.Key, generated.Painter, generated.Tileable);
                s_GroundTextures[generated.Key] = texture;
            }

            assets.InvalidateCaches();
            EditorUtility.SetDirty(assets);
        }

        static readonly Dictionary<string, Texture2D> s_GroundTextures = new Dictionary<string, Texture2D>(4);

        static void AttachGroundTextures(GameConfig config)
        {
            s_GroundTextures.TryGetValue("tex_street", out var street);
            s_GroundTextures.TryGetValue("tex_store_floor", out var floor);
            s_GroundTextures.TryGetValue("tex_store_wall", out var wall);
            MaterialFactory.AssignTextures(config.assets, street, floor, wall, config.city);
        }

        // --------------------------------------------------------------- audio

        public static void BuildAudio(GameConfig config)
        {
            var assets = config.assets;
            foreach (var clip in AudioLibrary.BuildAll(config.audio))
            {
                var imported = AssetWriter.WriteAudio(clip);
                assets.SetClip(clip.Key, imported);
            }
            assets.InvalidateCaches();
            EditorUtility.SetDirty(assets);
        }

        // --------------------------------------------------------------- nodes

        public static void BuildNodes(GameConfig config)
        {
            var assets = config.assets;
            var specs = ContentFactory.BuildNodeSpecs();

            config.furnishingNodes.Clear();
            config.startingUnlockedNodeIds.Clear();

            foreach (var spec in specs)
            {
                var node = AssetWriter.LoadOrCreate<FurnishingNodeData>(NodeFolder + "/" + spec.Id + ".asset");
                node.NodeID = spec.Id;
                node.TierLevel = spec.Tier;
                node.BaseCost = spec.BaseCost;
                node.Category = spec.Category;
                node.DependentNodeIDs = new List<string>(spec.Dependencies);
                node.displayName = spec.DisplayName;
                node.prefabKey = spec.PrefabKey;
                node.iconKey = spec.IconKey;
                node.localPosition = spec.Position;
                node.yaw = spec.Yaw;
                node.zoneSize = spec.ZoneSize;
                node.productId = spec.ProductId;
                node.rateMultiplier = spec.RateMultiplier;
                node.capacityMultiplier = spec.CapacityMultiplier;
                node.unlockedFromStart = spec.FromStart;
                node.accent = config.theme.neonCyan;
                node.PrefabToSpawn = assets.GetPrefab(spec.PrefabKey);

                EditorUtility.SetDirty(node);
                config.furnishingNodes.Add(node);
                if (spec.FromStart) config.startingUnlockedNodeIds.Add(spec.Id);
            }

            EditorUtility.SetDirty(config);
        }

        // ------------------------------------------------------------ utilities

        public static GameConfig FindConfig() => AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);

        [MenuItem("Tools/Void Mart/Open Game Scene", false, 20)]
        public static void OpenGameScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (System.IO.File.Exists(SceneFactory.GameScenePath))
                EditorSceneManager.OpenScene(SceneFactory.GameScenePath, OpenSceneMode.Single);
            else
                EditorUtility.DisplayDialog("Void Mart", "The Game scene has not been generated yet. Run the one-click setup first.", "OK");
        }

        [MenuItem("Tools/Void Mart/Select Game Config", false, 21)]
        public static void SelectConfig()
        {
            var config = FindConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("Void Mart", "No GameConfig yet. Run the one-click setup first.", "OK");
                return;
            }
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }
    }
}
