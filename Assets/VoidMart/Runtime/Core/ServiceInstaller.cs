using UnityEngine;
using VoidMart.Data;
using VoidMart.Monetization;
using VoidMart.Services;
using VoidMart.Tweaker;

namespace VoidMart.Core
{
    /// <summary>
    /// Creates and registers every long-lived service exactly once.  Lives in the Boot scene, but
    /// the Game scene carries one too so a designer can hit Play straight from Game.unity and get
    /// a fully wired session.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class ServiceInstaller : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] bool m_DontDestroyOnLoad = true;

        public static ServiceInstaller Active { get; private set; }
        public GameConfig Config => m_Config;

        public void Configure(GameConfig config) => m_Config = config;

        void Awake()
        {
            if (Active != null && Active != this)
            {
                Destroy(gameObject);
                return;
            }
            Active = this;
            if (m_DontDestroyOnLoad && transform.parent == null) DontDestroyOnLoad(gameObject);

            if (m_Config == null)
            {
                Debug.LogError("[VoidMart] ServiceInstaller has no GameConfig. Run Tools ▸ Void Mart ▸ One-Click Setup.");
                return;
            }

            ServiceLocator.Register(m_Config);
            if (m_Config.assets != null) ServiceLocator.Register(m_Config.assets);
            if (m_Config.events != null) ServiceLocator.Register(m_Config.events);

            ApplyFrameTarget();
            Install();
            RegisterCommonPools();
            RegisterTweakables();
            ConfigurePhysicsLayers();
            Haptics.ApplyFrom(m_Config.ui);
        }

        void Install()
        {
            EnsureChild<SaveService>("SaveService", s => s.Configure(m_Config));
            EnsureChild<EconomyService>("EconomyService", s => s.Configure(m_Config));
            EnsureChild<AudioService>("AudioService", s => s.Configure(m_Config));
            EnsureChild<PoolService>("PoolService", null);
            EnsureChild<MockAdService>("AdService", s => s.Configure(m_Config));
            EnsureChild<MockIapService>("IapService", s => s.Configure(m_Config));
            EnsureChild<LocalPlatformService>("PlatformService", null);
            EnsureChild<RuntimeTweakerOverlay>("RuntimeTweaker", null);
        }

        /// <summary>Uncapped vsync plus an explicit 60 FPS target, per the performance brief.</summary>
        static void ApplyFrameTarget()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        /// <summary>Effects and shoppers are shared across the whole game, so they pool here.</summary>
        void RegisterCommonPools()
        {
            var pools = ServiceLocator.Get<PoolService>();
            var assets = m_Config != null ? m_Config.assets : null;
            if (pools == null || assets == null) return;

            pools.Register("fx_cash_bill", assets.GetPrefab("fx_cash_bill"), 24, 128);
            pools.Register("fx_product_box", assets.GetPrefab("fx_product_box"), 16, 96);
            pools.Register("fx_junk", assets.GetPrefab("fx_junk"), 16, 96);
            pools.Register("fx_poof", assets.GetPrefab("fx_poof"), 4, 24);
            pools.Register("customer", assets.GetPrefab("customer"), Mathf.Max(4, m_Config.store.maxCustomers), 64);
        }

        T EnsureChild<T>(string childName, System.Action<T> configure) where T : Component
        {
            var existing = GetComponentInChildren<T>(true);
            if (existing != null)
            {
                configure?.Invoke(existing);
                return existing;
            }
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            var component = go.AddComponent<T>();
            configure?.Invoke(component);
            return component;
        }

        /// <summary>
        /// Props switch to the SubGround layer as they are eaten so they fall through the world
        /// instead of bouncing off it - that layer must collide with nothing.
        /// </summary>
        static void ConfigurePhysicsLayers()
        {
            int subGround = LayerMask.NameToLayer(Gameplay.Layers.SubGroundName);
            int swallowable = LayerMask.NameToLayer(Gameplay.Layers.SwallowableName);
            int player = LayerMask.NameToLayer(Gameplay.Layers.PlayerName);
            if (subGround < 0) return;

            for (int layer = 0; layer < 32; layer++) Physics.IgnoreLayerCollision(subGround, layer, true);
            if (swallowable >= 0 && player >= 0) Physics.IgnoreLayerCollision(swallowable, player, true);
        }

        void RegisterTweakables()
        {
            RuntimeTweakerService.Register(m_Config.hole);
            RuntimeTweakerService.Register(m_Config.cameraRig);
            RuntimeTweakerService.Register(m_Config.city);
            RuntimeTweakerService.Register(m_Config.store);
            RuntimeTweakerService.Register(m_Config.economy);
            RuntimeTweakerService.Register(m_Config.puzzle);
            RuntimeTweakerService.Register(m_Config.audio);
            RuntimeTweakerService.Register(m_Config.ui);
            RuntimeTweakerService.Register(m_Config.monetization);
            RuntimeTweakerService.Register(m_Config.persistence);
        }

        void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
                RuntimeTweakerService.Clear();
            }
        }
    }
}
