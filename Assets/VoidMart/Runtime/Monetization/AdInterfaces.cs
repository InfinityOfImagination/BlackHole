using System;

namespace VoidMart.Monetization
{
    /// <summary>
    /// Mediation-agnostic ad surface (section 7).  Swap in an AppLovin MAX or AdMob adaptor by
    /// registering a different implementation with the ServiceLocator during boot; nothing in
    /// gameplay code changes.
    /// </summary>
    public interface IAdService
    {
        bool RewardedReady(string placementId);
        void ShowRewarded(string placementId, Action<bool> onComplete);
        bool InterstitialAllowed(string placementId);
        void ShowInterstitial(string placementId, Action onClosed);
        float InterstitialCooldownRemaining { get; }
        void NotifyInterstitialShown();
        void Tick(float deltaTime);
    }

    public interface IIapService
    {
        bool IsOwned(string productId);
        void Purchase(string productId, Action<bool> onComplete);
        void Restore(Action<bool> onComplete);
        string LocalizedPrice(string productId);
    }

    /// <summary>Platform services (Google Play Games v2 / Game Center) behind one seam.</summary>
    public interface IPlatformService
    {
        bool IsAuthenticated { get; }
        void Authenticate(Action<bool> onComplete);
        void SubmitScore(string leaderboardId, long score);
        void UnlockAchievement(string achievementId);
    }
}
