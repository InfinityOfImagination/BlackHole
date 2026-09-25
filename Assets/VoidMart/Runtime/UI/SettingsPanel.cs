using UnityEngine;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Monetization;
using VoidMart.Services;

namespace VoidMart.UI
{
    /// <summary>Volume, haptics, purchases and the debug reset — fades in on EaseInOutQuad.</summary>
    public class SettingsPanel : ModalPanel
    {
        [SerializeField] Slider m_Music;
        [SerializeField] Slider m_Sfx;
        [SerializeField] Toggle m_Haptics;
        [SerializeField] Button m_NoAds;
        [SerializeField] Button m_Restore;
        [SerializeField] Button m_Close;
        [SerializeField] Button m_ResetSave;
        [SerializeField] VMText m_Version;

        public void BindControls(Slider music, Slider sfx, Toggle haptics, Button noAds, Button restore, Button close, Button resetSave, VMText version)
        {
            m_Music = music;
            m_Sfx = sfx;
            m_Haptics = haptics;
            m_NoAds = noAds;
            m_Restore = restore;
            m_Close = close;
            m_ResetSave = resetSave;
            m_Version = version;
        }

        protected override void Awake()
        {
            base.Awake();
            if (m_Close != null) { m_Close.onClick.RemoveAllListeners(); m_Close.onClick.AddListener(Close); }
            if (m_NoAds != null) { m_NoAds.onClick.RemoveAllListeners(); m_NoAds.onClick.AddListener(BuyNoAds); }
            if (m_Restore != null) { m_Restore.onClick.RemoveAllListeners(); m_Restore.onClick.AddListener(Restore); }
            if (m_ResetSave != null) { m_ResetSave.onClick.RemoveAllListeners(); m_ResetSave.onClick.AddListener(ResetSave); }
            if (m_Music != null) m_Music.onValueChanged.AddListener(OnMusic);
            if (m_Sfx != null) m_Sfx.onValueChanged.AddListener(OnSfx);
            if (m_Haptics != null) m_Haptics.onValueChanged.AddListener(OnHaptics);
        }

        protected override void OnOpening()
        {
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data != null)
            {
                if (m_Music != null) m_Music.SetValueWithoutNotify(save.Data.settings.musicVolume);
                if (m_Sfx != null) m_Sfx.SetValueWithoutNotify(save.Data.settings.sfxVolume);
                if (m_Haptics != null) m_Haptics.SetIsOnWithoutNotify(save.Data.settings.hapticsEnabled);
                if (m_NoAds != null) m_NoAds.interactable = !save.Data.profile.noAdsActive;
            }
            if (m_Version != null && m_Config != null) m_Version.SetText("v" + m_Config.buildVersion);
        }

        void OnMusic(float value)
        {
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data == null) return;
            save.Data.settings.musicVolume = value;
            save.MarkDirty();
        }

        void OnSfx(float value)
        {
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data == null) return;
            save.Data.settings.sfxVolume = value;
            save.MarkDirty();
            ServiceLocator.Get<AudioService>()?.PlayUi();
        }

        void OnHaptics(bool value)
        {
            var save = ServiceLocator.Get<SaveService>();
            if (save?.Data == null) return;
            save.Data.settings.hapticsEnabled = value;
            save.MarkDirty();
            Haptics.Enabled = value;
        }

        void BuyNoAds()
        {
            var iap = ServiceLocator.Get<IIapService>();
            if (iap == null || m_Config == null) return;
            iap.Purchase(m_Config.monetization.iapNoAdsId, success =>
            {
                if (success && m_NoAds != null) m_NoAds.interactable = false;
            });
        }

        void Restore()
        {
            ServiceLocator.Get<IIapService>()?.Restore(null);
        }

        void ResetSave()
        {
            var save = ServiceLocator.Get<SaveService>();
            save?.ResetProfile();
            Close();
        }
    }
}
