using UnityEngine;
using VoidMart.Core;
using VoidMart.Store;

namespace VoidMart.Gameplay
{
    /// <summary>
    /// A floating arrow that appears once the hole is worth emptying and points the way back to
    /// the shop.  Without it a new player can wander the district with a full hole and no idea
    /// where the loop closes.
    /// </summary>
    public class NavigationHint : MonoBehaviour
    {
        [SerializeField] Transform m_Arrow;
        [SerializeField] float m_ShowAboveFill = 0.55f;
        [SerializeField] float m_Distance = 2.6f;
        [SerializeField] float m_Height = 0.6f;
        [SerializeField] float m_MinRange = 12f;

        PlayerHoleController m_Hole;
        StoreManager m_Store;
        float m_Visible;

        public void BindArrow(Transform arrow) => m_Arrow = arrow;

        void Start()
        {
            m_Hole = ServiceLocator.Get<PlayerHoleController>();
            if (m_Arrow != null) m_Arrow.gameObject.SetActive(false);
        }

        void Update()
        {
            if (m_Arrow == null) return;
            if (m_Hole == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }
            if (m_Store == null)
            {
                m_Store = ServiceLocator.Get<StoreManager>();
                if (m_Store == null) return;
            }

            Vector3 offset = m_Store.transform.position - m_Hole.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            bool wanted = m_Hole.FillNormalized >= m_ShowAboveFill && distance > m_MinRange;
            m_Visible = Mathf.MoveTowards(m_Visible, wanted ? 1f : 0f, Time.deltaTime * 4f);

            bool active = m_Visible > 0.01f;
            if (m_Arrow.gameObject.activeSelf != active) m_Arrow.gameObject.SetActive(active);
            if (!active) return;

            Vector3 direction = distance > 0.01f ? offset / distance : Vector3.forward;
            float radius = Mathf.Max(1f, m_Hole.VisualRadius);
            m_Arrow.position = m_Hole.transform.position + direction * (radius + m_Distance) + Vector3.up * m_Height;
            m_Arrow.rotation = Quaternion.LookRotation(direction, Vector3.up);

            float bob = 1f + Mathf.Sin(Time.time * 5f) * 0.12f;
            m_Arrow.localScale = Vector3.one * (m_Visible * 3.2f * bob);
        }
    }
}
