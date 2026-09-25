using UnityEngine;
using VoidMart.Core;

namespace VoidMart.Gameplay
{
    /// <summary>
    /// Stickman pedestrians that amble around the district and panic-run when the hole gets close.
    /// Pure transform work with a cheap sine bob, no navmesh, no allocations.
    /// </summary>
    public class Pedestrian : MonoBehaviour, IPoolable
    {
        [SerializeField] Transform m_Visual;
        [SerializeField] float m_WalkSpeed = 1.5f;
        [SerializeField] float m_RunSpeed = 4.4f;
        [SerializeField] float m_PanicRadius = 9f;
        [SerializeField] float m_WanderRadius = 7f;
        [SerializeField] float m_BobHeight = 0.09f;
        [SerializeField] float m_BobSpeed = 9f;

        Vector3 m_Home;
        Vector3 m_Target;
        float m_Repath;
        float m_Phase;
        PlayerHoleController m_Hole;

        void Awake()
        {
            if (m_Visual == null) m_Visual = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        public void OnSpawned()
        {
            m_Home = transform.position;
            m_Target = m_Home;
            m_Repath = 0f;
            m_Phase = Random.Range(0f, 10f);
        }

        public void OnDespawned() { }

        void Update()
        {
            if (m_Hole == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole == null) return;
            }

            float deltaTime = Time.deltaTime;
            Vector3 position = transform.position;
            Vector3 toHole = m_Hole.transform.position - position;
            toHole.y = 0f;
            float holeDistance = toHole.magnitude;

            float speed;
            Vector3 direction;

            if (holeDistance < m_PanicRadius)
            {
                direction = -toHole.normalized;
                speed = m_RunSpeed;
                m_Repath = 0f;
            }
            else
            {
                m_Repath -= deltaTime;
                if (m_Repath <= 0f)
                {
                    Vector2 offset = Random.insideUnitCircle * m_WanderRadius;
                    m_Target = m_Home + new Vector3(offset.x, 0f, offset.y);
                    m_Repath = Random.Range(1.8f, 4.5f);
                }
                Vector3 toTarget = m_Target - position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.09f) { m_Repath = 0f; return; }
                direction = toTarget.normalized;
                speed = m_WalkSpeed;
            }

            transform.position = position + direction * (speed * deltaTime);
            var look = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 540f * deltaTime);

            if (m_Visual != null)
            {
                m_Phase += deltaTime * m_BobSpeed * (speed / Mathf.Max(0.1f, m_WalkSpeed));
                var local = m_Visual.localPosition;
                local.y = Mathf.Abs(Mathf.Sin(m_Phase)) * m_BobHeight;
                m_Visual.localPosition = local;
                m_Visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(m_Phase * 0.5f) * 5f);
            }
        }
    }
}
