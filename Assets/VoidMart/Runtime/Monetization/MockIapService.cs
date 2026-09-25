using System;
using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.Monetization
{
    /// <summary>Local, non-billing IAP stand-in that still drives the real entitlement flow.</summary>
    public class MockIapService : MonoBehaviour, IIapService
    {
        [SerializeField] GameConfig m_Config;
        readonly HashSet<string> m_Owned = new HashSet<string>();

        public void Configure(GameConfig config) => m_Config = config;

        void Awake() => ServiceLocator.Register<IIapService>(this);

        void OnDestroy()
        {
            if (ReferenceEquals(ServiceLocator.Get<IIapService>(), this)) ServiceLocator.Unregister<IIapService>();
        }

        void Start()
        {
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data != null && save.Data.profile.noAdsActive && m_Config != null)
                m_Owned.Add(m_Config.monetization.iapNoAdsId);
        }

        public bool IsOwned(string productId) => m_Owned.Contains(productId);

        public string LocalizedPrice(string productId)
        {
            if (m_Config == null) return "$1.99";
            if (productId == m_Config.monetization.iapNoAdsId) return "$3.99";
            if (productId == m_Config.monetization.iapStarterPackId) return "$4.99";
            return "$1.99";
        }

        public void Purchase(string productId, Action<bool> onComplete)
        {
            m_Owned.Add(productId);
            var save = ServiceLocator.Get<SaveService>();
            var economy = ServiceLocator.Get<EconomyService>();

            if (m_Config != null && productId == m_Config.monetization.iapNoAdsId && save?.Data != null)
            {
                save.Data.profile.noAdsActive = true;
                save.MarkDirty();
                save.SaveNow();
            }
            else if (m_Config != null && productId == m_Config.monetization.iapStarterPackId)
            {
                economy?.AddGems(m_Config.monetization.starterPackGems);
                if (save?.Data != null)
                {
                    save.Data.hole.equippedSkin = m_Config.monetization.starterPackSkin;
                    save.MarkDirty();
                    save.SaveNow();
                }
            }

            m_Config?.events?.toast?.Raise(new ToastRequest("Purchase complete", ToastStyle.Reward));
            onComplete?.Invoke(true);
        }

        public void Restore(Action<bool> onComplete) => onComplete?.Invoke(true);
    }
}
