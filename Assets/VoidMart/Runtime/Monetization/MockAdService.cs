using System;
using System.Collections;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Monetization
{
    /// <summary>
    /// Development stand-in for the mediation SDK.  It reproduces the timing contract of a real
    /// rewarded/interstitial unit (a short blocking "playback", a cooldown, an IAP bypass) so the
    /// rest of the game can be built and balanced before any SDK is dropped in.
    /// </summary>
    public class MockAdService : MonoBehaviour, IAdService
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] float m_SimulatedPlaybackSeconds = 0.8f;

        float m_Cooldown;

        public float InterstitialCooldownRemaining => m_Cooldown;

        public void Configure(GameConfig config) => m_Config = config;

        void Awake() => ServiceLocator.Register<IAdService>(this);

        void OnDestroy()
        {
            if (ReferenceEquals(ServiceLocator.Get<IAdService>(), this)) ServiceLocator.Unregister<IAdService>();
        }

        void Start()
        {
            var save = ServiceLocator.Get<Services.SaveService>();
            if (save?.Data != null) m_Cooldown = Mathf.Max(m_Cooldown, save.Data.meta.interstitialCooldown);
            if (m_Config != null) m_Cooldown = Mathf.Max(m_Cooldown, m_Config.monetization.firstInterstitialDelay);
        }

        void Update() => Tick(Time.unscaledDeltaTime);

        public void Tick(float deltaTime)
        {
            if (m_Cooldown <= 0f) return;
            m_Cooldown = Mathf.Max(0f, m_Cooldown - deltaTime);
            var save = ServiceLocator.Get<Services.SaveService>();
            if (save?.Data != null) save.Data.meta.interstitialCooldown = m_Cooldown;
        }

        public bool RewardedReady(string placementId) => m_Config == null || m_Config.monetization.adsEnabled;

        public void ShowRewarded(string placementId, Action<bool> onComplete)
        {
            if (!RewardedReady(placementId))
            {
                onComplete?.Invoke(false);
                return;
            }
            Debug.Log($"[VoidMart] Rewarded '{placementId}' playing (mock).");
            StartCoroutine(Playback(() => onComplete?.Invoke(true)));
        }

        public bool InterstitialAllowed(string placementId)
        {
            if (m_Config == null || !m_Config.monetization.adsEnabled) return false;
            var save = ServiceLocator.Get<Services.SaveService>();
            if (save?.Data != null && save.Data.profile.noAdsActive) return false; // skipped entirely for IAP users
            return m_Cooldown <= 0f;
        }

        public void ShowInterstitial(string placementId, Action onClosed)
        {
            if (!InterstitialAllowed(placementId))
            {
                onClosed?.Invoke();
                return;
            }
            Debug.Log($"[VoidMart] Interstitial '{placementId}' playing (mock).");
            NotifyInterstitialShown();
            StartCoroutine(Playback(() => onClosed?.Invoke()));
        }

        public void NotifyInterstitialShown()
        {
            m_Cooldown = m_Config != null ? m_Config.monetization.interstitialCooldown : 180f;
            var save = ServiceLocator.Get<Services.SaveService>();
            if (save?.Data != null) save.Data.meta.interstitialCooldown = m_Cooldown;
        }

        IEnumerator Playback(Action done)
        {
            yield return new WaitForSecondsRealtime(m_SimulatedPlaybackSeconds);
            done?.Invoke();
        }
    }
}
