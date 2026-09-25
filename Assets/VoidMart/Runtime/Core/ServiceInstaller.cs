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

            Install();
            RegisterTweakables();
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
