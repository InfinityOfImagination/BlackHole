using System.IO;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Services
{
    /// <summary>
    /// Owns the in-memory <see cref="SaveData"/>, autosaves on the spec's cadence (suspension,
    /// purchases, and every 60 seconds) and reports how long the player was away so the idle
    /// layer can pay out offline profit.
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;

        public SaveData Data { get; private set; }
        public double PendingOfflineSeconds { get; private set; }
        public bool LoadedFromDisk { get; private set; }
        public string FilePath => Path.Combine(Application.persistentDataPath, m_Config != null ? m_Config.persistence.fileName : "voidmart.sav");

        float m_Timer;
        bool m_Dirty;

        public void Configure(GameConfig config) => m_Config = config;

        void Awake()
        {
            ServiceLocator.Register(this);
            Load();
        }

        void OnDestroy()
        {
            if (ServiceLocator.Get<SaveService>() == this) ServiceLocator.Unregister<SaveService>();
        }

        void Start()
        {
            var bus = m_Config != null ? m_Config.events : null;
            if (bus != null && bus.saveRequested != null) bus.saveRequested.Register(SaveNow);
        }

        void OnDisable()
        {
            var bus = m_Config != null ? m_Config.events : null;
            if (bus != null && bus.saveRequested != null) bus.saveRequested.Unregister(SaveNow);
        }

        public void Load()
        {
            string json = null;
            if (m_Config == null || m_Config.persistence.encryptSaves)
                json = SecureSaveEngine.LoadEncrypted(FilePath);
            else if (File.Exists(FilePath))
                json = File.ReadAllText(FilePath);

            SaveData loaded = json != null ? SaveData.FromJson(json) : null;
            LoadedFromDisk = loaded != null;

            if (loaded == null)
            {
                Data = SaveData.CreateDefault(m_Config);
                PendingOfflineSeconds = 0d;
            }
            else
            {
                Data = loaded.Validate(m_Config);
                PendingOfflineSeconds = Data.SecondsSinceSaved();
            }

            Data.meta.appLaunches++;
            m_Timer = 0f;
            m_Dirty = true;
        }

        public void MarkDirty() => m_Dirty = true;

        public void ConsumeOfflineTime() => PendingOfflineSeconds = 0d;

        public void SaveNow()
        {
            if (Data == null) return;
            Data.timestamp = SaveData.Now();
            string json = Data.ToJson(false);

            if (m_Config == null || m_Config.persistence.encryptSaves)
                SecureSaveEngine.SaveEncrypted(FilePath, json);
            else
                File.WriteAllText(FilePath, json);

#if UNITY_EDITOR
            if (m_Config != null && m_Config.persistence.writePlainTextMirrorInEditor)
                SecureSaveEngine.WriteDebugMirror(FilePath + ".debug.json", Data.ToJson(true));
#endif
            m_Dirty = false;
            m_Timer = 0f;
        }

        void Update()
        {
            float interval = m_Config != null ? m_Config.persistence.autosaveSeconds : 60f;
            m_Timer += Time.unscaledDeltaTime;
            if (m_Timer < interval) return;
            if (m_Dirty) SaveNow();
            else m_Timer = 0f;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) SaveNow();
        }

        void OnApplicationQuit() => SaveNow();

        /// <summary>Wipes the profile (used by the Master Tool's debug tab).</summary>
        public void ResetProfile()
        {
            Data = SaveData.CreateDefault(m_Config);
            PendingOfflineSeconds = 0d;
            SaveNow();
            var bus = m_Config != null ? m_Config.events : null;
            if (bus != null && bus.dataReloaded != null) bus.dataReloaded.Raise();
        }

        public void DeleteSaveFile()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                string mirror = FilePath + ".debug.json";
                if (File.Exists(mirror)) File.Delete(mirror);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[VoidMart] Could not delete save: " + e.Message);
            }
        }
    }
}
