using System;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Gameplay;
using VoidMart.Services;

namespace VoidMart.Store
{
    /// <summary>
    /// A purchase pad (spec section 5).  Standing on an unbought zone drains the wallet at an
    /// exponential tick rate, bills arc out of the player onto the pad, and on completion the pad
    /// retires with a particle poof while the new structure snaps in on a 1.0 → 1.25 → 1.0 bounce.
    /// </summary>
    public class FurnishingZone : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] FurnishingNodeData m_Node;
        [SerializeField] Transform m_FillVisual;
        [SerializeField] Transform m_Marker;
        [SerializeField] Renderer m_PadRenderer;

        StoreManager m_Store;
        EconomyService m_Economy;
        AudioService m_Audio;
        PlayerHoleController m_Hole;

        double m_Paid;
        double m_Cost = 1d;
        float m_Elapsed;
        float m_TickTimer;
        float m_BillTimer;
        bool m_Completed;
        bool m_Visible = true;

        public event Action<FurnishingZone, FurnishingNodeData> Purchased;

        public FurnishingNodeData Node => m_Node;
        public double Paid => m_Paid;
        public double Cost => m_Cost;
        public float Progress => m_Cost <= 0d ? 1f : Mathf.Clamp01((float)(m_Paid / m_Cost));
        public bool Completed => m_Completed;

        public void Configure(GameConfig config, FurnishingNodeData node)
        {
            m_Config = config;
            m_Node = node;
        }

        public void BindVisuals(Transform fillVisual, Transform marker, Renderer padRenderer)
        {
            m_FillVisual = fillVisual;
            m_Marker = marker;
            m_PadRenderer = padRenderer;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Store = ServiceLocator.Get<StoreManager>();
            m_Economy = ServiceLocator.Get<EconomyService>();
            m_Audio = ServiceLocator.Get<AudioService>();
            m_Hole = ServiceLocator.Get<PlayerHoleController>();

            m_Cost = m_Store != null ? m_Store.CostOf(m_Node) : (m_Node?.BaseCost ?? 1d);
            RestoreProgress();
            RefreshVisibility();
            RefreshFill();
        }

        void RestoreProgress()
        {
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data == null || m_Node == null) return;
            if (save.Data.progress.nodeProgress.TryGetValue(m_Node.NodeID, out double paid))
                m_Paid = Math.Min(paid, m_Cost);
        }

        void PersistProgress()
        {
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data == null || m_Node == null) return;
            save.Data.progress.nodeProgress[m_Node.NodeID] = m_Paid;
            save.MarkDirty();
        }

        void Update()
        {
            if (m_Completed || m_Config == null || m_Node == null) return;

            RefreshVisibility();
            if (!m_Visible) return;

            if (m_Hole == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }

            Vector3 offset = m_Hole.transform.position - transform.position;
            offset.y = 0f;
            float radius = Mathf.Max(m_Node.zoneSize.x, m_Node.zoneSize.y) * 0.5f + m_Hole.Radius * 0.5f;
            bool inside = offset.sqrMagnitude <= radius * radius;

            if (!inside)
            {
                m_Elapsed = 0f;
                m_TickTimer = 0f;
                return;
            }

            Drain(Time.deltaTime);
        }

        void Drain(float deltaTime)
        {
            var store = m_Config.store;
            m_Elapsed += deltaTime;
            m_TickTimer += deltaTime;
            if (m_TickTimer < store.purchaseTickInterval) return;

            float tick = m_TickTimer;
            m_TickTimer = 0f;

            // Exponential ramp: the longer you stand on the pad, the faster the money flows.
            double baseRate = m_Cost / Mathf.Max(0.2f, store.purchaseMinSeconds);
            double ramp = 0.35d + Math.Pow(m_Elapsed / Mathf.Max(0.2f, store.purchaseMinSeconds), store.purchaseDrainExponent);
            double requested = baseRate * ramp * tick;
            double remaining = m_Cost - m_Paid;
            if (requested > remaining) requested = remaining;
            if (requested <= 0d) return;

            double spent = m_Economy != null ? m_Economy.SpendUpTo(requested) : 0d;
            if (spent <= 0d) return;

            m_Paid += spent;
            PersistProgress();
            RefreshFill();
            SpawnBill();

            m_Audio?.PlaySfx(AudioService.SfxCash, 1f + Progress * 0.4f, 0.35f, 4);

            if (m_Paid >= m_Cost - 0.001d) Complete();
        }

        void SpawnBill()
        {
            m_BillTimer -= Time.deltaTime;
            if (m_BillTimer > 0f) return;
            m_BillTimer = 0.07f;

            var pools = ServiceLocator.Get<PoolService>();
            var go = pools?.Spawn("fx_cash_bill", m_Hole.transform.position + Vector3.up * 0.35f, UnityEngine.Random.rotation);
            var item = go != null ? go.GetComponent<FlyingItem>() : null;
            item?.Launch(m_Hole.transform.position + Vector3.up * 0.35f, transform.position + Vector3.up * 0.15f,
                m_Config.store.billArcHeight * 0.7f, m_Config.store.billFlightSeconds * 0.8f, null, 1.6f);
        }

        void RefreshFill()
        {
            if (m_FillVisual == null) return;
            float progress = Progress;
            var scale = m_FillVisual.localScale;
            m_FillVisual.localScale = new Vector3(progress, scale.y, 1f);
        }

        void RefreshVisibility()
        {
            bool shouldShow = m_Store == null || m_Store.DependenciesMet(m_Node);
            if (shouldShow == m_Visible) return;
            m_Visible = shouldShow;
            if (m_Marker != null) m_Marker.gameObject.SetActive(shouldShow);
            if (m_PadRenderer != null) m_PadRenderer.enabled = shouldShow;
        }

        void Complete()
        {
            if (m_Completed) return;
            m_Completed = true;
            m_Paid = m_Cost;

            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data != null && m_Node != null) save.Data.progress.nodeProgress.Remove(m_Node.NodeID);

            m_Store?.MarkUnlocked(m_Node.NodeID);
            m_Audio?.PlaySfx(AudioService.SfxUpgrade, 1f, 1f);
            Haptics.Play(HapticStrength.Heavy);

            var pools = ServiceLocator.Get<PoolService>();
            pools?.Spawn("fx_poof", transform.position + Vector3.up * 0.4f, Quaternion.identity);

            m_Config.events?.toast?.Raise(new ToastRequest(m_Node.displayName + " unlocked!", ToastStyle.Success));
            ServiceLocator.Get<CameraRig>()?.Shake(0.35f);

            Purchased?.Invoke(this, m_Node);
            gameObject.SetActive(false);
        }

        /// <summary>Used by the Master Tool / debug to unlock without paying.</summary>
        public void ForceComplete() => Complete();
    }
}
