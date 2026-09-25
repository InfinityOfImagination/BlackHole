using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Store
{
    /// <summary>Display shelf that holds finished goods until a customer picks them up.</summary>
    public class ProductShelf : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] string m_ProductId = "";
        [SerializeField] float m_CapacityMultiplier = 1f;
        [SerializeField] Transform m_DropPoint;
        [SerializeField] Transform m_PickPoint;
        [SerializeField] StackVisual m_Stack;

        StoreManager m_Store;
        int m_Stock;
        int m_Incoming;

        public string ProductId => m_ProductId;
        public int Stock => m_Stock;
        public int Capacity => Mathf.Max(1, Mathf.RoundToInt((m_Config != null ? m_Config.store.shelfCapacity : 18f) * m_CapacityMultiplier));
        public bool IsFull => m_Stock + m_Incoming >= Capacity;
        public float FillNormalized => Capacity <= 0 ? 1f : (float)(m_Stock + m_Incoming) / Capacity;
        public Transform DropPoint => m_DropPoint != null ? m_DropPoint : transform;
        public Transform PickPoint => m_PickPoint != null ? m_PickPoint : DropPoint;

        public void Configure(GameConfig config, string productId, float capacityMultiplier)
        {
            m_Config = config;
            m_ProductId = productId;
            m_CapacityMultiplier = Mathf.Max(0.1f, capacityMultiplier);
        }

        public void BindVisuals(Transform dropPoint, Transform pickPoint, StackVisual stack)
        {
            m_DropPoint = dropPoint;
            m_PickPoint = pickPoint;
            m_Stack = stack;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Store = ServiceLocator.Get<StoreManager>();
            m_Store?.RegisterShelf(this);
            RefreshVisual();
        }

        void OnDestroy() => m_Store?.UnregisterShelf(this);

        public bool Accepts(string productId) =>
            string.IsNullOrEmpty(m_ProductId) || m_ProductId == productId;

        public void ReserveIncoming() => m_Incoming++;

        public void DeliverIncoming(string productId)
        {
            m_Incoming = Mathf.Max(0, m_Incoming - 1);
            if (string.IsNullOrEmpty(m_ProductId)) m_ProductId = productId;
            m_Stock = Mathf.Min(Capacity, m_Stock + 1);
            RefreshVisual();
        }

        /// <summary>Customer pickup. Returns how many units were actually taken.</summary>
        public int Take(int requested)
        {
            int taken = Mathf.Clamp(requested, 0, m_Stock);
            m_Stock -= taken;
            RefreshVisual();
            return taken;
        }

        public ProductDefinition Definition => m_Config != null ? m_Config.FindProduct(m_ProductId) : null;

        void RefreshVisual()
        {
            if (m_Stack == null) return;
            float ratio = Capacity <= 0 ? 0f : (float)m_Stock / Capacity;
            m_Stack.SetCount(Mathf.RoundToInt(ratio * m_Stack.MaxVisible));
        }
    }
}
