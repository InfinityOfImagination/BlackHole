using UnityEngine;

namespace VoidMart.UI
{
    /// <summary>Keeps HUD corners clear of notches and gesture bars.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform m_Rect;
        Rect m_LastSafeArea;
        Vector2Int m_LastResolution;

        void Awake()
        {
            m_Rect = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea == m_LastSafeArea &&
                m_LastResolution.x == Screen.width &&
                m_LastResolution.y == Screen.height) return;
            Apply();
        }

        void Apply()
        {
            if (m_Rect == null) return;
            var safeArea = Screen.safeArea;
            m_LastSafeArea = safeArea;
            m_LastResolution = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safeArea.position;
            Vector2 max = safeArea.position + safeArea.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            m_Rect.anchorMin = min;
            m_Rect.anchorMax = max;
            m_Rect.offsetMin = Vector2.zero;
            m_Rect.offsetMax = Vector2.zero;
        }
    }
}
