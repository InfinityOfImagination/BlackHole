using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.Gameplay
{
    /// <summary>
    /// A consumable city prop.  Implements the consumption physics from spec section 2: props idle
    /// as kinematic rigidbodies, and the moment the hole radius intersects their boundary the body
    /// wakes up, the collision layer switches to SubGround and a non-linear squash tween runs —
    /// Scale(t) = lerp(1.0, 0.0, t^2.5).
    /// </summary>
    public class Swallowable : MonoBehaviour, IPoolable
    {
        public enum Phase { Idle, Attracted, Falling }

        [SerializeField] Transform m_Visual;
        [SerializeField] Rigidbody m_Body;
        [SerializeField] Collider m_Collider;

        public Phase CurrentPhase { get; private set; } = Phase.Idle;
        public float Mass { get; private set; } = 1f;
        public double Value { get; private set; } = 1d;
        public int Tier { get; private set; } = 1;
        public float Footprint { get; private set; } = 0.5f;
        public string PropId { get; private set; } = "";

        Vector3 m_BaseScale = Vector3.one;
        Vector3 m_SpinAxis = Vector3.up;
        float m_Timer;
        float m_Duration = 0.36f;
        float m_ScalePower = 2.5f;
        float m_Spin = 1.4f;
        int m_IdleLayer;
        PlayerHoleController m_Consumer;

        void Awake()
        {
            if (m_Visual == null) m_Visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
            if (m_Body == null) m_Body = GetComponent<Rigidbody>();
            if (m_Collider == null) m_Collider = GetComponentInChildren<Collider>();
            m_BaseScale = m_Visual != null ? m_Visual.localScale : Vector3.one;
            m_IdleLayer = gameObject.layer;
        }

        public void Configure(PropDefinition definition, float scaleMultiplier, EconomyService economy)
        {
            PropId = definition.id;
            Tier = definition.tier;
            Mass = definition.mass * scaleMultiplier;
            Footprint = definition.footprint * scaleMultiplier;
            Value = economy != null ? economy.ValueForMass(Mass, Tier) : Mass * definition.baseValue;

            if (m_Visual != null)
            {
                m_BaseScale = Vector3.one * scaleMultiplier;
                m_Visual.localScale = m_BaseScale;
            }
        }

        public void OnSpawned()
        {
            CurrentPhase = Phase.Idle;
            m_Timer = 0f;
            m_Consumer = null;
            gameObject.layer = m_IdleLayer;
            if (m_Visual != null) m_Visual.localScale = m_BaseScale;
            if (m_Collider != null) m_Collider.enabled = true;
            if (m_Body != null)
            {
                m_Body.isKinematic = true;   // idle props never cost simulation time
                m_Body.useGravity = false;
            }
        }

        public void OnDespawned()
        {
            CurrentPhase = Phase.Idle;
            m_Consumer = null;
            if (m_Body != null)
            {
                m_Body.isKinematic = true;
                m_Body.useGravity = false;
            }
        }

        public bool CanBeEaten(float holeRadius, float tolerance) => CurrentPhase == Phase.Idle && Footprint <= holeRadius * tolerance;

        /// <summary>Gentle drift toward the rim while the hole is still too small to swallow it.</summary>
        public void ApplyAttraction(Vector3 holeCentre, float strength, float deltaTime)
        {
            if (CurrentPhase != Phase.Idle) return;
            Vector3 offset = holeCentre - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance < 0.001f) return;
            transform.position += offset / distance * Mathf.Min(strength * deltaTime, distance);
        }

        public void BeginSwallow(PlayerHoleController hole, float duration, float scalePower, float spin)
        {
            if (CurrentPhase == Phase.Falling) return;

            CurrentPhase = Phase.Falling;
            m_Consumer = hole;
            m_Timer = 0f;
            m_Duration = Mathf.Max(0.05f, duration);
            m_ScalePower = Mathf.Max(0.5f, scalePower);
            m_Spin = spin;
            m_SpinAxis = Random.onUnitSphere;

            gameObject.layer = Layers.SubGround;
            if (m_Collider != null) m_Collider.enabled = false;
            if (m_Body != null)
            {
                m_Body.isKinematic = false;  // spec: wake the body as it is consumed
                m_Body.useGravity = true;
            }
        }

        void Update()
        {
            if (CurrentPhase != Phase.Falling) return;

            m_Timer += Time.deltaTime;
            float t = Mathf.Clamp01(m_Timer / m_Duration);

            if (m_Visual != null)
            {
                // Squash & stretch: a fast non-linear collapse so small props vanish with a pop.
                float scale = Mathf.Lerp(1f, 0f, Mathf.Pow(t, m_ScalePower));
                float squash = 1f + 0.35f * Mathf.Sin(t * Mathf.PI);
                m_Visual.localScale = new Vector3(
                    m_BaseScale.x * scale * squash,
                    m_BaseScale.y * scale / Mathf.Max(0.2f, squash),
                    m_BaseScale.z * scale * squash);
                m_Visual.Rotate(m_SpinAxis, m_Spin * 360f * Time.deltaTime, Space.Self);
            }

            if (m_Consumer != null)
            {
                Vector3 target = m_Consumer.SwallowPoint;
                Vector3 position = transform.position;
                float pull = Mathf.Lerp(6f, 26f, t);
                transform.position = Vector3.Lerp(position, target, 1f - Mathf.Exp(-pull * Time.deltaTime));
            }

            if (t >= 1f) Finish();
        }

        void Finish()
        {
            var consumer = m_Consumer;
            m_Consumer = null;
            CurrentPhase = Phase.Idle;
            consumer?.CompleteSwallow(this);
            ServiceLocator.Get<PoolService>()?.Despawn(gameObject);
        }
    }
}
