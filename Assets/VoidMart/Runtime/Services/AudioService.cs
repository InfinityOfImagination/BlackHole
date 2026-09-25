using System.Collections.Generic;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Services
{
    /// <summary>
    /// Music crossfading between exploration and interior modes plus a voice-limited SFX pool.
    /// Implements the spec's dynamic pitch ladder: every successive swallow raises the pop by
    /// 0.03 semitones (capped at +12) and the ladder resets 1.5 s after the last bite.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
        public const string MusicStreet = "music_street";
        public const string MusicStore = "music_store";
        public const string MusicPuzzle = "music_puzzle";
        public const string SfxSuctionPop = "sfx_suction_pop";
        public const string SfxUnload = "sfx_unload";
        public const string SfxCash = "sfx_cash";
        public const string SfxUpgrade = "sfx_upgrade";
        public const string SfxJam = "sfx_jam";
        public const string SfxRepair = "sfx_repair";
        public const string SfxPlace = "sfx_puzzle_place";
        public const string SfxClear = "sfx_puzzle_clear";
        public const string SfxTap = "sfx_ui_tap";
        public const string SfxLevelUp = "sfx_level_up";
        public const string SfxPoof = "sfx_poof";
        public const string SfxSell = "sfx_sell";

        [SerializeField] GameConfig m_Config;
        [SerializeField] int m_SfxVoices = 24;

        AudioSource m_MusicA;
        AudioSource m_MusicB;
        AudioSource m_UnloadLoop;
        AudioSource[] m_Sfx;
        int m_NextVoice;

        bool m_UsingA = true;
        string m_CurrentMusicKey;
        float m_Fade = 1f;
        float m_UnloadTarget;

        int m_SwallowStreak;
        float m_LastSwallowTime = -10f;

        readonly Dictionary<string, int> m_ActiveVoices = new Dictionary<string, int>(8);
        readonly List<string> m_VoiceKeysByIndex = new List<string>(32);

        float MusicVolume
        {
            get
            {
                if (m_Config == null) return 0.6f;
                var save = ServiceLocator.Get<SaveService>();
                float user = save?.Data?.settings.musicVolume ?? 1f;
                return m_Config.audio.masterVolume * m_Config.audio.musicVolume * user;
            }
        }

        float SfxVolume
        {
            get
            {
                if (m_Config == null) return 0.9f;
                var save = ServiceLocator.Get<SaveService>();
                float user = save?.Data?.settings.sfxVolume ?? 1f;
                return m_Config.audio.masterVolume * m_Config.audio.sfxVolume * user;
            }
        }

        public void Configure(GameConfig config) => m_Config = config;

        void Awake()
        {
            ServiceLocator.Register(this);

            m_MusicA = CreateSource("Music A", true);
            m_MusicB = CreateSource("Music B", true);
            m_UnloadLoop = CreateSource("Unload Loop", true);
            m_UnloadLoop.volume = 0f;

            m_Sfx = new AudioSource[Mathf.Max(4, m_SfxVoices)];
            for (int i = 0; i < m_Sfx.Length; i++)
            {
                m_Sfx[i] = CreateSource("SFX " + i, false);
                m_VoiceKeysByIndex.Add(null);
            }
        }

        void OnDestroy()
        {
            if (ServiceLocator.Get<AudioService>() == this) ServiceLocator.Unregister<AudioService>();
        }

        AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }

        AudioClip Clip(string key)
        {
            var assets = m_Config != null ? m_Config.assets : null;
            return assets != null ? assets.GetClip(key) : null;
        }

        // ----------------------------------------------------------------- music

        public void PlayMusic(string key)
        {
            if (key == m_CurrentMusicKey) return;
            var clip = Clip(key);
            if (clip == null) return;

            var next = m_UsingA ? m_MusicB : m_MusicA;
            next.clip = clip;
            next.volume = 0f;
            next.time = 0f;
            next.Play();

            m_UsingA = !m_UsingA;
            m_CurrentMusicKey = key;
            m_Fade = 0f;
        }

        public void PlayMusicForArea(WorldArea area) => PlayMusic(area == WorldArea.Store ? MusicStore : MusicStreet);

        void Update()
        {
            float crossfade = m_Config != null ? Mathf.Max(0.05f, m_Config.audio.musicCrossfadeSeconds) : 1.5f;
            if (m_Fade < 1f)
            {
                m_Fade = Mathf.Min(1f, m_Fade + Time.unscaledDeltaTime / crossfade);
                var incoming = m_UsingA ? m_MusicA : m_MusicB;
                var outgoing = m_UsingA ? m_MusicB : m_MusicA;
                float target = MusicVolume;
                incoming.volume = target * m_Fade;
                outgoing.volume = target * (1f - m_Fade);
                if (m_Fade >= 1f && outgoing.isPlaying) outgoing.Stop();
            }
            else
            {
                var current = m_UsingA ? m_MusicA : m_MusicB;
                current.volume = MusicVolume;
            }

            // Unload loop volume follows the actual transfer rate (spec 3).
            float unloadVolume = Mathf.Clamp01(m_UnloadTarget) * SfxVolume;
            m_UnloadLoop.volume = Easing.Damp(m_UnloadLoop.volume, unloadVolume, 10f, Time.unscaledDeltaTime);
            if (m_UnloadLoop.volume > 0.005f)
            {
                if (!m_UnloadLoop.isPlaying)
                {
                    m_UnloadLoop.clip = Clip(SfxUnload);
                    if (m_UnloadLoop.clip != null) m_UnloadLoop.Play();
                }
                m_UnloadLoop.pitch = 0.9f + 0.35f * Mathf.Clamp01(m_UnloadTarget);
            }
            else if (m_UnloadLoop.isPlaying)
            {
                m_UnloadLoop.Stop();
            }
            m_UnloadTarget = Mathf.MoveTowards(m_UnloadTarget, 0f, Time.unscaledDeltaTime * 4f);

            // Pitch ladder decay.
            if (m_SwallowStreak > 0 && Time.time - m_LastSwallowTime > ResetSeconds) m_SwallowStreak = 0;

            ReleaseFinishedVoices();
        }

        float ResetSeconds => m_Config != null ? m_Config.audio.pitchResetSeconds : 1.5f;

        // ------------------------------------------------------------------- sfx

        void ReleaseFinishedVoices()
        {
            for (int i = 0; i < m_Sfx.Length; i++)
            {
                string key = m_VoiceKeysByIndex[i];
                if (key == null || m_Sfx[i].isPlaying) continue;
                m_VoiceKeysByIndex[i] = null;
                if (m_ActiveVoices.TryGetValue(key, out int count))
                    m_ActiveVoices[key] = Mathf.Max(0, count - 1);
            }
        }

        public void PlaySfx(string key, float pitch = 1f, float volumeScale = 1f, int maxVoices = 0)
        {
            var clip = Clip(key);
            if (clip == null || m_Sfx == null) return;

            if (maxVoices > 0)
            {
                m_ActiveVoices.TryGetValue(key, out int active);
                if (active >= maxVoices) return;
                m_ActiveVoices[key] = active + 1;
            }

            int index = FindFreeVoice();
            var source = m_Sfx[index];
            source.clip = clip;
            source.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
            source.volume = SfxVolume * Mathf.Clamp01(volumeScale);
            source.spatialBlend = 0f;
            source.Play();
            m_VoiceKeysByIndex[index] = maxVoices > 0 ? key : null;
        }

        int FindFreeVoice()
        {
            for (int i = 0; i < m_Sfx.Length; i++)
            {
                int index = (m_NextVoice + i) % m_Sfx.Length;
                if (!m_Sfx[index].isPlaying)
                {
                    m_NextVoice = (index + 1) % m_Sfx.Length;
                    return index;
                }
            }
            int fallback = m_NextVoice;
            m_NextVoice = (m_NextVoice + 1) % m_Sfx.Length;
            return fallback;
        }

        /// <summary>Crisp rubber "plop" with the rising streak pitch and ±5% random variance.</summary>
        public void PlaySwallowPop()
        {
            var audio = m_Config != null ? m_Config.audio : null;
            float step = audio?.pitchStepSemitones ?? 0.03f;
            float cap = audio?.pitchCapSemitones ?? 12f;
            float variance = audio?.suctionPitchVariance ?? 0.05f;

            if (Time.time - m_LastSwallowTime > ResetSeconds) m_SwallowStreak = 0;
            m_LastSwallowTime = Time.time;
            m_SwallowStreak++;

            float semitones = Mathf.Min(cap, step * m_SwallowStreak);
            float pitch = Mathf.Pow(2f, semitones / 12f) * (1f + Random.Range(-variance, variance));
            PlaySfx(SfxSuctionPop, pitch, 0.85f);
        }

        public int SwallowStreak => m_SwallowStreak;

        public void SetUnloadRate(float normalized) => m_UnloadTarget = Mathf.Clamp01(normalized);

        public void PlayCash()
        {
            int maxVoices = m_Config != null ? m_Config.audio.cashMaxVoices : 6;
            PlaySfx(SfxCash, Random.Range(0.94f, 1.08f), 0.8f, maxVoices);
        }

        public void PlayUi() => PlaySfx(SfxTap, 1f, 0.6f);

        public void StopAll()
        {
            if (m_Sfx == null) return;
            for (int i = 0; i < m_Sfx.Length; i++) m_Sfx[i].Stop();
            m_UnloadLoop.Stop();
            m_MusicA.Stop();
            m_MusicB.Stop();
            m_CurrentMusicKey = null;
        }
    }
}
