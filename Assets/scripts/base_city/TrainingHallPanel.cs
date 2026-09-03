using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Тренировочный зал. Позволяет тратить накопленные XP из экспедиций
/// для прибавки характеристик героев. Открывается из CityManager.
/// </summary>
public class TrainingHallPanel : MonoBehaviour
{
    [Header("UI — выбор героя")]
    public Transform heroTabsContainer;     // горизонтальный список кнопок-героев
    public GameObject heroTabPrefab;        // Prefab: кнопка с именем героя

    [Header("UI — характеристики выбранного героя")]
    public Transform statsContainer;        // ScrollView Content для строк характеристик
    public GameObject statEntryPrefab;      // Prefab: TrainingStatEntryUI
    public TextMeshProUGUI heroNameText;
    public TextMeshProUGUI heroTotalXpText;

    [Header("UI — общее")]
    public Button closeButton;
    public TextMeshProUGUI titleText;

    private UnitProgress selectedHero;
    private readonly List<Button> heroTabs = new List<Button>();

    private static readonly CharacterStatType[] ALL_STATS =
    {
        CharacterStatType.Strength,
        CharacterStatType.Endurance,
        CharacterStatType.Will,
        CharacterStatType.Wisdom,
        CharacterStatType.Agility,
        CharacterStatType.Perception,
    };

    private void OnEnable()
    {
        if (titleText != null) titleText.text = "Тренировочный зал";
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        Refresh();
    }

    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
    }

    public void Refresh()
    {
        BuildHeroTabs();

        // Выбираем первого героя по умолчанию
        var heroes = GameManager.Instance?.globalHeroes;
        if (heroes != null && heroes.Count > 0)
            SelectHero(heroes[0]);
        else
            ClearStatsView();
    }

    private void BuildHeroTabs()
    {
        if (heroTabsContainer == null || heroTabPrefab == null) return;

        foreach (Transform child in heroTabsContainer) Destroy(child.gameObject);
        heroTabs.Clear();

        var heroes = GameManager.Instance?.globalHeroes;
        if (heroes == null) return;

        foreach (var hero in heroes)
        {
            if (hero == null) continue;
            var obj = Instantiate(heroTabPrefab, heroTabsContainer);
            var btn = obj.GetComponent<Button>();
            var label = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = hero.heroName;

            var capturedHero = hero;
            if (btn != null) btn.onClick.AddListener(() => SelectHero(capturedHero));
            heroTabs.Add(btn);
        }
    }

    private void SelectHero(UnitProgress hero)
    {
        selectedHero = hero;
        BuildStatsView();
    }

    private void BuildStatsView()
    {
        if (statsContainer == null || statEntryPrefab == null) return;

        foreach (Transform child in statsContainer) Destroy(child.gameObject);

        if (selectedHero == null) return;

        if (heroNameText != null) heroNameText.text = selectedHero.heroName;
        if (heroTotalXpText != null)
        {
            int total = selectedHero.strengthXP + selectedHero.enduranceXP + selectedHero.willXP
                      + selectedHero.wisdomXP + selectedHero.agilityXP + selectedHero.perceptionXP;
            heroTotalXpText.text = $"Суммарный XP: {total}";
        }

        foreach (var stat in ALL_STATS)
        {
            var obj = Instantiate(statEntryPrefab, statsContainer);
            var entry = obj.GetComponent<TrainingStatEntryUI>();
            if (entry != null) entry.Setup(selectedHero, stat, this);
        }
    }

    private void ClearStatsView()
    {
        if (statsContainer != null)
            foreach (Transform child in statsContainer) Destroy(child.gameObject);
        if (heroNameText != null) heroNameText.text = "—";
        if (heroTotalXpText != null) heroTotalXpText.text = string.Empty;
    }

    /// <summary>
    /// Вызывается из TrainingStatEntryUI после траты XP.
    /// </summary>
    public void OnStatUpdated() => BuildStatsView();

    public void ClosePanel() => gameObject.SetActive(false);
}
