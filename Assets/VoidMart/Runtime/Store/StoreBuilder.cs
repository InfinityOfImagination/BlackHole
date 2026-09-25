using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.UI;

namespace VoidMart.Store
{
    /// <summary>
    /// Turns the <see cref="FurnishingNodeData"/> table into a living store: unlocked nodes spawn
    /// as working machines, locked ones spawn as purchase pads, and dependencies decide what is
    /// even visible.  Re-orders itself whenever a node is bought, with the spec's snap-in bounce.
    /// </summary>
    public class StoreBuilder : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] Transform m_NodeRoot;
        [SerializeField] Transform m_CustomerDoor;
        [SerializeField] Transform m_CustomerExit;
        [SerializeField] Transform m_RobotBank;

        readonly Dictionary<string, GameObject> m_Structures = new Dictionary<string, GameObject>(16);
        readonly Dictionary<string, FurnishingZone> m_Pads = new Dictionary<string, FurnishingZone>(16);
        readonly List<RobotHelper> m_Robots = new List<RobotHelper>(4);

        StoreManager m_Store;
        float m_RobotCheckTimer;

        public Transform NodeRoot => m_NodeRoot != null ? m_NodeRoot : transform;

        public void Configure(GameConfig config, Transform nodeRoot, Transform door, Transform exit, Transform bank)
        {
            m_Config = config;
            m_NodeRoot = nodeRoot;
            m_CustomerDoor = door;
            m_CustomerExit = exit;
            m_RobotBank = bank;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Store = ServiceLocator.Get<StoreManager>();
            if (m_Store != null)
            {
                m_Store.CustomerDoor = m_CustomerDoor;
                m_Store.CustomerExit = m_CustomerExit != null ? m_CustomerExit : m_CustomerDoor;
            }
            BuildAll();
        }

        public void BuildAll()
        {
            if (m_Config == null) return;
            ClearAll();

            for (int i = 0; i < m_Config.furnishingNodes.Count; i++)
            {
                var node = m_Config.furnishingNodes[i];
                if (node == null) continue;

                bool unlocked = node.unlockedFromStart || (m_Store != null && m_Store.IsUnlocked(node.NodeID));
                if (unlocked)
                {
                    if (node.unlockedFromStart) m_Store?.MarkUnlocked(node.NodeID);
                    SpawnStructure(node, false);
                }
                else
                {
                    SpawnPad(node);
                }
            }
        }

        void ClearAll()
        {
            foreach (var kv in m_Structures) if (kv.Value != null) Destroy(kv.Value);
            foreach (var kv in m_Pads) if (kv.Value != null) Destroy(kv.Value.gameObject);
            m_Structures.Clear();
            m_Pads.Clear();
        }

        // ----------------------------------------------------------------- pads

        void SpawnPad(FurnishingNodeData node)
        {
            var assets = m_Config.assets;
            var prefab = assets != null ? assets.GetPrefab("zone_pad") : null;
            if (prefab == null) return;

            var instance = Instantiate(prefab, NodeRoot);
            instance.name = "Pad_" + node.NodeID;
            instance.transform.localPosition = node.localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, node.yaw, 0f);
            instance.transform.localScale = new Vector3(node.zoneSize.x, 1f, node.zoneSize.y);

            var zone = instance.GetComponent<FurnishingZone>();
            if (zone == null) zone = instance.AddComponent<FurnishingZone>();
            zone.Configure(m_Config, node);
            zone.Purchased += OnZonePurchased;
            m_Pads[node.NodeID] = zone;

            var label = instance.GetComponentInChildren<ZoneLabel>();
            label?.Bind(m_Config, node, zone);
        }

        void OnZonePurchased(FurnishingZone zone, FurnishingNodeData node)
        {
            zone.Purchased -= OnZonePurchased;
            m_Pads.Remove(node.NodeID);
            Destroy(zone.gameObject);
            SpawnStructure(node, true);

            // Newly met dependencies may reveal further pads.
            for (int i = 0; i < m_Config.furnishingNodes.Count; i++)
            {
                var other = m_Config.furnishingNodes[i];
                if (other == null || m_Structures.ContainsKey(other.NodeID) || m_Pads.ContainsKey(other.NodeID)) continue;
                if (m_Store != null && m_Store.IsUnlocked(other.NodeID)) continue;
                SpawnPad(other);
            }
        }

        // ----------------------------------------------------------- structures

        public GameObject SpawnStructure(FurnishingNodeData node, bool animate)
        {
            if (m_Structures.ContainsKey(node.NodeID)) return m_Structures[node.NodeID];

            var assets = m_Config.assets;
            GameObject prefab = node.PrefabToSpawn;
            if (prefab == null && assets != null) prefab = assets.GetPrefab(node.prefabKey);
            if (prefab == null) return null;

            var instance = Instantiate(prefab, NodeRoot);
            instance.name = "Node_" + node.NodeID;
            instance.transform.localPosition = node.localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, node.yaw, 0f);
            m_Structures[node.NodeID] = instance;

            ApplyNodeData(node, instance);
            if (animate) StartCoroutine(SnapIn(instance.transform));
            return instance;
        }

        void ApplyNodeData(FurnishingNodeData node, GameObject instance)
        {
            switch (node.Category)
            {
                case FurnitureType.Crafter:
                {
                    var machine = instance.GetComponent<CraftingMachine>();
                    machine?.Configure(m_Config, node.NodeID, node.productId, node.rateMultiplier);
                    break;
                }
                case FurnitureType.Shelf:
                {
                    var shelf = instance.GetComponent<ProductShelf>();
                    shelf?.Configure(m_Config, node.productId, node.capacityMultiplier);
                    break;
                }
                case FurnitureType.Register:
                {
                    var checkout = instance.GetComponent<Checkout>();
                    if (checkout != null)
                    {
                        checkout.Configure(m_Config);
                        if (m_Store != null) m_Store.Register = checkout;
                    }
                    var spawner = instance.GetComponentInChildren<CustomerSpawner>();
                    spawner?.Configure(m_Config, m_CustomerDoor);
                    break;
                }
                case FurnitureType.Hopper:
                {
                    var hopper = instance.GetComponent<Hopper>();
                    if (hopper != null && m_Config != null) hopper.Configure(m_Config, null, null, Mathf.Max(node.zoneSize.x, node.zoneSize.y) * 0.5f + 1.2f);
                    break;
                }
                case FurnitureType.Robot:
                {
                    var robot = instance.GetComponent<RobotHelper>();
                    robot?.Configure(m_Config, m_RobotBank);
                    if (robot != null) m_Robots.Add(robot);
                    break;
                }
            }
        }

        /// <summary>Procedural scale bounce: 1.0 → 1.25 → 1.0.</summary>
        IEnumerator SnapIn(Transform target)
        {
            Vector3 baseScale = target.localScale;
            float duration = 0.42f;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / duration);
                float scale = t < 0.45f
                    ? Mathf.Lerp(0.2f, 1.25f, Easing.Evaluate(Ease.OutCubic, t / 0.45f))
                    : Mathf.Lerp(1.25f, 1f, Easing.Evaluate(Ease.OutBack, (t - 0.45f) / 0.55f));
                target.localScale = baseScale * scale;
                yield return null;
            }
            target.localScale = baseScale;
        }

        // --------------------------------------------------------------- robots

        void Update()
        {
            if (m_Config == null || m_Store == null) return;
            m_RobotCheckTimer -= Time.deltaTime;
            if (m_RobotCheckTimer > 0f) return;
            m_RobotCheckTimer = 1.5f;

            int budget = m_Store.RobotBudget;
            while (m_Robots.Count < budget)
            {
                var spawned = SpawnRobot();
                if (spawned == null) break;
                m_Robots.Add(spawned);
            }
            while (m_Robots.Count > budget && m_Robots.Count > 0)
            {
                var last = m_Robots[m_Robots.Count - 1];
                m_Robots.RemoveAt(m_Robots.Count - 1);
                if (last != null) Destroy(last.gameObject);
            }
        }

        RobotHelper SpawnRobot()
        {
            var assets = m_Config.assets;
            var prefab = assets != null ? assets.GetPrefab("store_robot") : null;
            if (prefab == null) return null;

            Vector3 anchor = m_RobotBank != null ? m_RobotBank.position : NodeRoot.position;
            var instance = Instantiate(prefab, anchor + Random.insideUnitSphere * 1.2f, Quaternion.identity, NodeRoot);
            instance.name = "Robot";
            var robot = instance.GetComponent<RobotHelper>();
            robot?.Configure(m_Config, m_RobotBank);
            return robot;
        }

        public FurnishingZone FindPad(string nodeId) => m_Pads.TryGetValue(nodeId, out var zone) ? zone : null;
        public GameObject FindStructure(string nodeId) => m_Structures.TryGetValue(nodeId, out var go) ? go : null;
    }
}
