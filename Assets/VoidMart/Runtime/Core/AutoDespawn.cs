using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Returns a pooled effect to its pool after a fixed lifetime.</summary>
    public class AutoDespawn : MonoBehaviour, IPoolable
    {
        [SerializeField] float m_Lifetime = 1.2f;
        ParticleSystem m_Particles;
        float m_Timer;
        bool m_Searched;

        public void Configure(float lifetime) => m_Lifetime = lifetime;

        public void OnSpawned()
        {
            m_Timer = m_Lifetime;

            // Re-enabling a pooled object does not reliably restart a particle system, so drive
            // it explicitly.
            if (!m_Searched)
            {
                m_Particles = GetComponentInChildren<ParticleSystem>(true);
                m_Searched = true;
            }
            if (m_Particles == null) return;
            m_Particles.Clear(true);
            m_Particles.Play(true);
        }

        public void OnDespawned() => m_Timer = 0f;

        void Update()
        {
            if (m_Timer <= 0f) return;
            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f) return;

            var pools = ServiceLocator.Get<PoolService>();
            if (pools != null) pools.Despawn(gameObject);
            else gameObject.SetActive(false);
        }
    }
}
