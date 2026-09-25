using UnityEngine;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Store;

namespace VoidMart.UI
{
    /// <summary>World-space price tag floating over an unbought furnishing pad.</summary>
    public class ZoneLabel : MonoBehaviour
    {
        [SerializeField] VMText m_Title;
        [SerializeField] VMText m_Price;
        [SerializeField] Image m_Fill;
        [SerializeField] Transform m_Billboard;

        GameConfig m_Config;
        FurnishingNodeData m_Node;
        FurnishingZone m_Zone;
        Camera m_Camera;

        public void Bind(GameConfig config, FurnishingNodeData node, FurnishingZone zone)
        {
            m_Config = config;
            m_Node = node;
            m_Zone = zone;
            if (m_Title != null) m_Title.SetText(node != null ? node.displayName : "");
            Refresh();
        }

        public void BindVisuals(VMText title, VMText price, Image fill, Transform billboard)
        {
            m_Title = title;
            m_Price = price;
            m_Fill = fill;
            m_Billboard = billboard;
        }

        void LateUpdate()
        {
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Billboard != null && m_Camera != null)
                m_Billboard.rotation = Quaternion.LookRotation(m_Billboard.position - m_Camera.transform.position, Vector3.up);
            Refresh();
        }

        void Refresh()
        {
            if (m_Zone == null) return;
            double remaining = System.Math.Max(0d, m_Zone.Cost - m_Zone.Paid);
            if (m_Price != null) m_Price.SetText(NumberFormat.Money(remaining));
            if (m_Fill != null) m_Fill.fillAmount = m_Zone.Progress;
        }
    }
}
