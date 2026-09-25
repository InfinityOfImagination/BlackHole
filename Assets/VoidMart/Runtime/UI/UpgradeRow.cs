using UnityEngine;
using UnityEngine.UI;
using VoidMart.Core;
using VoidMart.Data;
using VoidMart.Services;

namespace VoidMart.UI
{
    /// <summary>One purchasable upgrade line inside the store modal.</summary>
    public class UpgradeRow : MonoBehaviour
    {
        [SerializeField] Image m_Icon;
        [SerializeField] Image m_Background;
        [SerializeField] VMText m_Title;
        [SerializeField] VMText m_Detail;
        [SerializeField] VMText m_Cost;
        [SerializeField] Button m_Button;
        [SerializeField] Image m_LevelFill;

        UpgradeDefinition m_Definition;
        EconomyService m_Economy;
        GameConfig m_Config;

        public UpgradeDefinition Definition => m_Definition;

        public void BindVisuals(Image icon, Image background, VMText title, VMText detail, VMText cost, Button button, Image levelFill)
        {
            m_Icon = icon;
            m_Background = background;
            m_Title = title;
            m_Detail = detail;
            m_Cost = cost;
            m_Button = button;
            m_LevelFill = levelFill;
        }

        public void Bind(GameConfig config, UpgradeDefinition definition, EconomyService economy)
        {
            m_Config = config;
            m_Definition = definition;
            m_Economy = economy;

            if (m_Icon != null && config != null && config.assets != null)
            {
                var sprite = config.assets.GetSprite(definition.iconKey);
                if (sprite != null) m_Icon.sprite = sprite;
                m_Icon.color = definition.tint;
            }

            if (m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                m_Button.onClick.AddListener(Purchase);
            }
            Refresh();
        }

        public void Refresh()
        {
            if (m_Definition == null || m_Economy == null) return;

            int level = m_Economy.GetUpgradeLevel(m_Definition.id);
            bool maxed = level >= m_Definition.maxLevel;
            double cost = m_Economy.GetUpgradeCost(m_Definition);
            bool affordable = !maxed && m_Economy.CanAfford(cost);

            if (m_Title != null) m_Title.SetText(m_Definition.displayName);
            if (m_Detail != null)
            {
                string detail = string.IsNullOrEmpty(m_Definition.description)
                    ? DescribeEffect(m_Definition, level)
                    : m_Definition.description;
                m_Detail.SetText("LV " + level + "   " + detail);
            }
            if (m_Cost != null) m_Cost.SetText(maxed ? "MAX" : NumberFormat.Money(cost));
            if (m_LevelFill != null) m_LevelFill.fillAmount = m_Definition.maxLevel <= 0 ? 0f : (float)level / m_Definition.maxLevel;

            if (m_Button != null) m_Button.interactable = affordable;
            if (m_Background != null && m_Config != null)
            {
                var theme = m_Config.theme;
                m_Background.color = maxed ? theme.concreteB : affordable ? theme.electricViolet : theme.voidIndigo;
            }
        }

        static string DescribeEffect(UpgradeDefinition definition, int level)
        {
            float value = definition.ValueAt(level + 1) * 100f;
            switch (definition.effect)
            {
                case UpgradeEffect.RobotCount: return "+1 helper";
                case UpgradeEffect.HoleCapacity: return "+" + Mathf.RoundToInt(value) + "% capacity";
                case UpgradeEffect.HoleSpeed: return "+" + Mathf.RoundToInt(value) + "% speed";
                case UpgradeEffect.HoleRadius: return "+" + Mathf.RoundToInt(value) + "% growth";
                case UpgradeEffect.SuctionPower: return "+" + Mathf.RoundToInt(value) + "% suction";
                case UpgradeEffect.UnloadSpeed: return "+" + Mathf.RoundToInt(value) + "% unload";
                case UpgradeEffect.MachineSpeed: return "+" + Mathf.RoundToInt(value) + "% crafting";
                case UpgradeEffect.SellPrice: return "+" + Mathf.RoundToInt(value) + "% prices";
                case UpgradeEffect.CustomerRate: return "+" + Mathf.RoundToInt(value) + "% shoppers";
                case UpgradeEffect.JamResistance: return "+" + Mathf.RoundToInt(value) + "% jam resist";
                default: return "+" + Mathf.RoundToInt(value) + "% offline";
            }
        }

        void Purchase()
        {
            if (m_Economy == null || m_Definition == null) return;
            if (!m_Economy.TryBuyUpgrade(m_Definition)) return;
            ServiceLocator.Get<AudioService>()?.PlaySfx(AudioService.SfxUpgrade, 1f, 0.9f);
            Haptics.Play(HapticStrength.Medium);
            Refresh();
        }
    }
}
