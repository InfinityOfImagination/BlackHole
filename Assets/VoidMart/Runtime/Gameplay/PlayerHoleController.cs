using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;
using VoidMart.Tweaker;

namespace VoidMart.Gameplay
{
    /// <summary>
    /// The player: a sentient black hole that slides around the district eating props, grows as it
    /// feeds, and empties itself into the store's hoppers.  Tunables are mirrored onto the
    /// component (and registered with the runtime tweaker) exactly as the spec illustrates, while
    /// the authored defaults live in <see cref="GameConfig"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerHoleController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] GameConfig m_Config;
        [SerializeField] Transform m_VisualRoot;
        [SerializeField] Transform m_MaskQuad;
        [SerializeField] Transform m_PitMesh;
        [SerializeField] Transform m_RimRing;

        [Header("Live values (mirrored from GameConfig)")]
        [Tweakable("Hole_Physics", 1f, 25f)] public float moveSpeed = 8.5f;
        [Tweakable("Hole_Physics", 0.5f, 10f)] public float initialRadius = 1.2f;
        [Tweakable("Hole_Physics", 10f, 500f)] public float maxCapacity = 50f;
        [Tweakable("Hole_Physics", 0f, 60f)] public float suctionForce = 16f;
        [Tweakable("Hole_Physics", 0.5f, 6f)] public float suctionRadiusMultiplier = 2.1f;

        const int MaxOverlap = 96;
        readonly Collider[] m_Overlap = new Collider[MaxOverlap];

        Vector3 m_Velocity;
        float m_Radius = 1.2f;
        float m_VisualRadius = 1.2f;
        float m_Load;
        double m_LoadValue;
        int m_Tier = 1;
        float m_PunchTimer;
        bool m_FeverActive;
        float m_FeverMultiplier = 1f;
        Bounds m_Bounds;
        bool m_BoundsSet;

        public float Radius => m_Radius * m_FeverMultiplier;
        public float VisualRadius => m_VisualRadius;
        public float Load => m_Load;
        public double LoadValue => m_LoadValue;
        /// <summary>Authored capacity scaled by every capacity upgrade the player owns.</summary>
        public float Capacity => maxCapacity * (m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.HoleCapacity) : 1f);
        public float FillNormalized => Capacity <= 0f ? 0f : Mathf.Clamp01(m_Load / Capacity);
        public bool IsFull => m_Load >= Capacity - 0.001f;
        public int Tier => m_Tier;
        public Vector3 SwallowPoint => transform.position + Vector3.down * (Radius * 0.55f);
        public Vector3 Velocity => m_Velocity;
        public GameConfig Config => m_Config;

        GameEventBus Bus => m_Config != null ? m_Config.events : null;
        EconomyService m_Economy;
        AudioService m_Audio;

        public void Configure(GameConfig config) => m_Config = config;

        void Awake()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            SyncFromConfig();
            RuntimeTweakerService.Register(this);
            ServiceLocator.Register(this);
        }

        void OnDestroy()
        {
            RuntimeTweakerService.Unregister(this);
            if (ServiceLocator.Get<PlayerHoleController>() == this) ServiceLocator.Unregister<PlayerHoleController>();
        }

        void Start()
        {
            m_Economy = ServiceLocator.Get<EconomyService>();
            m_Audio = ServiceLocator.Get<AudioService>();

            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data != null)
            {
                m_Radius = Mathf.Max(initialRadius, save.Data.hole.currentRadius);
                maxCapacity = Mathf.Max(1f, save.Data.hole.capacityMax);
                m_Tier = Mathf.Max(1, save.Data.hole.level);
            }
            else
            {
                m_Radius = initialRadius;
            }

            m_VisualRadius = m_Radius;
            ApplyVisualScale(true);

            var bus = Bus;
            if (bus != null)
            {
                bus.feverStarted?.Register(OnFeverStarted);
                bus.feverEnded?.Register(OnFeverEnded);
            }
            RaiseFill();
        }

        void OnDisable()
        {
            var bus = Bus;
            if (bus != null)
            {
                bus.feverStarted?.Unregister(OnFeverStarted);
                bus.feverEnded?.Unregister(OnFeverEnded);
            }
        }

        public void SyncFromConfig()
        {
            if (m_Config == null) return;
            var hole = m_Config.hole;
            moveSpeed = hole.moveSpeed;
            initialRadius = hole.initialRadius;
            maxCapacity = hole.maxCapacity;
            suctionForce = hole.suctionForce;
            suctionRadiusMultiplier = hole.suctionRadiusMultiplier;
        }

        public void SetBounds(Bounds bounds)
        {
            m_Bounds = bounds;
            m_BoundsSet = true;
        }

        void OnFeverStarted()
        {
            m_FeverActive = true;
            m_FeverMultiplier = m_Config != null ? m_Config.hole.feverSizeMultiplier : 3f;
        }

        void OnFeverEnded()
        {
            m_FeverActive = false;
            m_FeverMultiplier = 1f;
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            Move(deltaTime);
            Consume(deltaTime);
            UpdateVisual(deltaTime);
        }

        // ------------------------------------------------------------- movement

        void Move(float deltaTime)
        {
            var hole = m_Config != null ? m_Config.hole : null;
            float acceleration = hole?.acceleration ?? 34f;
            float deceleration = hole?.deceleration ?? 26f;

            Vector2 axis = PlayerInputRouter.MoveAxis;
            Vector3 desired = new Vector3(axis.x, 0f, axis.y) * (moveSpeed * SpeedMultiplier);
            float rate = desired.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            m_Velocity = Vector3.MoveTowards(m_Velocity, desired, rate * deltaTime);

            if (m_Velocity.sqrMagnitude > 0.000001f)
            {
                Vector3 position = transform.position + m_Velocity * deltaTime;
                if (m_BoundsSet)
                {
                    float padding = Radius;
                    position.x = Mathf.Clamp(position.x, m_Bounds.min.x + padding, m_Bounds.max.x - padding);
                    position.z = Mathf.Clamp(position.z, m_Bounds.min.z + padding, m_Bounds.max.z - padding);
                }
                position.y = 0f;
                transform.position = position;
            }
        }

        float SpeedMultiplier => m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.HoleSpeed) : 1f;

        // -------------------------------------------------------------- eating

        void Consume(float deltaTime)
        {
            if (m_Config == null) return;
            var hole = m_Config.hole;
            float radius = Radius;
            float suctionRadius = radius * suctionRadiusMultiplier;
            if (m_FeverActive) suctionRadius = Mathf.Max(suctionRadius, hole.feverAutoSuctionRadius);

            int count = Physics.OverlapSphereNonAlloc(transform.position, suctionRadius, m_Overlap, Layers.SwallowableMask, QueryTriggerInteraction.Collide);
            bool full = IsFull;

            for (int i = 0; i < count; i++)
            {
                var collider = m_Overlap[i];
                if (collider == null) continue;
                var prop = collider.GetComponentInParent<Swallowable>();
                if (prop == null || prop.CurrentPhase != Swallowable.Phase.Idle) continue;

                Vector3 offset = prop.transform.position - transform.position;
                offset.y = 0f;
                float distance = offset.magnitude;

                bool sizeOk = prop.CanBeEaten(radius, hole.swallowTolerance);
                if (!sizeOk)
                {
                    // Too big for now: nudge it so the player can see it wobble at the rim.
                    if (distance < radius * 1.15f) prop.ApplyAttraction(transform.position, suctionForce * 0.08f, deltaTime);
                    continue;
                }

                if (full) continue;

                if (distance <= radius * hole.swallowTolerance)
                {
                    prop.BeginSwallow(this, hole.swallowDuration, hole.swallowScalePower, hole.swallowSpin);
                }
                else
                {
                    float falloff = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, suctionRadius));
                    float pull = suctionForce * falloff * (m_FeverActive ? 2.2f : 1f) * SuctionMultiplier;
                    prop.ApplyAttraction(transform.position, pull, deltaTime);
                }
            }
        }

        float SuctionMultiplier => m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.SuctionPower) : 1f;

        /// <summary>Called by a prop once its swallow tween finishes.</summary>
        public void CompleteSwallow(Swallowable prop)
        {
            if (prop == null) return;

            m_Load = Mathf.Min(Capacity, m_Load + prop.Mass);
            m_LoadValue += prop.Value;
            Grow(prop.Mass);

            m_Audio?.PlaySwallowPop();
            Haptics.Play(prop.Tier >= 3 ? HapticStrength.Medium : HapticStrength.Light);

            var bus = Bus;
            if (bus != null)
            {
                bus.propSwallowed?.Raise(new SwallowInfo
                {
                    mass = prop.Mass,
                    value = prop.Value,
                    tier = prop.Tier,
                    streak = m_Audio != null ? m_Audio.SwallowStreak : 0,
                    worldPosition = prop.transform.position
                });
            }

            if (m_Economy != null) m_Economy.AddXp(m_Config.economy.xpPerProp);

            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data != null)
            {
                save.Data.progress.totalPropsEaten++;
                save.Data.hole.currentRadius = m_Radius;
                save.MarkDirty();
            }

            RaiseFill();
            if (IsFull) bus?.holeFull?.Raise();
        }

        void Grow(float mass)
        {
            var hole = m_Config.hole;
            float radiusMultiplier = m_Economy != null ? m_Economy.GetMultiplier(UpgradeEffect.HoleRadius) : 1f;
            float falloff = Mathf.Pow(Mathf.Max(0.05f, initialRadius / Mathf.Max(initialRadius, m_Radius)), hole.growthFalloff);
            m_Radius = Mathf.Min(hole.maxRadius, m_Radius + mass * hole.radiusPerMass * falloff * radiusMultiplier);
            m_PunchTimer = 1f;

            int tier = 1;
            for (int i = 0; i < hole.tierRadius.Length; i++)
                if (m_Radius >= hole.tierRadius[i]) tier = i + 1;

            if (tier != m_Tier)
            {
                m_Tier = tier;
                Bus?.holeTierChanged?.Raise(tier);
                var save = ServiceLocator.Get<SaveService>();
                if (save?.Data != null) save.Data.hole.level = tier;
            }
        }

        void RaiseFill() => Bus?.holeFillChanged?.Raise(FillNormalized);

        // ------------------------------------------------------------ unloading

        /// <summary>Drains mass into a hopper; returns how much was actually transferred.</summary>
        public float DrainLoad(float requested)
        {
            if (m_Load <= 0f || requested <= 0f) return 0f;
            float taken = Mathf.Min(m_Load, requested);
            float ratio = m_Load > 0f ? taken / m_Load : 0f;
            m_Load -= taken;
            m_LoadValue -= m_LoadValue * ratio;
            if (m_Load < 0.001f) { m_Load = 0f; m_LoadValue = 0d; }
            RaiseFill();
            return taken;
        }

        /// <summary>Editor/debug helper: tops the hole up to its current capacity.</summary>
        public void FillLoad()
        {
            m_Load = Capacity;
            m_LoadValue = m_Economy != null ? m_Economy.ValueForMass(m_Load, m_Tier) : m_Load;
            RaiseFill();
            Bus?.holeFull?.Raise();
        }

        public void ClearLoad()
        {
            m_Load = 0f;
            m_LoadValue = 0d;
            RaiseFill();
        }

        // --------------------------------------------------------------- visual

        void UpdateVisual(float deltaTime)
        {
            m_VisualRadius = Easing.Damp(m_VisualRadius, Radius, 9f, deltaTime);
            if (m_PunchTimer > 0f) m_PunchTimer = Mathf.Max(0f, m_PunchTimer - deltaTime * 3.4f);
            ApplyVisualScale(false);
        }

        void ApplyVisualScale(bool immediate)
        {
            float punch = 0f;
            if (!immediate && m_PunchTimer > 0f && m_Config != null)
                punch = Mathf.Sin(m_PunchTimer * Mathf.PI) * m_Config.hole.growthPunch;

            float radius = (immediate ? Radius : m_VisualRadius) * (1f + punch);
            float diameter = radius * 2f;

            if (m_VisualRoot != null) m_VisualRoot.localScale = new Vector3(diameter, 1f, diameter);
            if (m_MaskQuad != null) m_MaskQuad.localScale = new Vector3(diameter, diameter, 1f);
            if (m_RimRing != null) m_RimRing.localScale = new Vector3(diameter * 1.06f, 1f, diameter * 1.06f);
            // Slightly wider than the stencil circle: the stencil clips the overhang, and the
            // extra width guarantees no sliver of sky shows between the ground and the shaft.
            if (m_PitMesh != null) m_PitMesh.localScale = new Vector3(diameter * 1.04f, Mathf.Max(3f, diameter * 1.6f), diameter * 1.04f);
        }

        public void BindVisuals(Transform visualRoot, Transform maskQuad, Transform pit, Transform rim)
        {
            m_VisualRoot = visualRoot;
            m_MaskQuad = maskQuad;
            m_PitMesh = pit;
            m_RimRing = rim;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? Radius : initialRadius);
            Gizmos.color = new Color(1f, 0.24f, 0.65f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, (Application.isPlaying ? Radius : initialRadius) * suctionRadiusMultiplier);
        }
#endif
    }
}
