using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Gameplay;
using VoidMart.Store;
using VoidMart.UI;

namespace VoidMart.EditorTools
{
    /// <summary>
    /// Assembles every runtime prefab: swallowable props, flying effects, the player's hole rig,
    /// and the storefront devices.  Components are wired here so the prefabs work standalone and
    /// nothing has to be hooked up by hand in the scene.
    /// </summary>
    public static class PrefabFactory
    {
        public static void BuildAll(GameConfig config, GameAssets assets)
        {
            BuildProps(config, assets);
            BuildEffects(config, assets);
            BuildPlayer(config, assets);
            BuildStore(config, assets);
            BuildCustomers(config, assets);
            EditorUtility.SetDirty(assets);
        }

        // ---------------------------------------------------------------- utils

        static GameObject NewRoot(string name, int layer)
        {
            var go = new GameObject(name);
            go.layer = layer;
            return go;
        }

        static GameObject AddVisual(GameObject parent, GameAssets assets, string meshKey, string materialKey = "mat_clay",
            Vector3 localPosition = default, Vector3? localEuler = null, float scale = 1f, string childName = "Visual")
        {
            var mesh = assets.GetMesh(meshKey);
            var material = assets.GetMaterial(materialKey);
            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            if (localEuler.HasValue) go.transform.localEulerAngles = localEuler.Value;
            go.transform.localScale = Vector3.one * scale;
            go.layer = parent.layer;

            if (mesh != null)
            {
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            return go;
        }

        static Transform Anchor(GameObject parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            go.layer = parent.layer;
            return go.transform;
        }

        static Bounds MeshBounds(GameAssets assets, string meshKey)
        {
            var mesh = assets.GetMesh(meshKey);
            return mesh != null ? mesh.bounds : new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);
        }

        // ---------------------------------------------------------------- props

        static void BuildProps(GameConfig config, GameAssets assets)
        {
            int layer = LayerMask.NameToLayer(Layers.SwallowableName);
            if (layer < 0) layer = 0;

            for (int i = 0; i < config.props.Count; i++)
            {
                var definition = config.props[i];
                if (definition == null) continue;

                var root = NewRoot("Prop_" + definition.id, layer);
                var visual = AddVisual(root, assets, definition.meshKey);

                var bounds = MeshBounds(assets, definition.meshKey);
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.mass = Mathf.Max(0.1f, definition.mass);
                body.interpolation = RigidbodyInterpolation.None;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;

                var collider = root.AddComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = new Vector3(
                    Mathf.Max(0.15f, bounds.size.x),
                    Mathf.Max(0.15f, bounds.size.y),
                    Mathf.Max(0.15f, bounds.size.z));

                var swallowable = root.AddComponent<Swallowable>();
                var serialized = new SerializedObject(swallowable);
                serialized.FindProperty("m_Visual").objectReferenceValue = visual.transform;
                serialized.FindProperty("m_Body").objectReferenceValue = body;
                serialized.FindProperty("m_Collider").objectReferenceValue = collider;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (definition.category == PropCategory.Pedestrian)
                {
                    var pedestrian = root.AddComponent<Pedestrian>();
                    var pedestrianSerialized = new SerializedObject(pedestrian);
                    pedestrianSerialized.FindProperty("m_Visual").objectReferenceValue = visual.transform;
                    pedestrianSerialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var prefab = AssetWriter.WritePrefab(root, "prop_" + definition.id);
                assets.SetPrefab("prop_" + definition.id, prefab);
            }
        }

        // -------------------------------------------------------------- effects

        static void BuildEffects(GameConfig config, GameAssets assets)
        {
            BuildFlyingItem(assets, "fx_cash_bill", "mesh_cash_bill", 1.6f);
            BuildFlyingItem(assets, "fx_product_box", "mesh_product_box", 1f);
            BuildFlyingItem(assets, "fx_junk", "mesh_junk_chunk", 1.1f);
            BuildPoof(config, assets);
        }

        static void BuildFlyingItem(GameAssets assets, string key, string meshKey, float scale)
        {
            var root = NewRoot(key, 0);
            var visual = AddVisual(root, assets, meshKey, "mat_clay", Vector3.zero, null, scale);
            visual.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var item = root.AddComponent<FlyingItem>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("m_Visual").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = AssetWriter.WritePrefab(root, key);
            assets.SetPrefab(key, prefab);
        }

        static void BuildPoof(GameConfig config, GameAssets assets)
        {
            var root = NewRoot("fx_poof", 0);
            var particles = root.AddComponent<ParticleSystem>();
            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = assets.GetMaterial("mat_particle");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var main = particles.main;
            main.duration = 0.7f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            main.gravityModifier = 0.55f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            main.startColor = new ParticleSystem.MinMaxGradient(config.theme.neonCyan, config.theme.hotMagenta);

            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            root.AddComponent<AutoDespawn>().Configure(1.1f);

            var prefab = AssetWriter.WritePrefab(root, "fx_poof");
            assets.SetPrefab("fx_poof", prefab);
        }

        // --------------------------------------------------------------- player

        static void BuildPlayer(GameConfig config, GameAssets assets)
        {
            int layer = LayerMask.NameToLayer(Layers.PlayerName);
            if (layer < 0) layer = 0;

            var root = NewRoot("Player Hole", layer);
            var controller = root.AddComponent<PlayerHoleController>();
            controller.Configure(config);

            // Stencil writer: a flat quad lying on the ground that punches the street open.
            var mask = AddVisual(root, assets, "mesh_quad_xy", "mat_hole_mask",
                new Vector3(0f, 0.02f, 0f), new Vector3(90f, 0f, 0f), 1f, "Stencil Mask");
            var maskRenderer = mask.GetComponent<MeshRenderer>();
            if (maskRenderer != null)
            {
                maskRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                maskRenderer.receiveShadows = false;
            }

            // The cavity itself, sitting under the street and only visible through the stencil.
            var pit = AddVisual(root, assets, "mesh_pit_tube", "mat_pit",
                new Vector3(0f, 0.01f, 0f), null, 1f, "Pit Interior");
            var pitRenderer = pit.GetComponent<MeshRenderer>();
            if (pitRenderer != null)
            {
                pitRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                pitRenderer.receiveShadows = false;
            }

            var rim = AddVisual(root, assets, "mesh_hole_ring", "mat_glow",
                new Vector3(0f, 0.03f, 0f), null, 1f, "Event Horizon");
            var rimRenderer = rim.GetComponent<MeshRenderer>();
            if (rimRenderer != null)
            {
                rimRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rimRenderer.receiveShadows = false;
            }

            var visualRoot = Anchor(root, "Visual Root", Vector3.zero);
            controller.BindVisuals(visualRoot, mask.transform, pit.transform, rim.transform);

            // Way-finding arrow, shown once the hole is worth emptying.
            var arrow = AddVisual(root, assets, "mesh_arrow", "mat_glow", Vector3.zero, null, 1f, "Store Arrow");
            var arrowRenderer = arrow.GetComponent<MeshRenderer>();
            if (arrowRenderer != null) arrowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arrow.SetActive(false);
            root.AddComponent<NavigationHint>().BindArrow(arrow.transform);

            var prefab = AssetWriter.WritePrefab(root, "player_hole");
            assets.SetPrefab("player_hole", prefab);
        }

        // ---------------------------------------------------------------- store

        static void BuildStore(GameConfig config, GameAssets assets)
        {
            int layer = LayerMask.NameToLayer(Layers.StoreName);
            if (layer < 0) layer = 0;

            BuildHopper(config, assets, layer);
            BuildMachine(config, assets, layer);
            BuildShelf(config, assets, layer);
            BuildRegister(config, assets, layer);
            BuildRobot(config, assets, layer);
            BuildZonePad(config, assets, layer);

            BuildDecor(assets, layer, "store_conveyor", "mesh_conveyor");
            BuildDecor(assets, layer, "store_plant", "mesh_plant");
            BuildDecor(assets, layer, "store_sign", "mesh_sign");
            BuildDecor(assets, layer, "store_lamp", "mesh_floor_lamp");
        }

        static void BuildDecor(GameAssets assets, int layer, string key, string meshKey)
        {
            var root = NewRoot(key, layer);
            AddVisual(root, assets, meshKey);
            var prefab = AssetWriter.WritePrefab(root, key);
            assets.SetPrefab(key, prefab);
        }

        static StackVisual AddStack(GameObject parent, GameAssets assets, string name, Vector3 localPosition,
            string meshKey, int maxVisible, Vector3 cell, int columns, int rows, float itemScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPosition;
            go.layer = parent.layer;

            var stack = go.AddComponent<StackVisual>();
            var serialized = new SerializedObject(stack);
            serialized.FindProperty("m_Cell").vector3Value = cell;
            serialized.FindProperty("m_Columns").intValue = columns;
            serialized.FindProperty("m_Rows").intValue = rows;
            serialized.FindProperty("m_MaxVisible").intValue = maxVisible;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // The stack builds its renderers at runtime from these keys.
            var binder = go.AddComponent<StackBinder>();
            var binderSerialized = new SerializedObject(binder);
            binderSerialized.FindProperty("m_MeshKey").stringValue = meshKey;
            binderSerialized.FindProperty("m_MaterialKey").stringValue = "mat_clay";
            binderSerialized.FindProperty("m_MaxVisible").intValue = maxVisible;
            binderSerialized.FindProperty("m_Cell").vector3Value = cell;
            binderSerialized.FindProperty("m_Columns").intValue = columns;
            binderSerialized.FindProperty("m_Rows").intValue = rows;
            binderSerialized.FindProperty("m_ItemScale").floatValue = itemScale;
            binderSerialized.ApplyModifiedPropertiesWithoutUndo();

            return stack;
        }

        static void BuildHopper(GameConfig config, GameAssets assets, int layer)
        {
            var root = NewRoot("store_hopper", layer);
            AddVisual(root, assets, "mesh_hopper");
            var mouth = Anchor(root, "Mouth", new Vector3(0f, 2.1f, 0f));
            var pad = Anchor(root, "Pad", new Vector3(0f, 0f, -2.4f));

            var hopper = root.AddComponent<Hopper>();
            hopper.Configure(config, mouth, pad, 3.2f);

            var prefab = AssetWriter.WritePrefab(root, "store_hopper");
            assets.SetPrefab("store_hopper", prefab);
        }

        static void BuildMachine(GameConfig config, GameAssets assets, int layer)
        {
            var root = NewRoot("store_machine", layer);
            var shakeRoot = Anchor(root, "Shake Root", Vector3.zero);

            var visual = AddVisual(shakeRoot.gameObject, assets, "mesh_machine");
            var output = Anchor(root, "Output", new Vector3(0f, 1.9f, -0.95f));
            var jamIcon = AddVisual(root, assets, "mesh_wrench", "mat_glow", new Vector3(0f, 3.1f, 0f), null, 1.6f, "Jam Icon");
            jamIcon.SetActive(false);

            var stack = AddStack(root, assets, "Output Stack", new Vector3(0f, 2.02f, -0.95f),
                "mesh_product_box", 12, new Vector3(0.42f, 0.34f, 0.42f), 2, 2, 0.9f);

            var machine = root.AddComponent<CraftingMachine>();
            machine.Configure(config, "crusher_machine_01", "product_chair", 1f);
            machine.BindVisuals(output, jamIcon.transform, shakeRoot, stack);

            var prefab = AssetWriter.WritePrefab(root, "store_machine");
            assets.SetPrefab("store_machine", prefab);
        }

        static void BuildShelf(GameConfig config, GameAssets assets, int layer)
        {
            var root = NewRoot("store_shelf", layer);
            AddVisual(root, assets, "mesh_shelf");
            var drop = Anchor(root, "Drop Point", new Vector3(0f, 1.6f, 0f));
            var pick = Anchor(root, "Pick Point", new Vector3(0f, 0f, 1.6f));
            var stack = AddStack(root, assets, "Stack", new Vector3(0f, 0.5f, 0f),
                "mesh_product_box", 18, new Vector3(0.62f, 0.52f, 0.42f), 3, 2, 1.1f);

            var shelf = root.AddComponent<ProductShelf>();
            shelf.Configure(config, "", 1f);
            shelf.BindVisuals(drop, pick, stack);

            var prefab = AssetWriter.WritePrefab(root, "store_shelf");
            assets.SetPrefab("store_shelf", prefab);
        }

        static void BuildRegister(GameConfig config, GameAssets assets, int layer)
        {
            var root = NewRoot("store_register", layer);
            AddVisual(root, assets, "mesh_counter");

            var serve = Anchor(root, "Serve Point", new Vector3(0f, 0f, 1.5f));
            var cash = Anchor(root, "Cash Point", new Vector3(0.9f, 1.25f, 0f));
            var queue = new Transform[4];
            for (int i = 0; i < queue.Length; i++)
                queue[i] = Anchor(root, "Queue " + i, new Vector3(0f, 0f, 1.5f + (i + 1) * 1.25f));

            var cashStack = AddStack(root, assets, "Cash Stack", new Vector3(0.9f, 1.12f, 0f),
                "mesh_cash_bill", 16, new Vector3(0.36f, 0.09f, 0.2f), 2, 2, 1.1f);

            var checkout = root.AddComponent<Checkout>();
            checkout.Configure(config);
            checkout.BindVisuals(queue, serve, cash, cashStack);

            var door = Anchor(root, "Customer Door", new Vector3(3.5f, 0f, 8f));
            var spawnerObject = new GameObject("Customer Spawner");
            spawnerObject.transform.SetParent(root.transform, false);
            spawnerObject.transform.localPosition = door.localPosition;
            var spawner = spawnerObject.AddComponent<CustomerSpawner>();
            spawner.Configure(config, door);

            var prefab = AssetWriter.WritePrefab(root, "store_register");
            assets.SetPrefab("store_register", prefab);
        }

        static void BuildRobot(GameConfig config, GameAssets assets, int layer)
        {
            var root = NewRoot("store_robot", layer);
            var visual = AddVisual(root, assets, "mesh_robot");
            var stack = AddStack(root, assets, "Carry", new Vector3(0f, 1.35f, 0f),
                "mesh_cash_bill", 6, new Vector3(0.3f, 0.09f, 0.2f), 2, 1, 1f);

            var robot = root.AddComponent<RobotHelper>();
            var serialized = new SerializedObject(robot);
            serialized.FindProperty("m_Visual").objectReferenceValue = visual.transform;
            serialized.FindProperty("m_CarryStack").objectReferenceValue = stack;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = AssetWriter.WritePrefab(root, "store_robot");
            assets.SetPrefab("store_robot", prefab);
        }

        static void BuildZonePad(GameConfig config, GameAssets assets, int layer)
        {
            var root = NewRoot("zone_pad", layer);
            var visualRoot = Anchor(root, "Visual Root", Vector3.zero);

            var pad = AddVisual(visualRoot.gameObject, assets, "mesh_zone_pad", "mat_glow", new Vector3(0f, 0.015f, 0f), null, 1f, "Pad");
            var padRenderer = pad.GetComponent<MeshRenderer>();

            var fill = AddVisual(visualRoot.gameObject, assets, "mesh_quad_xz", "mat_glow", new Vector3(0f, 0.02f, 0f), null, 1f, "Fill");
            fill.transform.localScale = new Vector3(0f, 1f, 1f);

            var marker = Anchor(root, "Marker", new Vector3(0f, 1.4f, 0f));
            UIFactory.BuildWorldLabel(config, assets, marker, "Node", "$0");

            var zone = root.AddComponent<FurnishingZone>();
            zone.BindVisuals(fill.transform, marker, padRenderer, visualRoot);

            var prefab = AssetWriter.WritePrefab(root, "zone_pad");
            assets.SetPrefab("zone_pad", prefab);
        }

        // ------------------------------------------------------------ customers

        static void BuildCustomers(GameConfig config, GameAssets assets)
        {
            int layer = LayerMask.NameToLayer(Layers.StoreName);
            if (layer < 0) layer = 0;

            var root = NewRoot("customer", layer);
            var visual = AddVisual(root, assets, "mesh_customer_a");
            var stack = AddStack(root, assets, "Carry", new Vector3(0f, 1.42f, 0f),
                "mesh_product_box", 6, new Vector3(0.3f, 0.26f, 0.3f), 2, 1, 0.7f);

            var agent = root.AddComponent<CustomerAgent>();
            agent.BindVisuals(visual.transform, stack);
            agent.Configure(config, null);

            var variants = root.AddComponent<CustomerVariant>();
            var serialized = new SerializedObject(variants);
            serialized.FindProperty("m_Filter").objectReferenceValue = visual.GetComponent<MeshFilter>();
            var meshArray = serialized.FindProperty("m_Meshes");
            meshArray.arraySize = 3;
            meshArray.GetArrayElementAtIndex(0).objectReferenceValue = assets.GetMesh("mesh_customer_a");
            meshArray.GetArrayElementAtIndex(1).objectReferenceValue = assets.GetMesh("mesh_customer_b");
            meshArray.GetArrayElementAtIndex(2).objectReferenceValue = assets.GetMesh("mesh_customer_c");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = AssetWriter.WritePrefab(root, "customer");
            assets.SetPrefab("customer", prefab);
        }
    }
}
