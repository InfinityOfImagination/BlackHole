using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoidMart.Data;
using VoidMart.Gameplay;

namespace VoidMart.UI
{
    /// <summary>
    /// Floating one-finger joystick.  Driven entirely through the uGUI event system so it behaves
    /// identically under the legacy input manager and the Input System package.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] RectTransform m_Base;
        [SerializeField] RectTransform m_Knob;
        [SerializeField] CanvasGroup m_Group;
        [SerializeField] Graphic m_Raycast;

        RectTransform m_Self;
        Canvas m_Canvas;
        Vector2 m_Origin;
        bool m_Active;
        float m_Alpha;

        public Vector2 Value { get; private set; }

        public void Configure(GameConfig config, RectTransform baseRect, RectTransform knob, CanvasGroup group)
        {
            m_Config = config;
            m_Base = baseRect;
            m_Knob = knob;
            m_Group = group;
        }

        void Awake()
        {
            m_Self = transform as RectTransform;
            m_Canvas = GetComponentInParent<Canvas>();
            if (m_Group == null) m_Group = GetComponent<CanvasGroup>();
        }

        void OnDisable()
        {
            Value = Vector2.zero;
            PlayerInputRouter.Clear();
            m_Active = false;
        }

        UIConfig Ui => m_Config != null ? m_Config.ui : s_Fallback;
        static readonly UIConfig s_Fallback = new UIConfig();

        public void OnPointerDown(PointerEventData eventData)
        {
            m_Active = true;
            if (Ui.floatingJoystick && m_Base != null)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Self, eventData.position, EventCamera(eventData), out var local))
                {
                    m_Base.anchoredPosition = local;
                }
            }
            m_Origin = m_Base != null ? m_Base.anchoredPosition : Vector2.zero;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_Active) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(m_Self, eventData.position, EventCamera(eventData), out var local)) return;

            Vector2 delta = local - m_Origin;
            float radius = Mathf.Max(1f, Ui.joystickRadius);
            Vector2 clamped = Vector2.ClampMagnitude(delta, radius);
            if (m_Knob != null) m_Knob.anchoredPosition = m_Origin + clamped;

            Vector2 axis = clamped / radius;
            if (axis.magnitude < Ui.joystickDeadZone) axis = Vector2.zero;
            Value = axis;
            PlayerInputRouter.SetMove(axis);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_Active = false;
            Value = Vector2.zero;
            PlayerInputRouter.Clear();
            if (m_Knob != null && m_Base != null) m_Knob.anchoredPosition = m_Base.anchoredPosition;
        }

        Camera EventCamera(PointerEventData eventData)
        {
            if (eventData.pressEventCamera != null) return eventData.pressEventCamera;
            if (m_Canvas == null) return null;
            return m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_Canvas.worldCamera;
        }

        void Update()
        {
            float target = m_Active ? Ui.joystickOpacity : Ui.joystickOpacity * 0.45f;
            m_Alpha = Mathf.MoveTowards(m_Alpha, target, Time.unscaledDeltaTime * 4f);
            if (m_Group != null) m_Group.alpha = m_Alpha;
        }
    }
}
