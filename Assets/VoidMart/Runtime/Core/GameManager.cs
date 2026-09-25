using UnityEngine;
using VoidMart.Data;
using VoidMart.Monetization;
using VoidMart.Services;

namespace VoidMart.Core
{
    /// <summary>
    /// Coordinates the lifecycle states between the economy, production and save layers, owns the
    /// world-area switch (street vs storefront) and hands the puzzle overlay its work.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class GameManager : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;

        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Boot;
        public WorldArea Area { get; private set; } = WorldArea.Street;
        public double PendingOfflineReward { get; private set; }
        public bool FeverActive => m_FeverTimer > 0f;
        public float FeverRemaining => m_FeverTimer;
        public GameConfig Config => m_Config;

        float m_FeverTimer;
        float m_SessionTime;
        float m_FeverOfferTimer;

        GameEventBus Bus => m_Config != null ? m_Config.events : null;

        public void Configure(GameConfig config) => m_Config = config;

        void Awake()
        {
            Instance = this;
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            ServiceLocator.Register(this);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ServiceLocator.Get<GameManager>() == this) ServiceLocator.Unregister<GameManager>();
        }

        void Start()
        {
            var bus = Bus;
            if (bus != null)
            {
                bus.puzzleCompleted?.Register(OnPuzzleCompleted);
                bus.puzzleRequested?.Register(OnPuzzleRequested);
                bus.nodePurchased?.Register(OnNodePurchased);
            }

            SetState(GameState.Gathering);
            ServiceLocator.Get<AudioService>()?.PlayMusicForArea(Area);
            PayOfflineEarnings();
        }

        void OnDisable()
        {
            var bus = Bus;
            if (bus != null)
            {
                bus.puzzleCompleted?.Unregister(OnPuzzleCompleted);
                bus.puzzleRequested?.Unregister(OnPuzzleRequested);
                bus.nodePurchased?.Unregister(OnNodePurchased);
            }
        }

        void Update()
        {
            m_SessionTime += Time.unscaledDeltaTime;

            if (m_FeverTimer > 0f)
            {
                m_FeverTimer -= Time.deltaTime;
                if (m_FeverTimer <= 0f)
                {
                    m_FeverTimer = 0f;
                    Bus?.feverEnded?.Raise();
                }
            }

            if (m_Config != null && m_Config.monetization.feverOfferInterval > 0f)
                m_FeverOfferTimer += Time.unscaledDeltaTime;
        }

        // ------------------------------------------------------------- lifecycle

        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            Bus?.stateChanged?.Raise(next);
        }

        public void SetArea(WorldArea area)
        {
            if (Area == area) return;
            Area = area;
            Bus?.areaChanged?.Raise((int)area);
            ServiceLocator.Get<AudioService>()?.PlayMusicForArea(area);
            SetState(area == WorldArea.Store ? GameState.Storefront : GameState.Gathering);
        }

        // ---------------------------------------------------------------- offline

        void PayOfflineEarnings()
        {
            var save = ServiceLocator.Get<SaveService>();
            var economy = ServiceLocator.Get<EconomyService>();
            if (save == null || economy == null) return;

            double seconds = save.PendingOfflineSeconds;
            save.ConsumeOfflineTime();
            if (seconds <= 0d) return;

            double reward = economy.ComputeOfflineEarnings(seconds);
            if (reward <= 0.5d) return;

            PendingOfflineReward = reward;
            economy.AddCash(reward);
            Bus?.toast?.Raise(new ToastRequest("Welcome back! +" + NumberFormat.Money(reward) + " while away", ToastStyle.Reward));
        }

        /// <summary>rv_double_offline — multiplies the offline payout that was just granted.</summary>
        public void OfferDoubleOffline()
        {
            if (PendingOfflineReward <= 0d) return;
            var ads = ServiceLocator.Get<IAdService>();
            var economy = ServiceLocator.Get<EconomyService>();
            if (ads == null || economy == null || m_Config == null) return;

            double bonus = PendingOfflineReward * (m_Config.monetization.offlineAdMultiplier - 1d);
            PendingOfflineReward = 0d;
            ads.ShowRewarded(m_Config.monetization.rewardedOfflineId, success =>
            {
                if (!success) return;
                economy.AddCash(bonus);
                Bus?.toast?.Raise(new ToastRequest("Offline profit doubled!", ToastStyle.Reward));
            });
        }

        // ------------------------------------------------------------------ fever

        /// <summary>rv_fever_mode — 30 s of 3x hole size and auto-suction.</summary>
        public void RequestFever()
        {
            var ads = ServiceLocator.Get<IAdService>();
            if (ads == null || m_Config == null) { StartFever(); return; }
            ads.ShowRewarded(m_Config.monetization.rewardedFeverId, success => { if (success) StartFever(); });
        }

        public void StartFever()
        {
            if (m_Config == null) return;
            m_FeverTimer = m_Config.hole.feverDuration;
            m_FeverOfferTimer = 0f;
            Bus?.feverStarted?.Raise();
            Bus?.toast?.Raise(new ToastRequest("FEVER MODE!", ToastStyle.Reward));
        }

        public bool ShouldOfferFever()
        {
            if (m_Config == null || FeverActive) return false;
            var ads = ServiceLocator.Get<IAdService>();
            if (ads == null || !ads.RewardedReady(m_Config.monetization.rewardedFeverId)) return false;
            return m_FeverOfferTimer >= m_Config.monetization.feverOfferInterval;
        }

        // ----------------------------------------------------------------- puzzle

        void OnPuzzleRequested(string machineId) => SetState(GameState.Puzzle);

        /// <summary>
        /// int_level_clear: buying a new factory node is the game's "level cleared" beat, so that
        /// is where the interstitial is gated - behind its cooldown, and never for no-ads owners.
        /// </summary>
        void OnNodePurchased(string nodeId)
        {
            if (m_SessionTime < (m_Config != null ? m_Config.monetization.firstInterstitialDelay : 240f)) return;
            TryShowLevelClearInterstitial();
        }

        void OnPuzzleCompleted(PuzzleResult result)
        {
            SetState(Area == WorldArea.Store ? GameState.Storefront : GameState.Gathering);
            if (!result.solved) return;

            var economy = ServiceLocator.Get<EconomyService>();
            economy?.AddCash(result.reward);
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data != null)
            {
                save.Data.progress.puzzlesSolved++;
                save.MarkDirty();
            }
        }

        /// <summary>int_level_clear — gated behind the cooldown and skipped for no-ads owners.</summary>
        public void TryShowLevelClearInterstitial(System.Action onClosed = null)
        {
            var ads = ServiceLocator.Get<IAdService>();
            if (ads == null || m_Config == null) { onClosed?.Invoke(); return; }
            ads.ShowInterstitial(m_Config.monetization.interstitialLevelClearId, onClosed);
        }

        public float SessionTime => m_SessionTime;
    }
}
