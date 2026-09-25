using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.UI
{
    /// <summary>Single-line banner that slides in for unlocks, jams and rewards.</summary>
    public class ToastView : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] RectTransform m_Panel;
        [SerializeField] Image m_Background;
        [SerializeField] VMText m_Label;
        [SerializeField] CanvasGroup m_Group;

        Coroutine m_Routine;
        Vector2 m_HiddenPosition;
        Vector2 m_ShownPosition;

        public void Bind(GameConfig config, RectTransform panel, Image background, VMText label, CanvasGroup group)
        {
            m_Config = config;
            m_Panel = panel;
            m_Background = background;
            m_Label = label;
            m_Group = group;
        }

        void Start()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            if (m_Panel != null)
            {
                m_ShownPosition = m_Panel.anchoredPosition;
                m_HiddenPosition = m_ShownPosition + new Vector2(0f, 160f);
                m_Panel.anchoredPosition = m_HiddenPosition;
            }
            if (m_Group != null) m_Group.alpha = 0f;

            var bus = m_Config != null ? m_Config.events : null;
            bus?.toast?.Register(Show);
        }

        void OnDestroy()
        {
            var bus = m_Config != null ? m_Config.events : null;
            bus?.toast?.Unregister(Show);
        }

        public void Show(ToastRequest request)
        {
            if (m_Label == null || m_Panel == null) return;
            m_Label.SetText(request.message);
            if (m_Background != null) m_Background.color = ColorFor(request.style);

            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(Play());
        }

        Color ColorFor(ToastStyle style)
        {
            var theme = m_Config != null ? m_Config.theme : null;
            if (theme == null) return Color.white;
            switch (style)
            {
                case ToastStyle.Success: return theme.mint;
                case ToastStyle.Warning: return theme.coral;
                case ToastStyle.Reward: return theme.sunsetAmber;
                default: return theme.electricViolet;
            }
        }

        IEnumerator Play()
        {
            float slide = m_Config != null ? m_Config.ui.modalSlideSeconds : 0.4f;
            float hold = m_Config != null ? m_Config.ui.toastSeconds : 2.2f;

            if (m_Group != null) m_Group.alpha = 1f;
            yield return UITween.Move(m_Panel, m_HiddenPosition, m_ShownPosition, slide, Ease.OutBack);
            yield return new WaitForSecondsRealtime(hold);
            yield return UITween.Move(m_Panel, m_ShownPosition, m_HiddenPosition, slide * 0.7f, Ease.InOutQuad);
            if (m_Group != null) m_Group.alpha = 0f;
            m_Routine = null;
        }
    }
}
