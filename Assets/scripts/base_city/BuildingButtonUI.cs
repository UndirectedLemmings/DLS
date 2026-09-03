using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Кнопка здания на сцене города. Блокируется замком, пока условие открытия не выполнено.
/// Назначить в инспекторе: requiredFaction, requiredMinResources, затем связать OnClick.
/// </summary>
public class BuildingButtonUI : MonoBehaviour
{
    [Header("Условие открытия")]
    [Tooltip("Фракция, ресурсы которой проверяются. Null = здание доступно сразу.")]
    public FactionData requiredFaction;

    [Tooltip("Минимальное количество ресурсов фракции для разблокировки здания.")]
    public int requiredMinResources = 0;

    [Header("UI-ссылки")]
    public Button mainButton;
    public GameObject lockIcon;
    public TextMeshProUGUI requirementText;

    private void OnEnable() => RefreshState();

    private void Start() => RefreshState();

    public void RefreshState()
    {
        bool unlocked = IsUnlocked();

        if (mainButton != null) mainButton.interactable = unlocked;
        if (lockIcon != null) lockIcon.SetActive(!unlocked);

        if (requirementText != null)
        {
            if (unlocked)
            {
                requirementText.text = string.Empty;
            }
            else
            {
                string factionName = requiredFaction != null ? requiredFaction.factionName : "???";
                int current = requiredFaction != null && GameManager.Instance != null
                    ? GameManager.Instance.GetFactionResource(requiredFaction)
                    : 0;
                requirementText.text = $"Нужно: {requiredMinResources} [{factionName}]\nЕсть: {current}";
            }
        }
    }

    private bool IsUnlocked()
    {
        if (requiredFaction == null || requiredMinResources <= 0) return true;
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.GetFactionResource(requiredFaction) >= requiredMinResources;
    }
}
