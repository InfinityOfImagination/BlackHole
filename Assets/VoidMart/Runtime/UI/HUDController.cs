using System;
using UnityEngine;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Gameplay;
using VoidMart.Services;

namespace VoidMart.UI
{
    /// <summary>
    /// The heads-up display from spec section 4: level + XP top-left, cash and gems top-right,
    /// the hole capacity gauge centre-bottom.  Everything is event driven, so the HUD never has to
    /// poll gameplay objects.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Serializable]
        public class Refs
        {
            public VMText cashLabel;
            public VMText gemLabel;
            public VMText levelLabel;
            public VMText capacityLabel;
            public VMText areaLabel;
            public Image xpFill;
            public Image capacityFill;
            public RectTransform cashGroup;
            public RectTransform gemGroup;
            public RectTransform capacityGroup;
            public GameObject feverButton;
            public GameObject upgradeButton;
            public Image capacityGlow;
        }

        [SerializeField] GameConfig m_Config;
        [SerializeField] Refs m_Refs = new Refs();

        EconomyService m_Economy;
        PlayerHoleController m_Hole;
        float m_CapacityDisplay;
        float m_FeverPulse;

        public Refs Widgets => m_Refs;

        public void Bind(GameConfig config, Refs refs)
        {
            m_Config = config;
            m_Refs = refs;
        }

        GameEventBus Bus => m_Config != null ? m_Config.events : null;

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            m_Economy = ServiceLocator.Get<EconomyService>();
            m_Hole = ServiceLocator.Get<PlayerHoleController>();

            var bus = Bus;
            if (bus != null)
            {
                bus.cashChanged?.Register(OnCash);
                bus.gemsChanged?.Register(OnGems);
                bus.xpChanged?.Register(OnXp);
                bus.playerLevelUp?.Register(OnLevelUp);
                bus.holeFillChanged?.Register(OnCapacity);
                bus.areaChanged?.Register(OnArea);
            }

            if (m_Economy != null)
            {
                OnCash(m_Economy.Cash);
                OnGems(m_Economy.Gems);
                OnXp(m_Economy.XpNormalized);
                OnLevelUp(m_Economy.PlayerLevel);
            }
            OnCapacity(0f);
            OnArea((int)WorldArea.Street);
        }

        void OnDestroy()
        {
            var bus = Bus;
            if (bus == null) return;
            bus.cashChanged?.Unregister(OnCash);
            bus.gemsChanged?.Unregister(OnGems);
            bus.xpChanged?.Unregister(OnXp);
            bus.playerLevelUp?.Unregister(OnLevelUp);
            bus.holeFillChanged?.Unregister(OnCapacity);
            bus.areaChanged?.Unregister(OnArea);
        }

        void OnCash(double value)
        {
            if (m_Refs.cashLabel != null) m_Refs.cashLabel.SetText(NumberFormat.Money(value));
            Punch(m_Refs.cashGroup);
        }

        void OnGems(int value)
        {
            if (m_Refs.gemLabel != null) m_Refs.gemLabel.SetText(NumberFormat.Compact(value));
            Punch(m_Refs.gemGroup);
        }

        void OnXp(float normalized)
        {
            if (m_Refs.xpFill != null) m_Refs.xpFill.fillAmount = Mathf.Clamp01(normalized);
        }

        void OnLevelUp(int level)
        {
            if (m_Refs.levelLabel != null) m_Refs.levelLabel.SetText("LV " + level);
            ServiceLocator.Get<AudioService>()?.PlaySfx(AudioService.SfxLevelUp, 1f, 0.8f);
        }

        void OnCapacity(float normalized)
        {
            if (m_Refs.capacityLabel == null || m_Hole == null) return;
            m_Refs.capacityLabel.SetText(Mathf.RoundToInt(normalized * 100f) + "%");
        }

        void OnArea(int area)
        {
            if (m_Refs.areaLabel != null)
                m_Refs.areaLabel.SetText((WorldArea)area == WorldArea.Store ? "STOREFRONT" : "THE STRIP");
        }

        readonly System.Collections.Generic.Dictionary<RectTransform, Coroutine> m_Punches =
            new System.Collections.Generic.Dictionary<RectTransform, Coroutine>(4);

        void Punch(RectTransform target)
        {
            if (target == null || m_Config == null || !isActiveAndEnabled) return;
            if (m_Punches.TryGetValue(target, out var running) && running != null) StopCoroutine(running);
            m_Punches[target] = StartCoroutine(UITween.Punch(target, m_Config.ui.hudPunchScale, 0.22f));
        }

        void Update()
        {
            if (m_Hole == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }

            float target = m_Hole.FillNormalized;
            m_CapacityDisplay = Easing.Damp(m_CapacityDisplay, target, 12f, Time.unscaledDeltaTime);
            if (m_Refs.capacityFill != null) m_Refs.capacityFill.fillAmount = m_CapacityDisplay;
            if (m_Refs.capacityLabel != null) m_Refs.capacityLabel.SetText(Mathf.RoundToInt(m_CapacityDisplay * 100f) + "%");

            if (m_Refs.capacityGlow != null && m_Config != null)
            {
                bool full = m_Hole.IsFull;
                float alpha = full ? 0.45f + Mathf.Sin(Time.unscaledTime * 6f) * 0.25f : 0f;
                var color = m_Config.theme.hotMagenta;
                color.a = alpha;
                m_Refs.capacityGlow.color = color;
            }

            var manager = GameManager.Instance;
            if (m_Refs.feverButton != null && manager != null)
            {
                bool show = manager.ShouldOfferFever();
                if (m_Refs.feverButton.activeSelf != show) m_Refs.feverButton.SetActive(show);
                if (show)
                {
                    m_FeverPulse += Time.unscaledDeltaTime * 3.2f;
                    m_Refs.feverButton.transform.localScale = Vector3.one * (1f + Mathf.Sin(m_FeverPulse) * 0.07f);
                }
            }
        }

        public void OnFeverPressed()
        {
            ServiceLocator.Get<AudioService>()?.PlayUi();
            GameManager.Instance?.RequestFever();
            if (m_Refs.feverButton != null) m_Refs.feverButton.SetActive(false);
        }
    }
}
