using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Gameplay;
using VoidMart.Puzzle;
using VoidMart.Store;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Builds the two scenes the game ships with.  Boot holds the services and the splash; Game
    /// holds the street, the storefront, the player rig and the HUD - one continuous world, so
    /// walking from the pavement into the shop never loads anything.
    /// </summary>
    public static class SceneFactory
    {
        public const string BootScenePath = AssetWriter.SceneFolder + "/Boot.unity";
        public const string GameScenePath = AssetWriter.SceneFolder + "/Game.unity";
        public const string GameSceneName = "Game";

        public static Vector2 StoreSize = new Vector2(32f, 22f);

        public static void BuildBoot(GameConfig config, GameAssets assets)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Boot Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = config.theme.voidInk;
            camera.orthographic = true;
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();

            var installer = new GameObject("Void Mart Services").AddComponent<ServiceInstaller>();
            installer.Configure(config);

            UIFactory.BuildBootCanvas(config, assets, GameSceneName);
            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetWriter.EnsureFolder(AssetWriter.SceneFolder);
            EditorSceneManager.SaveScene(scene, BootScenePath);
        }

        public static void BuildGame(GameConfig config, GameAssets assets)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ApplyRenderSettings(config, assets);

            var installer = new GameObject("Void Mart Services").AddComponent<ServiceInstaller>();
            installer.Configure(config);

            var manager = new GameObject("Game Manager").AddComponent<GameManager>();
            manager.Configure(config);

            BuildLighting(config);
            var player = BuildPlayer(config, assets);
            BuildCamera(config, player);
            BuildCity(config);
            BuildStore(config, assets);
            BuildPuzzle(config, assets);

            UIFactory.BuildGameCanvas(config, assets);
            CreateEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetWriter.EnsureFolder(AssetWriter.SceneFolder);
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        // ------------------------------------------------------------- environment

        static void ApplyRenderSettings(GameConfig config, GameAssets assets)
        {
            var theme = config.theme;
            RenderSettings.skybox = assets.skyboxMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = theme.ambientSky;
            RenderSettings.ambientEquatorColor = theme.ambientEquator;
            RenderSettings.ambientGroundColor = theme.ambientGround;
            RenderSettings.fog = theme.fogEnabled;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = theme.fogColor;
            RenderSettings.fogDensity = theme.fogDensity;
        }

        static void BuildLighting(GameConfig config)
        {
            var theme = config.theme;
            var sunObject = new GameObject("Sun");
            sunObject.transform.rotation = Quaternion.Euler(theme.sunEuler);
            var light = sunObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = theme.sunColor;
            light.intensity = theme.sunIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.65f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
        }

        static GameObject BuildPlayer(GameConfig config, GameAssets assets)
        {
            var prefab = assets.GetPrefab("player_hole");
            GameObject player = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : new GameObject("Player Hole");
            player.name = "Player";
            // Start inside the district but on the shop side, so the first run home is short.
            player.transform.position = new Vector3(0f, 0f, config.city.blocksZ * (config.city.blockSize + config.city.roadWidth) * 0.22f);

            var controller = player.GetComponent<PlayerHoleController>();
            if (controller == null) controller = player.AddComponent<PlayerHoleController>();
            controller.Configure(config);
            return player;
        }

        static void BuildCamera(GameConfig config, GameObject player)
        {
            var rigObject = new GameObject("Camera Rig");
            var cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(rigObject.transform, false);
            cameraObject.tag = "MainCamera";

            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = config.cameraRig.fieldOfView;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            cameraObject.AddComponent<AudioListener>();

            var rig = rigObject.AddComponent<CameraRig>();
            rig.Configure(config, camera, player.transform);
        }

        static void BuildCity(GameConfig config)
        {
            var cityObject = new GameObject("City");
            var generator = cityObject.AddComponent<CityGenerator>();
            generator.Configure(config);

            var staticRoot = new GameObject("City Static");
            staticRoot.transform.SetParent(cityObject.transform, false);
            var propRoot = new GameObject("City Props");
            propRoot.transform.SetParent(cityObject.transform, false);

            var serialized = new SerializedObject(generator);
            serialized.FindProperty("m_StaticRoot").objectReferenceValue = staticRoot.transform;
            serialized.FindProperty("m_PropRoot").objectReferenceValue = propRoot.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------- store

        public static float StoreCentreZ(CityConfig city)
        {
            float pitch = city.blockSize + city.roadWidth;
            return city.blocksZ * pitch * 0.5f + StoreSize.y * 0.5f + 6f;
        }

        static void BuildStore(GameConfig config, GameAssets assets)
        {
            var city = config.city;
            float centreZ = StoreCentreZ(city);

            var root = new GameObject("Storefront");
            root.transform.position = new Vector3(0f, 0f, centreZ);
            int storeLayer = LayerMask.NameToLayer(Layers.StoreName);
            if (storeLayer >= 0) root.layer = storeLayer;

            var manager = root.AddComponent<StoreManager>();
            manager.Configure(config);

            // Floor (stencil-masked so the hole cuts through the shop as well).
            var floor = new GameObject("Floor");
            floor.transform.SetParent(root.transform, false);
            var floorFilter = floor.AddComponent<MeshFilter>();
            floorFilter.sharedMesh = assets.GetMesh("mesh_quad_xz");
            var floorRenderer = floor.AddComponent<MeshRenderer>();
            floorRenderer.sharedMaterial = assets.GetMaterial("mat_store_floor");
            floorRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            floor.transform.localScale = new Vector3(StoreSize.x, 1f, StoreSize.y);

            // Approach road linking the district to the shop door.
            var approach = new GameObject("Approach");
            approach.transform.SetParent(root.transform, false);
            approach.transform.localPosition = new Vector3(0f, -0.005f, -StoreSize.y * 0.5f - 3.5f);
            var approachFilter = approach.AddComponent<MeshFilter>();
            approachFilter.sharedMesh = assets.GetMesh("mesh_quad_xz");
            var approachRenderer = approach.AddComponent<MeshRenderer>();
            approachRenderer.sharedMaterial = assets.GetMaterial("mat_store_floor");
            approachRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            approach.transform.localScale = new Vector3(12f, 1f, 8f);

            BuildWalls(root, assets, config);

            // Anchors.
            var door = new GameObject("Customer Door").transform;
            door.SetParent(root.transform, false);
            door.localPosition = new Vector3(StoreSize.x * 0.5f - 1.5f, 0f, 0f);   // inside the east wall gap
            door.localRotation = Quaternion.Euler(0f, -90f, 0f);

            var exit = new GameObject("Customer Exit").transform;
            exit.SetParent(root.transform, false);
            exit.localPosition = new Vector3(StoreSize.x * 0.5f + 4f, 0f, 0f);

            var bank = new GameObject("Robot Bank").transform;
            bank.SetParent(root.transform, false);
            bank.localPosition = new Vector3(-StoreSize.x * 0.5f + 3f, 0f, 8f);

            var nodeRoot = new GameObject("Nodes").transform;
            nodeRoot.SetParent(root.transform, false);

            var builder = root.AddComponent<StoreBuilder>();
            builder.Configure(config, nodeRoot, door, exit, bank);

            var area = root.AddComponent<AreaVolume>();
            area.Configure(new Vector3(StoreSize.x + 2f, 8f, StoreSize.y + 8f), WorldArea.Store, WorldArea.Street);
        }

        static void BuildWalls(GameObject root, GameAssets assets, GameConfig config)
        {
            var wallMaterial = assets.GetMaterial("mat_store_wall");
            var cube = assets.GetMesh("mesh_cube");
            float halfX = StoreSize.x * 0.5f;
            float halfZ = StoreSize.y * 0.5f;
            const float thickness = 0.6f;
            const float height = 3.4f;
            const float doorWidth = 9f;

            void Wall(string name, Vector3 position, Vector3 size)
            {
                var wall = new GameObject(name);
                wall.transform.SetParent(root.transform, false);
                // mesh_cube is authored sitting on y = 0, so scale maps straight to world size.
                wall.transform.localPosition = position;
                wall.transform.localScale = size;
                wall.layer = root.layer;

                var filter = wall.AddComponent<MeshFilter>();
                filter.sharedMesh = cube;
                var renderer = wall.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = wallMaterial;
            }

            Wall("Wall North", new Vector3(0f, 0f, halfZ), new Vector3(StoreSize.x, height, thickness));
            Wall("Wall West", new Vector3(-halfX, 0f, 0f), new Vector3(thickness, height, StoreSize.y));

            float sideWidth = (StoreSize.y - 6f) * 0.5f;
            Wall("Wall East A", new Vector3(halfX, 0f, halfZ - sideWidth * 0.5f), new Vector3(thickness, height, sideWidth));
            Wall("Wall East B", new Vector3(halfX, 0f, -halfZ + sideWidth * 0.5f), new Vector3(thickness, height, sideWidth));

            float southWidth = (StoreSize.x - doorWidth) * 0.5f;
            Wall("Wall South A", new Vector3(-halfX + southWidth * 0.5f, 0f, -halfZ), new Vector3(southWidth, height, thickness));
            Wall("Wall South B", new Vector3(halfX - southWidth * 0.5f, 0f, -halfZ), new Vector3(southWidth, height, thickness));

            // Shop sign over the entrance.
            var sign = new GameObject("Shop Sign");
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, height + 0.9f, -halfZ);
            sign.transform.localScale = new Vector3(doorWidth, 1.6f, 0.4f);
            var signFilter = sign.AddComponent<MeshFilter>();
            signFilter.sharedMesh = cube;
            var signRenderer = sign.AddComponent<MeshRenderer>();
            signRenderer.sharedMaterial = assets.GetMaterial("mat_glow");
        }

        static void BuildPuzzle(GameConfig config, GameAssets assets)
        {
            var pieceSet = AssetWriter.LoadOrCreate<PuzzlePieceSet>(AssetWriter.ConfigFolder + "/PuzzlePieceSet.asset");
            if (pieceSet.shapes.Count == 0) pieceSet.ResetToDefaults();
            EditorUtility.SetDirty(pieceSet);

            var controllerObject = new GameObject("Puzzle Controller");
            var controller = controllerObject.AddComponent<PuzzleController>();
            controller.Configure(config, pieceSet);
        }

        // ---------------------------------------------------------- event system

        public static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

            // This project ships with Input System handling only, so the legacy module would
            // throw at runtime. Prefer the Input System module and make sure it has actions:
            // AddComponent does not call Reset(), which is what normally assigns them.
            var moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (moduleType != null)
            {
                var module = go.AddComponent(moduleType);
                var assign = moduleType.GetMethod("AssignDefaultActions",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                try { assign?.Invoke(module, null); }
                catch (Exception e) { Debug.LogWarning("[VoidMart] Could not assign default UI actions: " + e.Message); }
            }
            else
            {
                go.AddComponent<StandaloneInputModule>();
            }
        }
    }
}
