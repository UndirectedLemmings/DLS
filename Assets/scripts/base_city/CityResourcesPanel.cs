using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Панель ресурсов города. Показывает:
/// — Золото (globalGold)
/// — Фракционные ресурсы (по каждой известной фракции)
/// — Инвентарь (globalInventory, краткий список)
/// Рекомендуется размещать как постоянно видимый HUD или отдельную панель.
/// </summary>
public class CityResourcesPanel : MonoBehaviour
{
    [Header("Золото")]
    public TextMeshProUGUI goldText;

    [Header("Фракционные ресурсы")]
    public Transform factionResourcesContainer;  // вертикальный layout
    public GameObject resourceEntryPrefab;        // Prefab: одна строка «Фракция: N»

    [Header("Инвентарь (краткий список)")]
    public Transform inventoryContainer;
    public GameObject inventoryEntryPrefab;       // Prefab: одна строка предмета
    public TextMeshProUGUI inventoryCountText;     // «Предметов в запасе: N»

    [Header("Известные фракции (для отображения ресурсов)")]
    [Tooltip("Заполнить вручную или через CityManager.unlockedFactions.")]
    public List<FactionData> trackedFactions;

    [Header("UI")]
    public Button closeButton;

    private void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);

        GameManager.OnMetaResourcesChanged += Refresh;
        GameManager.OnFactionProgressChanged += Refresh;
        GameManager.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);

        GameManager.OnMetaResourcesChanged -= Refresh;
        GameManager.OnFactionProgressChanged -= Refresh;
        GameManager.OnInventoryChanged -= Refresh;
    }

    private void Start()
    {
        // Если список фракций не заполнен вручную — берём из CityManager
        if ((trackedFactions == null || trackedFactions.Count == 0) && CityManager.Instance != null)
            trackedFactions = CityManager.Instance.unlockedFactions;

        Refresh();
    }

    public void Refresh()
    {
        RefreshGold();
        RefreshFactionResources();
        RefreshInventory();
    }

    private void RefreshGold()
    {
        if (goldText == null || GameManager.Instance == null) return;
        goldText.text = $"Золото: {GameManager.Instance.globalGold}";
    }

    private void RefreshFactionResources()
    {
        if (factionResourcesContainer == null || resourceEntryPrefab == null) return;

        foreach (Transform child in factionResourcesContainer) Destroy(child.gameObject);

        if (trackedFactions == null || GameManager.Instance == null) return;

        foreach (var faction in trackedFactions)
        {
            if (faction == null) continue;
            int amount = GameManager.Instance.GetFactionResource(faction);

            var obj = Instantiate(resourceEntryPrefab, factionResourcesContainer);
            var label = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{faction.factionName}: {amount}";
        }
    }

    private void RefreshInventory()
    {
        if (GameManager.Instance == null) return;

        var inv = GameManager.Instance.globalInventory;

        if (inventoryCountText != null)
            inventoryCountText.text = $"Предметов в запасе: {inv?.Count ?? 0}";

        if (inventoryContainer == null || inventoryEntryPrefab == null) return;

        foreach (Transform child in inventoryContainer) Destroy(child.gameObject);

        if (inv == null) return;

        foreach (var item in inv)
        {
            if (item == null) continue;
            var obj = Instantiate(inventoryEntryPrefab, inventoryContainer);
            var label = obj.GetComponentInChildren<TextMeshProUGUI>();
            var icon  = obj.GetComponentInChildren<Image>();
            if (label != null) label.text = item.itemName;
            if (icon  != null && item.itemIcon != null) icon.sprite = item.itemIcon;
        }
    }

    public void ClosePanel() => gameObject.SetActive(false);
}
