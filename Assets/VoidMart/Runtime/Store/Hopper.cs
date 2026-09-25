using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Gameplay;
using VoidMart.Services;

namespace VoidMart.Store
{
    /// <summary>
    /// The unload station.  Park the full hole over the pad and it spits the city back out into
    /// the hopper: mass streams across on a timer, junk chunks arc into the funnel, and the
    /// pneumatic loop's volume tracks the live transfer rate exactly as the audio spec asks.
    /// </summary>
    public class Hopper : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] Transform m_Mouth;
        [SerializeField] Transform m_Pad;
        [SerializeField] float m_Radius = 2.8f;
        [SerializeField] StackVisual m_Fill;

        PlayerHoleController m_Hole;
        StoreManager m_Store;
        AudioService m_Audio;
        float m_SpawnAccumulator;
        float m_Buffer;

        public bool PlayerInRange { get; private set; }
        public float LastRate { get; private set; }

        public void Configure(GameConfig config, Transform mouth, Transform pad, float radius)
        {
            m_Config = config;
            if (mouth != null) m_Mouth = mouth;
            if (pad != null) m_Pad = pad;
            m_Radius = radius;
        }

        /// <summary>Runtime configuration that leaves the prefab's bound transforms alone.</summary>
        public void ConfigureRuntime(GameConfig config, float radius)
        {
            m_Config = config;
            m_Radius = radius;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Hole = ServiceLocator.Get<PlayerHoleController>();
            m_Store = ServiceLocator.Get<StoreManager>();
            m_Audio = ServiceLocator.Get<AudioService>();
            if (m_Mouth == null) m_Mouth = transform;
            if (m_Pad == null) m_Pad = transform;
        }

        void Update()
        {
            if (m_Hole == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }
            if (m_Config == null) return;

            Vector3 offset = m_Hole.transform.position - m_Pad.position;
            offset.y = 0f;
            PlayerInRange = offset.sqrMagnitude <= m_Radius * m_Radius;

            if (!PlayerInRange || m_Hole.Load <= 0f)
            {
                LastRate = 0f;
                return;
            }

            float multiplier = m_Store != null ? m_Store.UnloadMultiplier : 1f;
            float rate = m_Config.store.unloadRate * multiplier;
            float requested = rate * Time.deltaTime;
            float drained = m_Hole.DrainLoad(requested);
            if (drained <= 0f)
            {
                LastRate = 0f;
                return;
            }

            LastRate = drained / Mathf.Max(0.0001f, Time.deltaTime);
            m_Store?.AddRawMass(drained);
            m_Buffer += drained;

            m_Config.events?.unloadRate?.Raise(Mathf.Clamp01(LastRate / Mathf.Max(1f, rate)));
            m_Audio?.SetUnloadRate(Mathf.Clamp01(LastRate / Mathf.Max(1f, rate)));

            SpawnJunkVisual(drained, rate);

            if (m_Hole.Load <= 0.0001f)
            {
                m_Config.events?.unloadFinished?.Raise();
                var economy = ServiceLocator.Get<EconomyService>();
                economy?.AddXp(m_Config.economy.xpPerProp * 2f);
            }
        }

        void SpawnJunkVisual(float drained, float rate)
        {
            m_SpawnAccumulator += drained;
            float perChunk = Mathf.Max(1f, rate * 0.12f);
            if (m_SpawnAccumulator < perChunk) return;
            m_SpawnAccumulator = 0f;

            var pools = ServiceLocator.Get<PoolService>();
            var flying = pools?.Spawn("fx_junk", m_Hole.transform.position + Vector3.up * 0.4f, Random.rotation);
            if (flying == null) return;

            var item = flying.GetComponent<FlyingItem>();
            if (item == null) return;
            item.Launch(m_Hole.transform.position + Vector3.up * 0.5f, m_Mouth.position, 2.6f, 0.38f, null, 2.4f);
        }

        public void SetFillVisual(float normalized)
        {
            if (m_Fill == null) return;
            m_Fill.SetCount(Mathf.RoundToInt(normalized * m_Fill.MaxVisible));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.4f);
            Gizmos.DrawWireSphere((m_Pad != null ? m_Pad : transform).position, m_Radius);
        }
    }
}
