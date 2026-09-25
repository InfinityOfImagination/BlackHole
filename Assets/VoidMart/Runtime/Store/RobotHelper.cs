using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.Store
{
    /// <summary>
    /// Hired help.  Robots sweep the register for cash the player has not picked up yet and bank
    /// it automatically, which is what turns the shop from "tap everything" into a real idle loop.
    /// </summary>
    public class RobotHelper : MonoBehaviour, IPoolable
    {
        enum State { Idle, ToRegister, Collecting, ToBank }

        [SerializeField] Transform m_Visual;
        [SerializeField] StackVisual m_CarryStack;

        GameConfig m_Config;
        StoreManager m_Store;
        EconomyService m_Economy;
        Transform m_Bank;

        State m_State = State.Idle;
        double m_Carried;
        float m_Timer;
        float m_Phase;
        Vector3 m_IdleAnchor;

        public void Configure(GameConfig config, Transform bank)
        {
            m_Config = config;
            m_Bank = bank;
        }

        public void OnSpawned()
        {
            m_State = State.Idle;
            m_Carried = 0d;
            m_IdleAnchor = transform.position;
            m_CarryStack?.SetCount(0);
        }

        public void OnDespawned() => m_Carried = 0d;

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Store = ServiceLocator.Get<StoreManager>();
            m_Economy = ServiceLocator.Get<EconomyService>();
            m_IdleAnchor = transform.position;
        }

        void Update()
        {
            if (m_Config == null || m_Store == null) return;
            float deltaTime = Time.deltaTime;
            var register = m_Store.Register;

            switch (m_State)
            {
                case State.Idle:
                    Hover(deltaTime);
                    if (register != null && register.PendingCash > 0.5d) m_State = State.ToRegister;
                    break;

                case State.ToRegister:
                    if (register == null) { m_State = State.Idle; break; }
                    if (MoveToward(register.CashPoint.position, deltaTime))
                    {
                        m_Carried = register.PendingCash;
                        register.CollectAll(transform);
                        m_CarryStack?.SetCount(m_CarryStack != null ? Mathf.Min(6, m_CarryStack.MaxVisible) : 0);
                        m_Timer = 0.25f;
                        m_State = State.Collecting;
                    }
                    break;

                case State.Collecting:
                    m_Timer -= deltaTime;
                    if (m_Timer <= 0f) m_State = State.ToBank;
                    break;

                case State.ToBank:
                    Vector3 target = m_Bank != null ? m_Bank.position : m_IdleAnchor;
                    if (MoveToward(target, deltaTime))
                    {
                        m_Carried = 0d;
                        m_CarryStack?.SetCount(0);
                        m_State = State.Idle;
                    }
                    break;
            }
        }

        void Hover(float deltaTime)
        {
            m_Phase += deltaTime * 2.2f;
            if (m_Visual == null) return;
            var local = m_Visual.localPosition;
            local.y = 0.12f + Mathf.Sin(m_Phase) * 0.06f;
            m_Visual.localPosition = local;
            m_Visual.localRotation = Quaternion.Euler(0f, Mathf.Sin(m_Phase * 0.7f) * 12f, 0f);
        }

        bool MoveToward(Vector3 target, float deltaTime)
        {
            Vector3 position = transform.position;
            Vector3 offset = target - position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= 0.4f) return true;

            float speed = m_Config.store.robotSpeed;
            Vector3 direction = offset / distance;
            transform.position = position + direction * Mathf.Min(speed * deltaTime, distance);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 480f * deltaTime);
            Hover(deltaTime);
            return false;
        }
    }
}
