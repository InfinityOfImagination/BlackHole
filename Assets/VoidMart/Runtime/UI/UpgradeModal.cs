using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.UI
{
    /// <summary>Store upgrade modal — slides up on EaseOutBack, rebuilt from the config table.</summary>
    public class UpgradeModal : ModalPanel
    {
        [SerializeField] RectTransform m_Content;
        [SerializeField] GameObject m_RowTemplate;
        [SerializeField] VMText m_Header;
        [SerializeField] Button m_CloseButton;

        readonly List<UpgradeRow> m_Rows = new List<UpgradeRow>(12);
        EconomyService m_Economy;
        float m_RefreshTimer;

        public void BindContent(RectTransform content, GameObject rowTemplate, VMText header, Button closeButton)
        {
            m_Content = content;
            m_RowTemplate = rowTemplate;
            m_Header = header;
            m_CloseButton = closeButton;
        }

        protected override void Awake()
        {
            base.Awake();
            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.RemoveAllListeners();
                m_CloseButton.onClick.AddListener(Close);
            }
        }

        protected override void OnOpening()
        {
            m_Economy = ServiceLocator.Get<EconomyService>();
            if (m_Header != null) m_Header.SetText("UPGRADES");
            BuildRows();
        }

        void BuildRows()
        {
            if (m_Config == null || m_Content == null || m_RowTemplate == null) return;

            int index = 0;
            for (; index < m_Config.upgrades.Count; index++)
            {
                var definition = m_Config.upgrades[index];
                if (definition == null) continue;

                UpgradeRow row;
                if (index < m_Rows.Count)
                {
                    row = m_Rows[index];
                }
                else
                {
                    var instance = Instantiate(m_RowTemplate, m_Content);
                    instance.SetActive(true);
                    row = instance.GetComponent<UpgradeRow>();
                    if (row == null) continue;
                    m_Rows.Add(row);
                }
                row.gameObject.SetActive(true);
                row.Bind(m_Config, definition, m_Economy);
            }

            for (int i = index; i < m_Rows.Count; i++) m_Rows[i].gameObject.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen) return;
            m_RefreshTimer -= Time.unscaledDeltaTime;
            if (m_RefreshTimer > 0f) return;
            m_RefreshTimer = 0.25f;
            for (int i = 0; i < m_Rows.Count; i++)
                if (m_Rows[i].gameObject.activeSelf) m_Rows[i].Refresh();
        }
    }
}
