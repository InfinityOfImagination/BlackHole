using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Tweaker;

namespace VoidMart.Gameplay
{
    /// <summary>
    /// Fixed isometric-style tilt from the art direction: a low-FOV perspective camera angled
    /// downward, pulling back smoothly as the hole grows so the player always reads their scale.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] GameConfig m_Config;
        [SerializeField] Camera m_Camera;
        [SerializeField] Transform m_Target;

        float m_Distance;
        Vector3 m_SmoothTarget;
        Vector3 m_ShakeOffset;
        float m_ShakeAmount;
        PlayerHoleController m_Hole;

        public Camera Camera => m_Camera;

        public void Configure(GameConfig config, Camera camera, Transform target)
        {
            m_Config = config;
            m_Camera = camera;
            m_Target = target;
        }

        void Awake()
        {
            if (m_Camera == null) m_Camera = GetComponentInChildren<Camera>();
            if (m_Config == null && ServiceInstaller.Active != null) m_Config = ServiceInstaller.Active.Config;
            ServiceLocator.Register(this);
        }

        void OnDestroy()
        {
            if (ServiceLocator.Get<CameraRig>() == this) ServiceLocator.Unregister<CameraRig>();
        }

        void Start()
        {
            m_Hole = ServiceLocator.Get<PlayerHoleController>();
            if (m_Target == null && m_Hole != null) m_Target = m_Hole.transform;
            m_SmoothTarget = m_Target != null ? m_Target.position : Vector3.zero;
            m_Distance = Cfg.baseDistance;
            ApplyImmediate();
        }

        CameraConfig Cfg => m_Config != null ? m_Config.cameraRig : s_Fallback;
        static readonly CameraConfig s_Fallback = new CameraConfig();

        void LateUpdate()
        {
            if (m_Target == null)
            {
                m_Hole = ServiceLocator.Get<PlayerHoleController>();
                if (m_Hole != null) m_Target = m_Hole.transform;
                if (m_Target == null) return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            var cfg = Cfg;

            Vector3 lead = Vector3.zero;
            if (m_Hole != null && cfg.lookAhead > 0f) lead = m_Hole.Velocity * cfg.lookAhead * 0.1f;

            Vector3 focus = m_Target.position + cfg.targetOffset + lead;
            m_SmoothTarget = Easing.Damp(m_SmoothTarget, focus, cfg.followSharpness, deltaTime);

            float radius = m_Hole != null ? m_Hole.VisualRadius : 1.2f;
            float baseRadius = m_Config != null ? m_Config.hole.initialRadius : 1.2f;
            float targetDistance = cfg.baseDistance + Mathf.Max(0f, radius - baseRadius) * cfg.distancePerRadius;
            m_Distance = Easing.Damp(m_Distance, targetDistance, cfg.zoomSharpness, deltaTime);

            if (m_ShakeAmount > 0.0001f)
            {
                m_ShakeAmount = Mathf.Max(0f, m_ShakeAmount - deltaTime * 2.6f);
                float magnitude = m_ShakeAmount * cfg.shakeScale;
                m_ShakeOffset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 26f, 0f) - 0.5f) * magnitude,
                    (Mathf.PerlinNoise(0f, Time.time * 26f) - 0.5f) * magnitude,
                    0f);
            }
            else
            {
                m_ShakeOffset = Vector3.zero;
            }

            Apply(cfg);
        }

        void Apply(CameraConfig cfg)
        {
            var rotation = Quaternion.Euler(cfg.pitch, cfg.yaw, 0f);
            Vector3 position = m_SmoothTarget - rotation * Vector3.forward * m_Distance;
            transform.SetPositionAndRotation(position, rotation);

            if (m_Camera != null)
            {
                m_Camera.fieldOfView = cfg.fieldOfView;
                m_Camera.transform.localPosition = m_ShakeOffset;
            }
        }

        void ApplyImmediate()
        {
            var cfg = Cfg;
            if (m_Target != null) m_SmoothTarget = m_Target.position + cfg.targetOffset;
            Apply(cfg);
        }

        public void Shake(float amount) => m_ShakeAmount = Mathf.Max(m_ShakeAmount, amount);
    }
}
