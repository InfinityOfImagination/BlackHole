using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Data
{
    public enum FurnitureType { Crafter, Register, Decorative, Hopper, Shelf, Conveyor, Expansion, Robot }

    /// <summary>
    /// Structural data model for expansion nodes, matching section 5 of the tech spec.
    /// Spec fields keep their published names; everything after them is placement / behaviour
    /// metadata the builder needs to lay the store out procedurally.
    /// </summary>
    [CreateAssetMenu(fileName = "FurnishingNode", menuName = "VoidMart/FurnishingNode")]
    public class FurnishingNodeData : ScriptableObject
    {
        public string NodeID;
        public int TierLevel;
        public double BaseCost;

        public GameObject PrefabToSpawn;

        // Must unlock prerequisites before this node appears
        public List<string> DependentNodeIDs = new List<string>();

        public FurnitureType Category; // Crafter, Register, Decorative

        [Header("Presentation")]
        public string displayName = "Node";
        [TextArea(1, 3)] public string description = "";
        public string prefabKey = "";
        public string iconKey = "icon_box";
        public Color accent = Color.white;

        [Header("Placement (store-local space)")]
        public Vector3 localPosition;
        public float yaw;
        public Vector2 zoneSize = new Vector2(3.2f, 3.2f);

        [Header("Behaviour")]
        [Tooltip("Crafter: which product this machine outputs.")]
        public string productId = "";
        [Tooltip("Crafter / Register throughput multiplier.")]
        public float rateMultiplier = 1f;
        [Tooltip("Shelf / hopper capacity multiplier.")]
        public float capacityMultiplier = 1f;
        public bool unlockedFromStart;

        public bool DependenciesMet(ICollection<string> unlockedIds)
        {
            if (DependentNodeIDs == null) return true;
            for (int i = 0; i < DependentNodeIDs.Count; i++)
            {
                string dependency = DependentNodeIDs[i];
                if (string.IsNullOrEmpty(dependency)) continue;
                if (!unlockedIds.Contains(dependency)) return false;
            }
            return true;
        }
    }
}
