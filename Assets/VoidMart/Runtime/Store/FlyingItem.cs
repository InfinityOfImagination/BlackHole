using System;
using UnityEngine;
using VoidMart.Core;

namespace VoidMart.Store
{
    /// <summary>
    /// A pooled object that arcs from A to B along a quadratic Bezier, used for cash bills popping
    /// out of the register, crafted goods gliding to a shelf, and junk flying into a hopper.
    /// </summary>
    public class FlyingItem : MonoBehaviour, IPoolable
    {
        [SerializeField] Transform m_Visual;

        Vector3 m_Start, m_Control, m_End;
        Vector3 m_SpinAxis;
        float m_Duration = 0.5f;
        float m_Timer;
        float m_Spin;
        bool m_Active;
        Action m_OnArrive;
        Vector3 m_BaseScale = Vector3.one;

        void Awake()
        {
            if (m_Visual == null) m_Visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            m_BaseScale = m_Visual.localScale;
        }

        public void OnSpawned()
        {
            m_Active = false;
            m_Timer = 0f;
            if (m_Visual != null) m_Visual.localScale = m_BaseScale;
        }

        public void OnDespawned()
        {
            m_Active = false;
            m_OnArrive = null;
        }

        public void Launch(Vector3 start, Vector3 end, float arcHeight, float duration, Action onArrive = null, float spin = 1.4f)
        {
            m_Start = start;
            m_End = end;
            m_Control = (start + end) * 0.5f + Vector3.up * arcHeight;
            m_Duration = Mathf.Max(0.05f, duration);
            m_Timer = 0f;
            m_OnArrive = onArrive;
            m_Spin = spin;
            m_SpinAxis = UnityEngine.Random.onUnitSphere;
            m_Active = true;
            transform.position = start;
        }

        void Update()
        {
            if (!m_Active) return;

            m_Timer += Time.deltaTime;
            float t = Mathf.Clamp01(m_Timer / m_Duration);
            transform.position = Easing.Bezier(m_Start, m_Control, m_End, t);

            if (m_Visual != null && m_Spin > 0f)
                m_Visual.Rotate(m_SpinAxis, m_Spin * 360f * Time.deltaTime, Space.Self);

            if (t < 1f) return;

            m_Active = false;
            var callback = m_OnArrive;
            m_OnArrive = null;
            callback?.Invoke();
            ServiceLocator.Get<PoolService>()?.Despawn(gameObject);
        }
    }
}
