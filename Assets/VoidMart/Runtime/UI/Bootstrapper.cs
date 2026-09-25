using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Monetization;

namespace VoidMart.UI
{
    /// <summary>
    /// Boot scene → splash / silent auth → core game loop, exactly the screen flow in the spec.
    /// The splash doubles as the cover for the async scene load so the hand-off is seamless.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] string m_GameSceneName = "Game";
        [SerializeField] CanvasGroup m_SplashGroup;
        [SerializeField] RectTransform m_Logo;
        [SerializeField] VMText m_StatusLabel;
        [SerializeField] Image m_ProgressFill;
        [SerializeField] float m_MinimumSplashSeconds = 1.25f;

        public void Bind(GameConfig config, CanvasGroup splash, RectTransform logo, VMText status, Image progress)
        {
            m_Config = config;
            m_SplashGroup = splash;
            m_Logo = logo;
            m_StatusLabel = status;
            m_ProgressFill = progress;
        }

        IEnumerator Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            GameManagerSetState(GameState.Splash);

            if (m_SplashGroup != null) m_SplashGroup.alpha = 0f;
            if (m_StatusLabel != null) m_StatusLabel.SetText("WAKING THE VOID");

            yield return UITween.Fade(m_SplashGroup, 0f, 1f, 0.35f, Ease.InOutQuad);
            if (m_Logo != null) StartCoroutine(UITween.Scale(m_Logo, 0.55f, 1f, 0.7f, Ease.OutElastic));

            bool authDone = false;
            var platform = ServiceLocator.Get<IPlatformService>();
            if (platform != null) platform.Authenticate(_ => authDone = true);
            else authDone = true;

            float elapsed = 0f;
            var load = SceneManager.LoadSceneAsync(m_GameSceneName, LoadSceneMode.Single);
            if (load == null)
            {
                Debug.LogError($"[VoidMart] Scene '{m_GameSceneName}' is missing from Build Settings. Run the one-click setup.");
                yield break;
            }
            load.allowSceneActivation = false;

            while (elapsed < m_MinimumSplashSeconds || !authDone || load.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (m_ProgressFill != null)
                    m_ProgressFill.fillAmount = Mathf.Clamp01(Mathf.Max(load.progress / 0.9f, elapsed / Mathf.Max(0.01f, m_MinimumSplashSeconds)));
                yield return null;
            }

            if (m_ProgressFill != null) m_ProgressFill.fillAmount = 1f;
            if (m_StatusLabel != null) m_StatusLabel.SetText("OPENING THE STORE");
            yield return UITween.Fade(m_SplashGroup, 1f, 0f, 0.3f, Ease.InOutQuad);

            load.allowSceneActivation = true;
        }

        static void GameManagerSetState(GameState state) => GameManager.Instance?.SetState(state);
    }
}
