using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Панель Арсенала. Показывает список апгрейдов оружия, позволяет купить за ресурсы фракций.
/// Подключить к GameObject-попапу в сцене города (аналогично ExpeditionSetupPanel).
/// </summary>
public class ArsenalPanel : MonoBehaviour
{
    [Header("Данные")]
    [Tooltip("Все доступные апгрейды — заполнить в инспекторе.")]
    public WeaponUpgradeData[] upgradesList;

    [Header("UI-ссылки")]
    public Transform entriesContainer;       // ScrollView Content
    public GameObject upgradeEntryPrefab;    // Prefab: UpgradeEntryUI
    public Button closeButton;
    public TextMeshProUGUI titleText;

    private void OnEnable()
    {
        if (titleText != null) titleText.text = "Арсенал";
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        GameManager.OnMetaResourcesChanged += Refresh;
        GameManager.OnFactionProgressChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);

        GameManager.OnMetaResourcesChanged -= Refresh;
        GameManager.OnFactionProgressChanged -= Refresh;
    }

    public void Refresh()
    {
        if (entriesContainer == null || upgradeEntryPrefab == null) return;

        foreach (Transform child in entriesContainer)
            Destroy(child.gameObject);

        if (upgradesList == null) return;

        foreach (var upgrade in upgradesList)
        {
            if (upgrade == null) continue;
            GameObject entryObj = Instantiate(upgradeEntryPrefab, entriesContainer);
            var entry = entryObj.GetComponent<UpgradeEntryUI>();
            if (entry != null) entry.Setup(upgrade, this);
        }
    }

    /// <summary>
    /// Попытка купить апгрейд. Проверяет ресурсы и стадию фракции.
    /// </summary>
    public bool TryPurchaseUpgrade(WeaponUpgradeData upgrade)
    {
        if (upgrade == null || GameManager.Instance == null) return false;

        // Уже куплено?
        if (GameManager.Instance.completedUpgrades.Contains(upgrade))
        {
            Debug.Log($"[Арсенал] Апгрейд «{upgrade.upgradeName}» уже куплен.");
            return false;
        }

        // Проверка стадии фракции
        if (upgrade.requiredFaction != null)
        {
            FactionProgressStage currentStage = GameManager.Instance.GetFactionProgressStage(upgrade.requiredFaction);
            if (currentStage < upgrade.requiredFactionStage)
            {
                Debug.Log($"[Арсенал] Недостаточный прогресс фракции {upgrade.requiredFaction.factionName}.");
                return false;
            }

            // Списываем ресурсы
            if (!GameManager.Instance.SpendFactionResource(upgrade.requiredFaction, upgrade.resourceCost))
                return false;
        }

        // Применяем апгрейд
        GameManager.Instance.completedUpgrades.Add(upgrade);
        if (upgrade.targetItem != null && upgrade.grantedFeat != null)
        {
            if (!upgrade.targetItem.grantedFeats.Contains(upgrade.grantedFeat))
                upgrade.targetItem.grantedFeats.Add(upgrade.grantedFeat);
        }

        Debug.Log($"[Арсенал] Апгрейд «{upgrade.upgradeName}» куплен!");
        Refresh();
        return true;
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
