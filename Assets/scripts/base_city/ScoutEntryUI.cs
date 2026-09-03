using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Строка записи о враге в Разведбюро.
/// Prefab: unitNameText, infoText (скрыт пока не куплено), costText, buyButton.
/// </summary>
public class ScoutEntryUI : MonoBehaviour
{
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI costText;
    public Button buyButton;
    public Image portrait;

    private ScoutUnlockData entryData;
    private ScoutBureauPanel panel;

    public void Setup(ScoutUnlockData data, ScoutBureauPanel ownerPanel)
    {
        entryData = data;
        panel = ownerPanel;

        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);

        RefreshState();
    }

    private void OnBuyClicked()
    {
        if (panel.TryUnlockEntry(entryData))
            RefreshState();
    }

    public void RefreshState()
    {
        if (entryData == null) return;

        bool unlocked = GameManager.Instance != null &&
                        GameManager.Instance.unlockedScoutEntries.Contains(entryData);

        // Имя юнита: показываем всегда
        if (unitNameText != null)
            unitNameText.text = entryData.targetUnit != null ? entryData.targetUnit.unitName : "???";

        // Портрет
        if (portrait != null && entryData.targetUnit != null && entryData.targetUnit.portrait != null)
            portrait.sprite = entryData.targetUnit.portrait;

        // Информация: только если куплено
        if (infoText != null)
        {
            infoText.gameObject.SetActive(unlocked);
            if (unlocked) infoText.text = entryData.revealedInfo;
        }

        // Стоимость и кнопка
        if (costText != null)
        {
            if (unlocked)
            {
                costText.text = "✓ Изучено";
            }
            else
            {
                string factionName = entryData.requiredFaction != null ? entryData.requiredFaction.factionName : "—";
                int available = entryData.requiredFaction != null
                    ? GameManager.Instance?.GetFactionResource(entryData.requiredFaction) ?? 0
                    : 0;
                costText.text = $"{entryData.resourceCost} [{factionName}] (есть: {available})";
            }
        }

        if (buyButton != null)
        {
            bool canAfford = entryData.requiredFaction == null ||
                (GameManager.Instance != null &&
                 GameManager.Instance.GetFactionResource(entryData.requiredFaction) >= entryData.resourceCost);
            buyButton.gameObject.SetActive(!unlocked);
            buyButton.interactable = canAfford;
        }
    }
}
