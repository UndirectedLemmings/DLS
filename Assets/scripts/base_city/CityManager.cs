using System.Collections.Generic;
using UnityEngine;

public class CityManager : MonoBehaviour
{
    public static CityManager Instance { get; private set; }

    [Header("Данные города")]
    public int Gold;
    public int Materials;

    [Header("Казарма (Постоянная)")]
    [Tooltip("Все доступные герои")]
    public List<UnitProgress> allAvailableHeroes = new List<UnitProgress>();
    public Transform barracksListContainer;
    public GameObject heroEntryPrefab;

    [Header("Фракции")]
    public List<FactionData> unlockedFactions;

    [Header("Генерация новичков")]
    public HeroData baseHeroTemplate;
    public FeatData noviceClassFeat;

    [Header("Всплывающие окна (Popups)")]
    public ExpeditionSetupPanel expeditionSetupPanel;
    public HeroInfoPanel heroInfoPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 1. Подтягиваем глобальный список из GameManager
        if (GameManager.Instance != null)
        {
            // Теперь allAvailableHeroes указывает на тот же список в памяти, что и globalHeroes.
            // Любые добавления (Add) здесь автоматически сохранятся в GameManager!
            allAvailableHeroes = GameManager.Instance.globalHeroes;
        }
        else if (allAvailableHeroes == null)
        {
            allAvailableHeroes = new List<UnitProgress>();
        }

        // 2. Генерируем стартовых героев ТОЛЬКО если список пуст (самый первый запуск игры)
        if (allAvailableHeroes.Count == 0)
        {
            GenerateHeroBatch(4);
        }

        // Казарма открыта всегда, сразу заполняем её
        RefreshBarracksUI();
    }

    // ==========================================
    // ЛОГИКА ДАННЫХ
    // ==========================================

    public void GenerateHeroBatch(int amount)
    {
        if (baseHeroTemplate == null || noviceClassFeat == null)
        {
            Debug.LogWarning("[CityManager] Нельзя сгенерировать героев: не назначены шаблоны в Инспекторе!");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            UnitProgress newHero = new UnitProgress(baseHeroTemplate);
            newHero.heroName = "Искатель #" + Random.Range(100, 999);
            newHero.classFeat = noviceClassFeat;

            if (newHero.activeFeats == null) newHero.activeFeats = new List<FeatData>();
            newHero.activeFeats.Add(noviceClassFeat);
            newHero.overworldFeats = new FeatController(newHero.GetAllActiveFeats(), null);
            allAvailableHeroes.Add(newHero);
        }

        Debug.Log($"[CityManager] Успешно сгенерировано {amount} новых героев.");
    }

    public void RecruitHero(UnitProgress newHero)
    {
        allAvailableHeroes.Add(newHero);
        RefreshBarracksUI(); // Обновляем список при найме
    }

    public void AddResource(int amount) => Gold += amount;

    // ==========================================
    // УПРАВЛЕНИЕ ИНТЕРФЕЙСОМ (UI)
    // ==========================================

    /// <summary>
    /// Перерисовывает постоянный список героев в левой части экрана
    /// </summary>
    public void RefreshBarracksUI()
    {
        if (barracksListContainer == null || heroEntryPrefab == null) return;

        foreach (Transform child in barracksListContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (UnitProgress hero in allAvailableHeroes)
        {
            if (hero == null) continue;

            GameObject entryObj = Instantiate(heroEntryPrefab, barracksListContainer);
            HeroListEntryUI entryUI = entryObj.GetComponent<HeroListEntryUI>();

            if (entryUI != null)
            {
                // Передаем ссылку на панель, но управлять открытием теперь будет CityManager
                entryUI.Setup(hero, heroInfoPanel);
            }
        }
    }

    /// <summary>
    /// Открывает сборку отряда и ЗАКРЫВАЕТ инфо-панель героя
    /// </summary>
    public void OpenExpeditionSetupPanel()
    {
        // 1. Тушим конфликтующие окна
        if (heroInfoPanel != null) heroInfoPanel.ClosePanel();

        // 2. Открываем целевое окно
        if (expeditionSetupPanel != null)
        {
            expeditionSetupPanel.gameObject.SetActive(true);
            expeditionSetupPanel.PopulateRosters(allAvailableHeroes, unlockedFactions);
            Debug.Log("[CityManager] Панель сбора отряда открыта.");
        }
    }

    /// <summary>
    /// Открывает инфо-панель героя и ЗАКРЫВАЕТ сборку отряда
    /// </summary>
    public void OpenHeroInfoPanel(UnitProgress hero)
    {
        // 1. Тушим конфликтующие окна
        if (expeditionSetupPanel != null) expeditionSetupPanel.gameObject.SetActive(false);

        // 2. Открываем целевое окно
        if (heroInfoPanel != null)
        {
            heroInfoPanel.OpenPanel(hero);
            Debug.Log($"[CityManager] Открыта информация о герое {hero.heroName}.");
        }
    }
}