using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Одна строка апгрейда в панели Арсенала.
/// Prefab должен содержать: nameText, descriptionText, costText, statusText, buyButton.
/// </summary>
public class UpgradeEntryUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI statusText;
    public Button buyButton;
    public Image icon;

    private WeaponUpgradeData upgradeData;
    private ArsenalPanel panel;

    public void Setup(WeaponUpgradeData data, ArsenalPanel ownerPanel)
    {
        upgradeData = data;
        panel = ownerPanel;

        if (nameText != null) nameText.text = data.upgradeName;
        if (descriptionText != null) descriptionText.text = data.description;
        if (icon != null && data.icon != null) icon.sprite = data.icon;

        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);

        RefreshState();
    }

    private void OnBuyClicked()
    {
        if (panel.TryPurchaseUpgrade(upgradeData))
            RefreshState();
    }

    public void RefreshState()
    {
        if (GameManager.Instance == null || upgradeData == null) return;

        bool alreadyBought = GameManager.Instance.completedUpgrades.Contains(upgradeData);
        int available = upgradeData.requiredFaction != null
            ? GameManager.Instance.GetFactionResource(upgradeData.requiredFaction)
            : 0;
        bool canAfford = available >= upgradeData.resourceCost;

        // Стадия фракции
        bool stageOk = true;
        if (upgradeData.requiredFaction != null)
        {
            FactionProgressStage stage = GameManager.Instance.GetFactionProgressStage(upgradeData.requiredFaction);
            stageOk = stage >= upgradeData.requiredFactionStage;
        }

        if (costText != null)
        {
            string factionName = upgradeData.requiredFaction != null ? upgradeData.requiredFaction.factionName : "—";
            costText.text = $"{upgradeData.resourceCost} [{factionName}] (есть: {available})";
        }

        if (statusText != null)
        {
            if (alreadyBought)
                statusText.text = "✓ Куплено";
            else if (!stageOk)
                statusText.text = "🔒 Требуется прогресс";
            else if (!canAfford)
                statusText.text = "Недостаточно ресурсов";
            else
                statusText.text = "Доступно";
        }

        if (buyButton != null)
            buyButton.interactable = !alreadyBought && canAfford && stageOk;
    }
}
