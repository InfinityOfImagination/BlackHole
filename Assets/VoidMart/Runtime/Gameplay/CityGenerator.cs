using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.Gameplay
{
    /// <summary>
    /// Builds the street district the player farms: a flat, texture-painted block grid populated
    /// with pooled props, parked vehicles, buildings and pedestrians.  Everything is deterministic
    /// from <see cref="CityConfig.randomSeed"/>, so a layout a designer likes can be reproduced.
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        class SpawnSlot
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public PropDefinition Definition;
            public float Scale;
            public GameObject Instance;
            public float RespawnTimer;
            public bool Retired;
        }

        [SerializeField] GameConfig m_Config;
        [SerializeField] Transform m_StaticRoot;
        [SerializeField] Transform m_PropRoot;

        readonly List<SpawnSlot> m_Slots = new List<SpawnSlot>(256);
        readonly List<PropDefinition> m_Weighted = new List<PropDefinition>(64);
        System.Random m_Random;
        PoolService m_Pools;
        EconomyService m_Economy;
        int m_ScanCursor;

        public Bounds DistrictBounds { get; private set; }
        public float Pitch => m_Config != null ? m_Config.city.blockSize + m_Config.city.roadWidth : 32f;

        public void Configure(GameConfig config) => m_Config = config;

        void Awake()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            if (m_StaticRoot == null) m_StaticRoot = CreateChild("City Static");
            if (m_PropRoot == null) m_PropRoot = CreateChild("City Props");
            ServiceLocator.Register(this);
        }

        void OnDestroy()
        {
            if (ServiceLocator.Get<CityGenerator>() == this) ServiceLocator.Unregister<CityGenerator>();
        }

        Transform CreateChild(string childName)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go.transform;
        }

        void Start()
        {
            m_Pools = ServiceLocator.Get<PoolService>();
            m_Economy = ServiceLocator.Get<EconomyService>();
            Build();
        }

        public void Build()
        {
            if (m_Config == null) return;
            var city = m_Config.city;
            m_Random = new System.Random(city.randomSeed);

            // The street texture tiles once per block pitch, so the ground has to be an exact
            // multiple of it or the road markings drift out of alignment.
            float extent = city.blocksX * Pitch;
            float extentZ = city.blocksZ * Pitch;
            DistrictBounds = new Bounds(Vector3.zero, new Vector3(extent, 1f, extentZ));

            BuildGround(extent, extentZ);
            RegisterPools();
            PlaceEverything(city);

            var hole = ServiceLocator.Get<PlayerHoleController>();
            if (hole != null)
            {
                var bounds = DistrictBounds;
                bounds.Encapsulate(new Vector3(0f, 0f, extentZ * 0.5f + 46f));
                bounds.Expand(new Vector3(-city.boundaryPadding, 0f, -city.boundaryPadding));
                hole.SetBounds(bounds);
            }
        }

        void BuildGround(float extentX, float extentZ)
        {
            var assets = m_Config.assets;
            if (assets == null) return;

            var ground = new GameObject("Street Ground");
            ground.transform.SetParent(m_StaticRoot, false);
            ground.layer = Layers.Ground;

            var filter = ground.AddComponent<MeshFilter>();
            filter.sharedMesh = assets.GetMesh("mesh_quad_xz");
            var renderer = ground.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = assets.GetMaterial("mat_street");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ground.transform.localScale = new Vector3(extentX, 1f, extentZ);

            var collider = ground.AddComponent<BoxCollider>();
            collider.size = new Vector3(1f, 0.02f, 1f);
            collider.center = new Vector3(0f, -0.01f, 0f);
        }

        void RegisterPools()
        {
            if (m_Pools == null || m_Config.assets == null) return;
            for (int i = 0; i < m_Config.props.Count; i++)
            {
                var definition = m_Config.props[i];
                if (definition == null) continue;
                string key = "prop_" + definition.id;
                var prefab = m_Config.assets.GetPrefab(key);
                if (prefab == null) continue;
                m_Pools.Register(key, prefab, EstimatePrewarm(definition));
            }
        }

        int EstimatePrewarm(PropDefinition definition)
        {
            switch (definition.category)
            {
                case PropCategory.Structure: return 6;
                case PropCategory.Vehicle: return 8;
                case PropCategory.Pedestrian: return 8;
                default: return 16;
            }
        }

        void PlaceEverything(CityConfig city)
        {
            ClearSlots();
            BuildWeightedTable();

            float half = city.blocksX * Pitch * 0.5f;
            float halfZ = city.blocksZ * Pitch * 0.5f;

            // Buildings sit on block interiors.
            int buildingsPlaced = 0;
            for (int ix = 0; ix < city.blocksX && buildingsPlaced < city.buildingBudget; ix++)
            {
                for (int iz = 0; iz < city.blocksZ && buildingsPlaced < city.buildingBudget; iz++)
                {
                    Vector3 centre = BlockCentre(ix, iz, half, halfZ);
                    int perBlock = 1 + m_Random.Next(0, 3);
                    for (int i = 0; i < perBlock && buildingsPlaced < city.buildingBudget; i++)
                    {
                        var definition = PickByCategory(PropCategory.Structure);
                        if (definition == null) break;
                        float spread = city.blockSize * 0.28f;
                        Vector3 position = centre + new Vector3(RandomRange(-spread, spread), 0f, RandomRange(-spread, spread));
                        AddSlot(definition, position, Quaternion.Euler(0f, m_Random.Next(0, 4) * 90f, 0f));
                        buildingsPlaced++;
                    }
                }
            }

            // Street furniture and litter cluster along the kerb lines.
            for (int i = 0; i < city.propBudget; i++)
            {
                var definition = PickWeighted();
                if (definition == null) break;
                Vector3 position = RandomKerbPoint(city, half, halfZ);
                AddSlot(definition, position, Quaternion.Euler(0f, RandomRange(0f, 360f), 0f));
            }

            // Vehicles park along the roads, aligned with traffic.
            for (int i = 0; i < city.vehicleBudget; i++)
            {
                var definition = PickByCategory(PropCategory.Vehicle);
                if (definition == null) break;
                bool horizontal = m_Random.Next(0, 2) == 0;
                Vector3 position = RandomRoadPoint(city, half, halfZ, horizontal);
                AddSlot(definition, position, Quaternion.Euler(0f, horizontal ? 90f : 0f, 0f));
            }

            // Pedestrians wander the pavements.
            for (int i = 0; i < city.pedestrianBudget; i++)
            {
                var definition = PickByCategory(PropCategory.Pedestrian);
                if (definition == null) break;
                Vector3 position = RandomKerbPoint(city, half, halfZ);
                AddSlot(definition, position, Quaternion.Euler(0f, RandomRange(0f, 360f), 0f));
            }

            for (int i = 0; i < m_Slots.Count; i++) SpawnSlotInstance(m_Slots[i]);
        }

        Vector3 BlockCentre(int ix, int iz, float half, float halfZ)
        {
            // Inside each tile the road occupies [0, roadWidth] and the block the remainder,
            // matching how the street texture is painted.
            var city = m_Config.city;
            float x = -half + city.roadWidth + city.blockSize * 0.5f + ix * Pitch;
            float z = -halfZ + city.roadWidth + city.blockSize * 0.5f + iz * Pitch;
            return new Vector3(x, 0f, z);
        }

        Vector3 RandomKerbPoint(CityConfig city, float half, float halfZ)
        {
            int ix = m_Random.Next(0, Mathf.Max(1, city.blocksX));
            int iz = m_Random.Next(0, Mathf.Max(1, city.blocksZ));
            Vector3 centre = BlockCentre(ix, iz, half, halfZ);
            float edge = city.blockSize * 0.5f;
            float inset = RandomRange(0.4f, 2.6f);
            switch (m_Random.Next(0, 4))
            {
                case 0: return centre + new Vector3(RandomRange(-edge, edge), 0f, edge - inset);
                case 1: return centre + new Vector3(RandomRange(-edge, edge), 0f, -edge + inset);
                case 2: return centre + new Vector3(edge - inset, 0f, RandomRange(-edge, edge));
                default: return centre + new Vector3(-edge + inset, 0f, RandomRange(-edge, edge));
            }
        }

        Vector3 RandomRoadPoint(CityConfig city, float half, float halfZ, bool horizontal)
        {
            int lane = m_Random.Next(0, Mathf.Max(1, (horizontal ? city.blocksZ : city.blocksX) + 1));
            float laneCentre = -(horizontal ? halfZ : half) + city.roadWidth * 0.5f + lane * Pitch;
            float offset = RandomRange(-city.roadWidth * 0.22f, city.roadWidth * 0.22f);
            float along = RandomRange(-(horizontal ? half : halfZ) + 4f, (horizontal ? half : halfZ) - 4f);
            return horizontal
                ? new Vector3(along, 0f, laneCentre + offset)
                : new Vector3(laneCentre + offset, 0f, along);
        }

        void BuildWeightedTable()
        {
            m_Weighted.Clear();
            for (int i = 0; i < m_Config.props.Count; i++)
            {
                var definition = m_Config.props[i];
                if (definition == null || definition.category == PropCategory.Structure ||
                    definition.category == PropCategory.Vehicle || definition.category == PropCategory.Pedestrian) continue;
                int weight = Mathf.Max(1, Mathf.RoundToInt(definition.spawnWeight * 4f));
                for (int w = 0; w < weight; w++) m_Weighted.Add(definition);
            }
        }

        PropDefinition PickWeighted() => m_Weighted.Count == 0 ? null : m_Weighted[m_Random.Next(0, m_Weighted.Count)];

        PropDefinition PickByCategory(PropCategory category)
        {
            var candidates = ListPool<PropDefinition>.Get();
            for (int i = 0; i < m_Config.props.Count; i++)
                if (m_Config.props[i] != null && m_Config.props[i].category == category) candidates.Add(m_Config.props[i]);
            PropDefinition result = candidates.Count == 0 ? null : candidates[m_Random.Next(0, candidates.Count)];
            ListPool<PropDefinition>.Release(candidates);
            return result;
        }

        void AddSlot(PropDefinition definition, Vector3 position, Quaternion rotation)
        {
            float scale = RandomRange(definition.scaleRange.x, definition.scaleRange.y);
            m_Slots.Add(new SpawnSlot
            {
                Definition = definition,
                Position = position,
                Rotation = rotation,
                Scale = scale
            });
        }

        void SpawnSlotInstance(SpawnSlot slot)
        {
            if (m_Pools == null || slot.Definition == null) return;
            var instance = m_Pools.Spawn("prop_" + slot.Definition.id, slot.Position, slot.Rotation);
            if (instance == null) return;

            slot.Instance = instance;
            var swallowable = instance.GetComponent<Swallowable>();
            if (swallowable != null) swallowable.Configure(slot.Definition, slot.Scale, m_Economy);
        }

        void ClearSlots()
        {
            for (int i = 0; i < m_Slots.Count; i++)
            {
                if (m_Slots[i].Instance != null) m_Pools?.Despawn(m_Slots[i].Instance);
            }
            m_Slots.Clear();
        }

        void Update()
        {
            if (m_Config == null || m_Slots.Count == 0) return;
            var city = m_Config.city;
            float deltaTime = Time.deltaTime;

            // Stagger the scan so a big district never spikes a frame.
            int perFrame = Mathf.Clamp(m_Slots.Count / 20, 4, 48);
            for (int i = 0; i < perFrame; i++)
            {
                m_ScanCursor = (m_ScanCursor + 1) % m_Slots.Count;
                var slot = m_Slots[m_ScanCursor];
                if (slot.Retired) continue;

                bool gone = slot.Instance == null || !slot.Instance.activeInHierarchy;
                if (!gone) continue;

                if (slot.RespawnTimer <= 0f)
                {
                    slot.Instance = null;
                    if (RandomRange(0f, 1f) > city.respawnFraction)
                    {
                        slot.Retired = true;
                        continue;
                    }
                    slot.RespawnTimer = city.respawnSeconds * RandomRange(0.75f, 1.35f);
                }
            }

            for (int i = 0; i < m_Slots.Count; i++)
            {
                var slot = m_Slots[i];
                if (slot.RespawnTimer <= 0f) continue;
                slot.RespawnTimer -= deltaTime;
                if (slot.RespawnTimer > 0f) continue;
                slot.RespawnTimer = 0f;
                SpawnSlotInstance(slot);
            }
        }

        float RandomRange(float min, float max) => min + (float)m_Random.NextDouble() * (max - min);

        /// <summary>Re-rolls the whole district (used by the Master Tool's City tab).</summary>
        public void Rebuild(int seed)
        {
            if (m_Config != null) m_Config.city.randomSeed = seed;
            for (int i = m_StaticRoot.childCount - 1; i >= 0; i--) Destroy(m_StaticRoot.GetChild(i).gameObject);
            Build();
        }
    }
}
