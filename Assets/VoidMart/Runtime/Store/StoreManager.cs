using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.Store
{
    /// <summary>
    /// The storefront brain: owns the raw-material balance the hole delivers, tracks which
    /// furnishing nodes are unlocked, keeps machine state in sync with the save file and hands
    /// out the throughput multipliers every device reads.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class StoreManager : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;

        readonly List<CraftingMachine> m_Machines = new List<CraftingMachine>(8);
        readonly List<ProductShelf> m_Shelves = new List<ProductShelf>(8);
        readonly HashSet<string> m_Unlocked = new HashSet<string>();

        double m_RawMass;
        SaveService m_Save;
        EconomyService m_Economy;

        public GameConfig Config => m_Config;
        public double RawMass => m_RawMass;
        public IReadOnlyList<CraftingMachine> Machines => m_Machines;
        public IReadOnlyList<ProductShelf> Shelves => m_Shelves;
        public Checkout Register { get; set; }
        public Transform CustomerDoor { get; set; }
        public Transform CustomerExit { get; set; }

        GameEventBus Bus => m_Config != null ? m_Config.events : null;

        public void Configure(GameConfig config) => m_Config = config;

        void Awake()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            ServiceLocator.Register(this);
        }

        void OnDestroy()
        {
            if (ServiceLocator.Get<StoreManager>() == this) ServiceLocator.Unregister<StoreManager>();
        }

        void Start()
        {
            m_Save = ServiceLocator.Get<SaveService>();
            m_Economy = ServiceLocator.Get<EconomyService>();
            SyncUnlockedFromSave();
        }

        public void SyncUnlockedFromSave()
        {
            m_Unlocked.Clear();
            if (m_Save?.Data == null) return;
            var ids = m_Save.Data.store.unlockedNodeIds;
            for (int i = 0; i < ids.Count; i++)
                if (!string.IsNullOrEmpty(ids[i])) m_Unlocked.Add(ids[i]);
        }

        // ------------------------------------------------------------ materials

        public void AddRawMass(double amount)
        {
            if (amount <= 0d) return;
            m_RawMass += amount;
        }

        public bool TryConsumeRawMass(double amount)
        {
            if (m_RawMass < amount) return false;
            m_RawMass -= amount;
            return true;
        }

        public double ConsumeUpTo(double amount)
        {
            double taken = System.Math.Min(amount, m_RawMass);
            m_RawMass -= taken;
            return taken;
        }

        // ----------------------------------------------------------- registries

        public void RegisterMachine(CraftingMachine machine)
        {
            if (machine != null && !m_Machines.Contains(machine)) m_Machines.Add(machine);
        }

        public void UnregisterMachine(CraftingMachine machine) => m_Machines.Remove(machine);

        public void RegisterShelf(ProductShelf shelf)
        {
            if (shelf != null && !m_Shelves.Contains(shelf)) m_Shelves.Add(shelf);
        }

        public void UnregisterShelf(ProductShelf shelf) => m_Shelves.Remove(shelf);

        public ProductShelf FindShelfWithStock()
        {
            for (int i = 0; i < m_Shelves.Count; i++)
                if (m_Shelves[i] != null && m_Shelves[i].Stock > 0) return m_Shelves[i];
            return null;
        }

        public ProductShelf FindShelfWithSpace(string productId)
        {
            ProductShelf best = null;
            float bestFill = 1f;
            for (int i = 0; i < m_Shelves.Count; i++)
            {
                var shelf = m_Shelves[i];
                if (shelf == null || !shelf.Accepts(productId) || shelf.IsFull) continue;
                float fill = shelf.FillNormalized;
                if (fill < bestFill) { bestFill = fill; best = shelf; }
            }
            return best;
        }

        // -------------------------------------------------------------- unlocks

        public bool IsUnlocked(string nodeId) => !string.IsNullOrEmpty(nodeId) && m_Unlocked.Contains(nodeId);

        public bool DependenciesMet(FurnishingNodeData node) => node != null && node.DependenciesMet(m_Unlocked);

        public double CostOf(FurnishingNodeData node)
        {
            if (node == null || m_Config == null) return double.MaxValue;
            return m_Config.economy.NodeCost(node.BaseCost, node.TierLevel);
        }

        public void MarkUnlocked(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId) || m_Unlocked.Contains(nodeId)) return;
            m_Unlocked.Add(nodeId);
            if (m_Save?.Data != null)
            {
                if (!m_Save.Data.store.unlockedNodeIds.Contains(nodeId))
                    m_Save.Data.store.unlockedNodeIds.Add(nodeId);
                m_Save.MarkDirty();
                m_Save.SaveNow();
            }
            Bus?.nodePurchased?.Raise(nodeId);
        }

        // --------------------------------------------------------- machine state

        public MachineSave GetMachineState(string machineId)
        {
            if (m_Save?.Data == null || string.IsNullOrEmpty(machineId)) return null;
            if (!m_Save.Data.store.machineStates.TryGetValue(machineId, out var state))
            {
                state = new MachineSave { tier = 1 };
                m_Save.Data.store.machineStates[machineId] = state;
            }
            return state;
        }

        public void PersistMachineState() => m_Save?.MarkDirty();

        // ------------------------------------------------------------ modifiers

        public float MachineSpeedMultiplier => m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.MachineSpeed) : 1f;
        public float SellPriceMultiplier => m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.SellPrice) : 1f;
        public float CustomerRateMultiplier => m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.CustomerRate) : 1f;
        public float JamResistance => m_Economy != null ? m_Economy.GetAdditive(UpgradeEffect.JamResistance) : 0f;
        public float UnloadMultiplier => m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.UnloadSpeed) : 1f;
        public int RobotBudget
        {
            get
            {
                int fromUpgrades = m_Economy != null ? Mathf.FloorToInt(m_Economy.GetAdditive(UpgradeEffect.RobotCount)) : 0;
                int hired = m_Save?.Data != null ? m_Save.Data.progress.robotsHired : 0;
                int cap = m_Config != null ? m_Config.store.maxRobots : 4;
                return Mathf.Clamp(fromUpgrades + hired, 0, cap);
            }
        }
    }
}
