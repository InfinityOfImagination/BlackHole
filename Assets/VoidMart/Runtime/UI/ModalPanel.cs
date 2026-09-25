using System.Collections;
using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.UI
{
    public enum ModalTransition { SlideUp, ScalePop, Fade }

    /// <summary>
    /// Base class for every overlay: slide-up uses EaseOutBack, pop-in uses EaseOutElastic and
    /// fades use EaseInOutQuad, matching the transition table in the spec.
    /// </summary>
    public class ModalPanel : MonoBehaviour
    {
        [SerializeField] protected GameConfig m_Config;
        [SerializeField] protected RectTransform m_Panel;
        [SerializeField] protected CanvasGroup m_Group;
        [SerializeField] protected ModalTransition m_Transition = ModalTransition.SlideUp;
        [SerializeField] protected GameObject m_Blocker;

        Coroutine m_Routine;
        Vector2 m_ShownPosition;
        bool m_Captured;

        public bool IsOpen { get; private set; }

        public void BindModal(GameConfig config, RectTransform panel, CanvasGroup group, ModalTransition transition, GameObject blocker)
        {
            m_Config = config;
            m_Panel = panel;
            m_Group = group;
            m_Transition = transition;
            m_Blocker = blocker;
        }

        protected virtual void Awake()
        {
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            if (m_Panel == null) m_Panel = transform as RectTransform;
            if (m_Group == null) m_Group = GetComponent<CanvasGroup>();
            CapturePose();
            ApplyClosedState();
        }

        void CapturePose()
        {
            if (m_Captured || m_Panel == null) return;
            m_ShownPosition = m_Panel.anchoredPosition;
            m_Captured = true;
        }

        void ApplyClosedState()
        {
            if (m_Group != null)
            {
                m_Group.alpha = 0f;
                m_Group.interactable = false;
                m_Group.blocksRaycasts = false;
            }
            if (m_Blocker != null) m_Blocker.SetActive(false);
            gameObject.SetActive(false);
        }

        public virtual void Open()
        {
            if (IsOpen) return;
            CapturePose();
            IsOpen = true;
            gameObject.SetActive(true);
            if (m_Blocker != null) m_Blocker.SetActive(true);
            if (m_Group != null)
            {
                m_Group.interactable = true;
                m_Group.blocksRaycasts = true;
            }

            ServiceLocator.Get<AudioService>()?.PlayUi();
            OnOpening();

            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(PlayOpen());
        }

        public virtual void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (m_Group != null)
            {
                m_Group.interactable = false;
                m_Group.blocksRaycasts = false;
            }
            ServiceLocator.Get<AudioService>()?.PlayUi();

            if (m_Routine != null) StopCoroutine(m_Routine);
            m_Routine = StartCoroutine(PlayClose());
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        protected virtual void OnOpening() { }
        protected virtual void OnClosed() { }

        UIConfig Ui => m_Config != null ? m_Config.ui : s_Fallback;
        static readonly UIConfig s_Fallback = new UIConfig();

        IEnumerator PlayOpen()
        {
            switch (m_Transition)
            {
                case ModalTransition.SlideUp:
                    if (m_Group != null) m_Group.alpha = 1f;
                    yield return UITween.Move(m_Panel, m_ShownPosition + new Vector2(0f, -1400f), m_ShownPosition, Ui.modalSlideSeconds, Ease.OutBack);
                    break;
                case ModalTransition.ScalePop:
                    if (m_Group != null) m_Group.alpha = 1f;
                    yield return UITween.Scale(m_Panel, 0.4f, 1f, Ui.modalPopSeconds, Ease.OutElastic);
                    break;
                default:
                    yield return UITween.Fade(m_Group, 0f, 1f, Ui.fadeSeconds, Ease.InOutQuad);
                    break;
            }
            m_Routine = null;
        }

        IEnumerator PlayClose()
        {
            switch (m_Transition)
            {
                case ModalTransition.SlideUp:
                    yield return UITween.Move(m_Panel, m_ShownPosition, m_ShownPosition + new Vector2(0f, -1400f), Ui.modalSlideSeconds * 0.8f, Ease.InOutQuad);
                    break;
                case ModalTransition.ScalePop:
                    yield return UITween.Scale(m_Panel, 1f, 0.3f, Ui.modalPopSeconds * 0.55f, Ease.InOutQuad);
                    break;
                default:
                    yield return UITween.Fade(m_Group, 1f, 0f, Ui.fadeSeconds, Ease.InOutQuad);
                    break;
            }

            if (m_Group != null) m_Group.alpha = 0f;
            if (m_Blocker != null) m_Blocker.SetActive(false);
            if (m_Panel != null) m_Panel.anchoredPosition = m_ShownPosition;
            gameObject.SetActive(false);
            OnClosed();
            m_Routine = null;
        }
    }
}
