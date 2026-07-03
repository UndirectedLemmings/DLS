using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExpeditionSetupPanel : MonoBehaviour
{
    [Header("Ячейки Отряда (4 слота)")]
    [Tooltip("0-й слот - это всегда Лидер!")]
    public Button[] heroSlots = new Button[4];
    public TextMeshProUGUI[] heroSlotNames = new TextMeshProUGUI[4];

    [Header("Ячейки Врагов (3 слота)")]
    public Button[] factionSlots = new Button[3];
    public TextMeshProUGUI[] factionSlotNames = new TextMeshProUGUI[3];

    [Header("Ленты выбора (Scroll Views)")]
    public Transform heroesRosterContainer;
    public Transform factionsRosterContainer;
    public GameObject rosterButtonPrefab; // Твой префаб с RosterButtonUI

    [Header("Старт")]
    public Button startExpeditionButton;
    public string expeditionSceneName = "ExpeditionScene";

    // Текущий выбор
    private UnitProgress[] selectedHeroes = new UnitProgress[4];
    private FactionData[] selectedFactions = new FactionData[3];



    private void Start()
    {
        // Подписываем ячейки на клик (для удаления из слота)
        for (int i = 0; i < heroSlots.Length; i++)
        {
            int index = i; // Локальная копия для замыкания
            heroSlots[i].onClick.AddListener(() => RemoveHeroFromSlot(index));
        }

        for (int i = 0; i < factionSlots.Length; i++)
        {
            int index = i;
            factionSlots[i].onClick.AddListener(() => RemoveFactionFromSlot(index));
        }

        startExpeditionButton.onClick.AddListener(ConfirmAndStart);

        UpdateSlotsUI();
    }

    // ================== ЛОГИКА ГЕРОЕВ ==================

    public void TryAddHeroToSquad(UnitProgress hero)
    {
        // Проверяем, нет ли его уже в отряде
        foreach (var h in selectedHeroes) if (h == hero) return;

        // Ищем первый пустой слот
        for (int i = 0; i < selectedHeroes.Length; i++)
        {
            if (selectedHeroes[i] == null)
            {
                selectedHeroes[i] = hero;
                UpdateSlotsUI();
                return;
            }
        }
        Debug.Log("Отряд полон! Освободите место.");
    }

    private void RemoveHeroFromSlot(int slotIndex)
    {
        selectedHeroes[slotIndex] = null;
        UpdateSlotsUI();
    }

    // ================== ЛОГИКА ФРАКЦИЙ ==================

    public void TryAddFaction(FactionData faction)
    {
        // Проверяем на дубликаты (опционально, если хочешь чтобы можно было выбрать одних гоблинов 3 раза - удали эту строку)
        foreach (var f in selectedFactions) if (f == faction) return;

        for (int i = 0; i < selectedFactions.Length; i++)
        {
            if (selectedFactions[i] == null)
            {
                selectedFactions[i] = faction;
                UpdateSlotsUI();
                return;
            }
        }
        Debug.Log("Слоты фракций заполнены!");
    }

    private void RemoveFactionFromSlot(int slotIndex)
    {
        selectedFactions[slotIndex] = null;
        UpdateSlotsUI();
    }

    // ================== ОБНОВЛЕНИЕ UI ==================

    private void UpdateSlotsUI()
    {
        // Обновляем ячейки героев
        for (int i = 0; i < selectedHeroes.Length; i++)
        {
            if (selectedHeroes[i] != null)
            {
                heroSlotNames[i].text = (i == 0) ? $"[ЛИДЕР] {selectedHeroes[i].heroName}" : selectedHeroes[i].heroName;
            }
            else
            {
                heroSlotNames[i].text = (i == 0) ? "Выбрать Лидера" : "Пустой слот";
            }
        }

        // Обновляем ячейки фракций
        for (int i = 0; i < selectedFactions.Length; i++)
        {
            if (selectedFactions[i] != null)
            {
                factionSlotNames[i].text = selectedFactions[i].name;
            }
            else
            {
                factionSlotNames[i].text = "Выбрать фракцию";
            }
        }
    }

    // ================== ЗАПОЛНЕНИЕ ЛЕНТ ==================

    // Эту функцию можно вызвать из CityManager, передав ему списки
    public void PopulateRosters(List<UnitProgress> availableHeroes, List<FactionData> unlockedFactions)
    {
        // Очистка
        foreach (Transform child in heroesRosterContainer) Destroy(child.gameObject);
        foreach (Transform child in factionsRosterContainer) Destroy(child.gameObject);

        // Спавн Героев
        if (availableHeroes != null)
        {
            foreach (var hero in availableHeroes)
            {
                GameObject obj = Instantiate(rosterButtonPrefab, heroesRosterContainer);
                obj.GetComponent<RosterButtonUI>().SetupHero(hero, this);
            }
        }

        // Спавн Фракций
        if (unlockedFactions != null)
        {
            foreach (var faction in unlockedFactions)
            {
                GameObject obj = Instantiate(rosterButtonPrefab, factionsRosterContainer);
                obj.GetComponent<RosterButtonUI>().SetupFaction(faction, this);
            }
        }
    }

    // ================== ЗАПУСК ==================

    private void ConfirmAndStart()
    {
        // Главная проверка: назначен ли Лидер (0-й слот)
        if (selectedHeroes[0] == null)
        {
            Debug.LogError("[ExpeditionPanel] Нельзя начать без Лидера! Заполните первую ячейку.");
            return; // Можно вывести сообщение на экран
        }

        // Собираем фракции без пустых дыр (если игрок выбрал только 1 фракцию)
        List<FactionData> activeFactions = new List<FactionData>();
        foreach (var f in selectedFactions)
        {
            if (f != null) activeFactions.Add(f);
        }

        // Передаем данные в GameManager
        GameManager.Instance.SetExpeditionSquad(selectedHeroes, activeFactions);

        Debug.Log($"<color=lime>[СТАРТ]</color> Отряд собран! Загрузка сцены: {expeditionSceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(expeditionSceneName);
    }
}
