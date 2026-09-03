using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Панель Разведбюро. Два раздела:
/// 1) Записи о врагах — купить за ресурсы фракции, чтобы раскрыть информацию.
/// 2) Прогресс фракций — показывает текущую стадию каждой известной фракции.
/// </summary>
public class ScoutBureauPanel : MonoBehaviour
{
    [Header("Данные")]
    [Tooltip("Все возможные записи о врагах — заполнить в инспекторе.")]
    public ScoutUnlockData[] scoutEntries;

    [Header("UI — Записи о врагах")]
    public Transform entriesContainer;
    public GameObject scoutEntryPrefab;

    [Header("UI — Прогресс фракций")]
    public Transform factionProgressContainer;
    public GameObject factionProgressEntryPrefab;

    [Header("UI — Общее")]
    public Button closeButton;
    public TextMeshProUGUI titleText;

    private void OnEnable()
    {
        if (titleText != null) titleText.text = "Разведбюро";
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
        RefreshScoutEntries();
        RefreshFactionProgress();
    }

    private void RefreshScoutEntries()
    {
        if (entriesContainer == null || scoutEntryPrefab == null) return;

        foreach (Transform child in entriesContainer)
            Destroy(child.gameObject);

        if (scoutEntries == null) return;

        foreach (var entry in scoutEntries)
        {
            if (entry == null) continue;
            GameObject obj = Instantiate(scoutEntryPrefab, entriesContainer);
            var ui = obj.GetComponent<ScoutEntryUI>();
            if (ui != null) ui.Setup(entry, this);
        }
    }

    private void RefreshFactionProgress()
    {
        if (factionProgressContainer == null || factionProgressEntryPrefab == null) return;

        foreach (Transform child in factionProgressContainer)
            Destroy(child.gameObject);

        if (GameManager.Instance == null) return;

        // Показываем прогресс всех известных фракций через CityManager (город),
        // fallback — текущие фракции экспедиции.
        var factions = CityManager.Instance != null && CityManager.Instance.unlockedFactions != null && CityManager.Instance.unlockedFactions.Count > 0
            ? CityManager.Instance.unlockedFactions
            : GameManager.Instance.currentFactions;
        if (factions == null) return;

        foreach (var faction in factions)
        {
            if (faction == null) continue;
            GameObject obj = Instantiate(factionProgressEntryPrefab, factionProgressContainer);
            var label = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                FactionProgressStage stage = GameManager.Instance.GetFactionProgressStage(faction);
                int resources = GameManager.Instance.GetFactionResource(faction);
                label.text = $"{faction.factionName}  |  Стадия: {TranslateStage(stage)}  |  Ресурсы: {resources}";
            }
        }
    }

    /// <summary>
    /// Покупка записи о враге.
    /// </summary>
    public bool TryUnlockEntry(ScoutUnlockData entry)
    {
        if (entry == null || GameManager.Instance == null) return false;

        if (GameManager.Instance.unlockedScoutEntries.Contains(entry))
        {
            Debug.Log($"[Разведбюро] Запись «{entry.targetUnit?.unitName}» уже куплена.");
            return false;
        }

        if (entry.requiredFaction != null)
        {
            if (!GameManager.Instance.SpendFactionResource(entry.requiredFaction, entry.resourceCost))
                return false;
        }

        GameManager.Instance.unlockedScoutEntries.Add(entry);
        Debug.Log($"[Разведбюро] Запись о «{entry.targetUnit?.unitName}» разблокирована!");
        Refresh();
        return true;
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }

    private string TranslateStage(FactionProgressStage stage)
    {
        switch (stage)
        {
            case FactionProgressStage.Start: return "Начало";
            case FactionProgressStage.AfterFirstBoss: return "После 1-го босса";
            case FactionProgressStage.AfterSecondBoss: return "После 2-го босса";
            case FactionProgressStage.Completed: return "Завершено";
            default: return stage.ToString();
        }
    }
}
