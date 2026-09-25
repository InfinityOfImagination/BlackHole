using System;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Monetization
{
    /// <summary>
    /// Section 9 seam.  Google Play Games Services v2 authenticates silently in the background;
    /// this local implementation mimics that contract and carries the cloud-save conflict rule
    /// (newest meaningful progress wins) so the resolution logic is testable without the SDK.
    /// </summary>
    public class LocalPlatformService : MonoBehaviour, IPlatformService
    {
        public bool IsAuthenticated { get; private set; }

        void Awake() => ServiceLocator.Register<IPlatformService>(this);

        void OnDestroy()
        {
            if (ReferenceEquals(ServiceLocator.Get<IPlatformService>(), this)) ServiceLocator.Unregister<IPlatformService>();
        }

        void Start() => Authenticate(null);

        public void Authenticate(Action<bool> onComplete)
        {
            IsAuthenticated = true; // real GPGS v2 does this automatically at process start
            onComplete?.Invoke(true);
        }

        public void SubmitScore(string leaderboardId, long score) { }

        public void UnlockAchievement(string achievementId) { }

        /// <summary>Cloud/local save conflict: prefer the payload with more lifetime progress.</summary>
        public static SaveData ResolveConflict(SaveData local, SaveData cloud)
        {
            if (local == null) return cloud;
            if (cloud == null) return local;
            if (Math.Abs(local.profile.lifetimeEarnings - cloud.profile.lifetimeEarnings) > 1d)
                return local.profile.lifetimeEarnings > cloud.profile.lifetimeEarnings ? local : cloud;
            return local.timestamp >= cloud.timestamp ? local : cloud;
        }
    }
}
