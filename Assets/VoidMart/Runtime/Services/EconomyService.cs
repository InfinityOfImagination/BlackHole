using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Services
{
    /// <summary>
    /// Wallet, XP curve, upgrade levels and offline profit.  Everything that spends or earns goes
    /// through here so there is exactly one place that raises the currency events the HUD listens to.
    /// </summary>
    public class EconomyService : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;

        SaveService m_Save;
        readonly Dictionary<UpgradeEffect, float> m_EffectCache = new Dictionary<UpgradeEffect, float>(16);
        bool m_CacheValid;

        public GameConfig Config => m_Config;
        public SaveData Data => m_Save != null ? m_Save.Data : null;
        public double Cash => Data?.profile.softCurrency ?? 0d;
        public int Gems => Data?.profile.hardCurrency ?? 0;
        public int PlayerLevel => Data?.progress.playerLevel ?? 1;
        public float Xp => Data?.progress.xp ?? 0f;
        public float XpForNextLevel => m_Config != null ? m_Config.economy.XpForLevel(PlayerLevel) : 100f;
        public float XpNormalized => Mathf.Clamp01(XpForNextLevel <= 0f ? 0f : Xp / XpForNextLevel);

        public void Configure(GameConfig config) => m_Config = config;

        void Awake() => ServiceLocator.Register(this);

        void OnDestroy()
        {
            if (ServiceLocator.Get<EconomyService>() == this) ServiceLocator.Unregister<EconomyService>();
        }

        void Start()
        {
            m_Save = ServiceLocator.Get<SaveService>();
            var bus = Bus;
            if (bus != null && bus.dataReloaded != null) bus.dataReloaded.Register(OnDataReloaded);
            BroadcastAll();
        }

        void OnDisable()
        {
            var bus = Bus;
            if (bus != null && bus.dataReloaded != null) bus.dataReloaded.Unregister(OnDataReloaded);
        }

        GameEventBus Bus => m_Config != null ? m_Config.events : null;

        void OnDataReloaded()
        {
            m_CacheValid = false;
            BroadcastAll();
        }

        public void BroadcastAll()
        {
            var bus = Bus;
            if (bus == null || Data == null) return;
            bus.cashChanged?.Raise(Cash);
            bus.gemsChanged?.Raise(Gems);
            bus.xpChanged?.Raise(XpNormalized);
        }

        // ---------------------------------------------------------------- money

        public void AddCash(double amount)
        {
            if (Data == null || amount == 0d) return;
            if (amount > 0d) Data.profile.lifetimeEarnings += amount;
            Data.profile.softCurrency = Math.Max(0d, Data.profile.softCurrency + amount);
            m_Save?.MarkDirty();
            Bus?.cashChanged?.Raise(Data.profile.softCurrency);
        }

        public bool CanAfford(double amount) => Data != null && Data.profile.softCurrency >= amount - 0.0001d;

        public bool TrySpend(double amount)
        {
            if (Data == null || amount < 0d) return false;
            if (Data.profile.softCurrency < amount - 0.0001d) return false;
            Data.profile.softCurrency -= amount;
            m_Save?.MarkDirty();
            Bus?.cashChanged?.Raise(Data.profile.softCurrency);
            return true;
        }

        /// <summary>Partial spend used by the walk-in purchase pads (drains a little per tick).</summary>
        public double SpendUpTo(double requested)
        {
            if (Data == null || requested <= 0d) return 0d;
            double spent = Math.Min(requested, Data.profile.softCurrency);
            if (spent <= 0d) return 0d;
            Data.profile.softCurrency -= spent;
            m_Save?.MarkDirty();
            Bus?.cashChanged?.Raise(Data.profile.softCurrency);
            return spent;
        }

        public void AddGems(int amount)
        {
            if (Data == null || amount == 0) return;
            Data.profile.hardCurrency = Mathf.Max(0, Data.profile.hardCurrency + amount);
            m_Save?.MarkDirty();
            Bus?.gemsChanged?.Raise(Data.profile.hardCurrency);
        }

        public bool TrySpendGems(int amount)
        {
            if (Data == null || Data.profile.hardCurrency < amount) return false;
            Data.profile.hardCurrency -= amount;
            m_Save?.MarkDirty();
            Bus?.gemsChanged?.Raise(Data.profile.hardCurrency);
            return true;
        }

        // ------------------------------------------------------------------- xp

        public void AddXp(float amount)
        {
            if (Data == null || amount <= 0f || m_Config == null) return;
            Data.progress.xp += amount;
            int guard = 0;
            while (Data.progress.xp >= m_Config.economy.XpForLevel(Data.progress.playerLevel) && guard++ < 64)
            {
                Data.progress.xp -= m_Config.economy.XpForLevel(Data.progress.playerLevel);
                Data.progress.playerLevel++;
                Bus?.playerLevelUp?.Raise(Data.progress.playerLevel);
            }
            m_Save?.MarkDirty();
            Bus?.xpChanged?.Raise(XpNormalized);
        }

        // ------------------------------------------------------------- upgrades

        public int GetUpgradeLevel(string id)
        {
            if (Data == null || string.IsNullOrEmpty(id)) return 0;
            return Data.progress.upgradeLevels.TryGetValue(id, out int level) ? level : 0;
        }

        public double GetUpgradeCost(UpgradeDefinition upgrade)
        {
            if (upgrade == null || m_Config == null) return double.MaxValue;
            return upgrade.CostAt(m_Config.economy, GetUpgradeLevel(upgrade.id));
        }

        public bool IsUpgradeMaxed(UpgradeDefinition upgrade) => upgrade != null && GetUpgradeLevel(upgrade.id) >= upgrade.maxLevel;

        public bool TryBuyUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null || Data == null || IsUpgradeMaxed(upgrade)) return false;
            double cost = GetUpgradeCost(upgrade);
            if (!TrySpend(cost)) return false;

            int level = GetUpgradeLevel(upgrade.id) + 1;
            Data.progress.upgradeLevels[upgrade.id] = level;
            m_CacheValid = false;
            m_Save?.MarkDirty();
            m_Save?.SaveNow();
            Bus?.toast?.Raise(new ToastRequest(upgrade.displayName + " LV " + level, ToastStyle.Success));
            return true;
        }

        void RebuildEffectCache()
        {
            m_EffectCache.Clear();
            if (m_Config != null && Data != null)
            {
                for (int i = 0; i < m_Config.upgrades.Count; i++)
                {
                    var upgrade = m_Config.upgrades[i];
                    if (upgrade == null) continue;
                    int level = GetUpgradeLevel(upgrade.id);
                    if (level <= 0) continue;
                    m_EffectCache.TryGetValue(upgrade.effect, out float total);
                    m_EffectCache[upgrade.effect] = total + upgrade.ValueAt(level);
                }
            }
            m_CacheValid = true;
        }

        /// <summary>Raw additive total of every upgrade touching an effect.</summary>
        public float GetAdditive(UpgradeEffect effect)
        {
            if (!m_CacheValid) RebuildEffectCache();
            return m_EffectCache.TryGetValue(effect, out float value) ? value : 0f;
        }

        /// <summary>1.0 + additive total, i.e. the multiplier gameplay code applies.</summary>
        public float GetMultiplier(UpgradeEffect effect) => 1f + GetAdditive(effect);

        public void InvalidateUpgradeCache() => m_CacheValid = false;

        // --------------------------------------------------------------- offline

        /// <summary>
        /// Deterministic estimate of storefront income per second from the unlocked crafters,
        /// used for the offline payout so nothing extra has to be persisted.
        /// </summary>
        public double EstimateIncomePerSecond()
        {
            if (m_Config == null || Data == null) return 0d;
            double total = 0d;
            var unlocked = Data.store.unlockedNodeIds;
            for (int i = 0; i < m_Config.furnishingNodes.Count; i++)
            {
                var node = m_Config.furnishingNodes[i];
                if (node == null || node.Category != FurnitureType.Crafter) continue;
                if (!unlocked.Contains(node.NodeID)) continue;

                var product = m_Config.FindProduct(node.productId);
                if (product == null) continue;
                double perSecond = node.rateMultiplier * GetMultiplier(UpgradeEffect.MachineSpeed) / Math.Max(0.05f, product.craftSeconds);
                total += perSecond * product.basePrice * m_Config.economy.sellPriceMultiplier * GetMultiplier(UpgradeEffect.SellPrice);
            }
            return total;
        }

        public double ComputeOfflineEarnings(double seconds)
        {
            if (m_Config == null) return 0d;
            var economy = m_Config.economy;
            if (seconds < economy.offlineMinimumSeconds) return 0d;
            double capped = Math.Min(seconds, economy.offlineCapSeconds);
            double rate = economy.offlineEarningRate * GetMultiplier(UpgradeEffect.OfflineRate);
            return EstimateIncomePerSecond() * capped * rate;
        }

        // -------------------------------------------------------------- selling

        public double PriceFor(ProductDefinition product)
        {
            if (product == null || m_Config == null) return 0d;
            return product.basePrice * m_Config.economy.sellPriceMultiplier * GetMultiplier(UpgradeEffect.SellPrice);
        }

        public double ValueForMass(float mass, int tier)
        {
            if (m_Config == null) return mass;
            var economy = m_Config.economy;
            return mass * economy.valuePerMass * Math.Pow(economy.tierValueMultiplier, Math.Max(0, tier - 1));
        }
    }
}
