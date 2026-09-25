using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.Store
{
    /// <summary>
    /// Stickman shopper.  Walks in, grabs goods off a shelf, queues at the register, pays and
    /// leaves.  No navmesh: the store is convex and the paths are short, so straight-line steering
    /// with a bob animation reads perfectly at this camera angle and costs almost nothing.
    /// </summary>
    public class CustomerAgent : MonoBehaviour, IPoolable
    {
        public enum State { Entering, ToShelf, Picking, ToQueue, InQueue, Paying, Leaving, Done }

        [SerializeField] Transform m_Visual;
        [SerializeField] StackVisual m_CarryStack;
        [SerializeField] float m_ArriveDistance = 0.35f;
        [SerializeField] float m_BobHeight = 0.07f;

        GameConfig m_Config;
        StoreManager m_Store;
        CustomerSpawner m_Spawner;
        AudioService m_Audio;

        State m_State = State.Entering;
        ProductShelf m_Shelf;
        Vector3 m_Target;
        float m_Timer;
        float m_Patience;
        float m_Phase;
        int m_Items;
        int m_QueueIndex;
        double m_Bill;

        public bool ReadyToPay => m_State == State.Paying;
        public int CarriedItems => m_Items;
        public State CurrentState => m_State;

        public void Configure(GameConfig config, CustomerSpawner spawner)
        {
            m_Config = config;
            m_Spawner = spawner;
        }

        public void BindVisuals(Transform visual, StackVisual carryStack)
        {
            m_Visual = visual;
            m_CarryStack = carryStack;
        }

        public void OnSpawned()
        {
            m_State = State.Entering;
            m_Items = 0;
            m_Bill = 0d;
            m_Timer = 0f;
            m_Shelf = null;
            m_QueueIndex = 0;
            m_Phase = Random.Range(0f, 8f);
            m_Store = ServiceLocator.Get<StoreManager>();
            m_Audio = ServiceLocator.Get<AudioService>();
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Patience = m_Config != null ? m_Config.store.customerPatience : 26f;
            m_CarryStack?.SetCount(0);
            m_Target = transform.position;
        }

        public void OnDespawned()
        {
            if (m_Store != null && m_Store.Register != null) m_Store.Register.Leave(this);
            m_Shelf = null;
        }

        public void NotifyQueueIndex(int index) => m_QueueIndex = index;

        void Update()
        {
            if (m_Config == null || m_Store == null) return;
            float deltaTime = Time.deltaTime;
            m_Patience -= deltaTime;

            switch (m_State)
            {
                case State.Entering: TickEntering(); break;
                case State.ToShelf: TickToShelf(deltaTime); break;
                case State.Picking: TickPicking(deltaTime); break;
                case State.ToQueue: TickToQueue(deltaTime); break;
                case State.InQueue: TickInQueue(deltaTime); break;
                case State.Paying: MoveToward(m_Store.Register != null ? m_Store.Register.ServePoint.position : m_Target, deltaTime); break;
                case State.Leaving: TickLeaving(deltaTime); break;
            }

            if (m_Patience <= 0f && m_State != State.Leaving && m_State != State.Done) GiveUp();
        }

        void TickEntering()
        {
            m_Shelf = m_Store.FindShelfWithStock();
            if (m_Shelf != null)
            {
                m_Target = m_Shelf.PickPoint.position;
                m_State = State.ToShelf;
                return;
            }
            // Browse in place until stock shows up.
            m_Target = transform.position;
        }

        void TickToShelf(float deltaTime)
        {
            if (m_Shelf == null || m_Shelf.Stock <= 0)
            {
                m_State = State.Entering;
                return;
            }
            if (MoveToward(m_Shelf.PickPoint.position, deltaTime))
            {
                m_State = State.Picking;
                m_Timer = 0.35f;
            }
        }

        void TickPicking(float deltaTime)
        {
            m_Timer -= deltaTime;
            if (m_Timer > 0f) return;

            int wanted = Mathf.Max(1, m_Config.store.itemsPerCustomer);
            int taken = m_Shelf != null ? m_Shelf.Take(wanted) : 0;
            if (taken <= 0)
            {
                m_State = State.Entering;
                return;
            }

            var economy = ServiceLocator.Get<EconomyService>();
            var definition = m_Shelf.Definition;
            double unitPrice = economy != null ? economy.PriceFor(definition) : (definition?.basePrice ?? 5d);

            m_Items += taken;
            m_Bill += unitPrice * taken;
            m_CarryStack?.SetCount(Mathf.Min(m_Items, m_CarryStack.MaxVisible));

            m_State = State.ToQueue;
        }

        void TickToQueue(float deltaTime)
        {
            var register = m_Store.Register;
            if (register == null) { GiveUp(); return; }

            m_QueueIndex = register.Join(this);
            Vector3 slot = register.QueuePosition(m_QueueIndex);
            if (MoveToward(slot, deltaTime)) m_State = State.InQueue;
        }

        void TickInQueue(float deltaTime)
        {
            var register = m_Store.Register;
            if (register == null) { GiveUp(); return; }

            m_QueueIndex = register.IndexOf(this);
            if (m_QueueIndex < 0) { m_State = State.ToQueue; return; }

            if (m_QueueIndex == 0)
            {
                if (MoveToward(register.ServePoint.position, deltaTime)) m_State = State.Paying;
            }
            else
            {
                MoveToward(register.QueuePosition(m_QueueIndex), deltaTime);
            }
        }

        void TickLeaving(float deltaTime)
        {
            Vector3 exit = m_Store.CustomerExit != null ? m_Store.CustomerExit.position : m_Target;
            if (!MoveToward(exit, deltaTime)) return;
            m_State = State.Done;
            Despawn();
        }

        /// <summary>Called by the register once the checkout timer elapses.</summary>
        public double Pay()
        {
            double amount = m_Bill;
            m_Bill = 0d;
            m_Items = 0;
            m_CarryStack?.SetCount(0);

            var economy = ServiceLocator.Get<EconomyService>();
            economy?.AddXp(m_Config != null ? m_Config.economy.xpPerSale : 5f);
            return amount;
        }

        public void OnPaid()
        {
            m_State = State.Leaving;
            m_Patience = 30f;
            m_Audio?.PlaySfx(AudioService.SfxSell, Random.Range(0.95f, 1.1f), 0.5f, 4);
        }

        void GiveUp()
        {
            // Put everything back on the shelf, not just one unit.
            if (m_Shelf != null)
                for (int i = 0; i < m_Items; i++) m_Shelf.DeliverIncoming(m_Shelf.ProductId);
            m_Items = 0;
            m_Bill = 0d;
            m_CarryStack?.SetCount(0);
            m_Store?.Register?.Leave(this);
            m_State = State.Leaving;
            m_Patience = 30f;
        }

        void Despawn()
        {
            m_Spawner?.NotifyDespawn(this);
            ServiceLocator.Get<PoolService>()?.Despawn(gameObject);
        }

        bool MoveToward(Vector3 target, float deltaTime)
        {
            Vector3 position = transform.position;
            Vector3 offset = target - position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= m_ArriveDistance) return true;

            float speed = m_Config.store.customerMoveSpeed;
            Vector3 direction = offset / distance;
            transform.position = position + direction * Mathf.Min(speed * deltaTime, distance);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 720f * deltaTime);

            if (m_Visual != null)
            {
                m_Phase += deltaTime * 10f;
                var local = m_Visual.localPosition;
                local.y = Mathf.Abs(Mathf.Sin(m_Phase)) * m_BobHeight;
                m_Visual.localPosition = local;
            }
            return false;
        }
    }
}
