using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Store
{
    /// <summary>Feeds the store a steady trickle of shoppers, scaled by upgrades and stock.</summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] Transform m_Door;

        readonly List<CustomerAgent> m_Active = new List<CustomerAgent>(16);
        StoreManager m_Store;
        float m_Timer;

        public int ActiveCount => m_Active.Count;

        public void Configure(GameConfig config, Transform door)
        {
            m_Config = config;
            m_Door = door;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Store = ServiceLocator.Get<StoreManager>();
            if (m_Door == null) m_Door = transform;
            if (m_Store != null && m_Store.CustomerDoor == null) m_Store.CustomerDoor = m_Door;
        }

        void Update()
        {
            if (m_Config == null || m_Store == null) return;
            if (m_Active.Count >= m_Config.store.maxCustomers) return;

            float rate = Mathf.Max(0.05f, m_Config.store.customerSpawnInterval / Mathf.Max(0.05f, m_Store.CustomerRateMultiplier));
            m_Timer += Time.deltaTime;
            if (m_Timer < rate) return;
            m_Timer = 0f;

            // Only bring shoppers in when there is something to buy.
            if (m_Store.FindShelfWithStock() == null && m_Active.Count > 2) return;
            Spawn();
        }

        void Spawn()
        {
            var pools = ServiceLocator.Get<PoolService>();
            if (pools == null) return;

            Vector3 position = m_Door.position + m_Door.right * Random.Range(-0.7f, 0.7f);
            var go = pools.Spawn("customer", position, Quaternion.LookRotation(m_Door.forward, Vector3.up));
            if (go == null) return;

            var agent = go.GetComponent<CustomerAgent>();
            if (agent == null) return;
            agent.Configure(m_Config, this);
            m_Active.Add(agent);
        }

        public void NotifyDespawn(CustomerAgent agent) => m_Active.Remove(agent);
    }
}
