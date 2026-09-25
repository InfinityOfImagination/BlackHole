using UnityEngine;
using VoidMart.Core;
using VoidMart.Gameplay;

namespace VoidMart.Store
{
    /// <summary>
    /// Switches the game between street and storefront mode (music, HUD context, state machine)
    /// based on where the hole physically is - no loading screens, no doors to tap.
    /// </summary>
    public class AreaVolume : MonoBehaviour
    {
        [SerializeField] Vector3 m_Size = new Vector3(40f, 8f, 30f);
        [SerializeField] WorldArea m_InsideArea = WorldArea.Store;
        [SerializeField] WorldArea m_OutsideArea = WorldArea.Street;

        PlayerHoleController m_Hole;
        bool m_Inside;
        bool m_Initialised;

        public void Configure(Vector3 size, WorldArea inside, WorldArea outside)
        {
            m_Size = size;
            m_InsideArea = inside;
            m_OutsideArea = outside;
        }

        void Update()
        {
            if (m_Hole == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }

            Vector3 local = transform.InverseTransformPoint(m_Hole.transform.position);
            bool inside = Mathf.Abs(local.x) <= m_Size.x * 0.5f && Mathf.Abs(local.z) <= m_Size.z * 0.5f;
            if (m_Initialised && inside == m_Inside) return;

            m_Initialised = true;
            m_Inside = inside;
            GameManager.Instance?.SetArea(inside ? m_InsideArea : m_OutsideArea);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.69f, 0.13f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, m_Size);
        }
    }
}
