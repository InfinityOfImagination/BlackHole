using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Gameplay;
using VoidMart.Services;

namespace VoidMart.Store
{
    /// <summary>
    /// The register.  Customers queue here, pay, and their money lands as a physical stack of
    /// cash the player drives over to collect (bills arc to the hole on a Bezier, per spec 5).
    /// </summary>
    public class Checkout : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] Transform[] m_QueuePoints;
        [SerializeField] Transform m_ServePoint;
        [SerializeField] Transform m_CashPoint;
        [SerializeField] StackVisual m_CashStack;
        [SerializeField] float m_CollectRadius = 2.4f;

        readonly List<CustomerAgent> m_Queue = new List<CustomerAgent>(16);
        StoreManager m_Store;
        EconomyService m_Economy;
        AudioService m_Audio;
        PlayerHoleController m_Hole;

        double m_PendingCash;
        float m_ServeTimer;
        float m_CollectCooldown;

        public double PendingCash => m_PendingCash;
        public int QueueLength => m_Queue.Count;
        public Transform ServePoint => m_ServePoint != null ? m_ServePoint : transform;
        public Transform CashPoint => m_CashPoint != null ? m_CashPoint : transform;

        public void Configure(GameConfig config) => m_Config = config;

        public void BindVisuals(Transform[] queuePoints, Transform servePoint, Transform cashPoint, StackVisual cashStack)
        {
            m_QueuePoints = queuePoints;
            m_ServePoint = servePoint;
            m_CashPoint = cashPoint;
            m_CashStack = cashStack;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Store = ServiceLocator.Get<StoreManager>();
            m_Economy = ServiceLocator.Get<EconomyService>();
            m_Audio = ServiceLocator.Get<AudioService>();
            m_Hole = ServiceLocator.Get<PlayerHoleController>();
            if (m_Store != null) m_Store.Register = this;
        }

        public Vector3 QueuePosition(int index)
        {
            if (m_QueuePoints != null && m_QueuePoints.Length > 0)
            {
                int clamped = Mathf.Clamp(index, 0, m_QueuePoints.Length - 1);
                Vector3 basePosition = m_QueuePoints[clamped].position;
                if (index < m_QueuePoints.Length) return basePosition;
                Vector3 direction = m_QueuePoints.Length > 1
                    ? (m_QueuePoints[m_QueuePoints.Length - 1].position - m_QueuePoints[m_QueuePoints.Length - 2].position).normalized
                    : Vector3.back;
                return basePosition + direction * (1.1f * (index - m_QueuePoints.Length + 1));
            }
            return transform.position + Vector3.back * (1.1f * (index + 1));
        }

        public int Join(CustomerAgent customer)
        {
            if (customer == null) return 0;
            if (!m_Queue.Contains(customer)) m_Queue.Add(customer);
            return m_Queue.IndexOf(customer);
        }

        public void Leave(CustomerAgent customer)
        {
            int index = m_Queue.IndexOf(customer);
            if (index < 0) return;
            m_Queue.RemoveAt(index);
            for (int i = 0; i < m_Queue.Count; i++) m_Queue[i].NotifyQueueIndex(i);
        }

        public int IndexOf(CustomerAgent customer) => m_Queue.IndexOf(customer);

        void Update()
        {
            ServeFront();
            TryCollect();
        }

        void ServeFront()
        {
            if (m_Queue.Count == 0) { m_ServeTimer = 0f; return; }
            var front = m_Queue[0];
            if (front == null) { m_Queue.RemoveAt(0); return; }
            if (!front.ReadyToPay) { m_ServeTimer = 0f; return; }

            float speed = m_Store != null ? m_Store.MachineSpeedMultiplier : 1f;
            m_ServeTimer += Time.deltaTime * Mathf.Max(0.25f, speed);
            float required = m_Config != null ? m_Config.store.checkoutSeconds : 0.9f;
            if (m_ServeTimer < required) return;

            m_ServeTimer = 0f;
            double paid = front.Pay();
            AddCash(paid, front.transform.position);
            Leave(front);
            front.OnPaid();
        }

        public void AddCash(double amount, Vector3 origin)
        {
            if (amount <= 0d) return;
            m_PendingCash += amount;
            m_Audio?.PlayCash();
            RefreshCashStack();

            var bus = m_Config != null ? m_Config.events : null;
            bus?.productSold?.Raise(new SaleInfo
            {
                quantity = 1,
                revenue = amount,
                worldPosition = origin
            });
        }

        void RefreshCashStack()
        {
            if (m_CashStack == null) return;
            double scale = m_Config != null ? System.Math.Max(1d, m_Config.economy.startingCash + 50d) : 50d;
            int count = Mathf.Clamp(Mathf.RoundToInt((float)(m_PendingCash / scale) * m_CashStack.MaxVisible), m_PendingCash > 0d ? 1 : 0, m_CashStack.MaxVisible);
            m_CashStack.SetCount(count);
        }

        void TryCollect()
        {
            if (m_PendingCash <= 0d) return;
            if (m_CollectCooldown > 0f) { m_CollectCooldown -= Time.deltaTime; return; }
            if (m_Hole == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }

            Vector3 offset = m_Hole.transform.position - CashPoint.position;
            offset.y = 0f;
            float radius = m_CollectRadius + m_Hole.Radius;
            if (offset.sqrMagnitude > radius * radius) return;

            CollectAll(m_Hole.transform);
        }

        public void CollectAll(Transform receiver)
        {
            if (m_PendingCash <= 0d) return;
            double amount = m_PendingCash;
            m_PendingCash = 0d;
            RefreshCashStack();
            m_CollectCooldown = 0.12f;

            int bills = Mathf.Clamp(Mathf.RoundToInt((float)amount / 25f), 3, 12);
            double perBill = amount / bills;
            var pools = ServiceLocator.Get<PoolService>();
            float flightSeconds = m_Config != null ? m_Config.store.billFlightSeconds : 0.55f;
            float arc = m_Config != null ? m_Config.store.billArcHeight : 2.4f;

            for (int i = 0; i < bills; i++)
            {
                var go = pools?.Spawn("fx_cash_bill", CashPoint.position + Random.insideUnitSphere * 0.25f, Random.rotation);
                var item = go != null ? go.GetComponent<FlyingItem>() : null;
                if (item == null)
                {
                    m_Economy?.AddCash(perBill);
                    continue;
                }
                Vector3 target = receiver != null ? receiver.position + Vector3.up * 0.2f : CashPoint.position;
                item.Launch(CashPoint.position, target, arc * Random.Range(0.7f, 1.3f),
                    flightSeconds * Random.Range(0.85f, 1.25f), () => m_Economy?.AddCash(perBill), 1.8f);
            }

            m_Audio?.PlayCash();
            Haptics.Play(HapticStrength.Light);
        }
    }
}
