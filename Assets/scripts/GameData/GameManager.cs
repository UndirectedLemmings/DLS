using System.Collections.Generic;
using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Глобальная точка доступа (Синглтон)
    public static GameManager Instance { get; private set; }

    // --- ИСПРАВЛЕНО: Убрали HeroData! Теперь тут только живой прогресс. ---
    [Header("Боевое построение")]
    public UnitProgress[] combatFormation = new UnitProgress[4];

    // Умная ссылка на лидера (всегда 0-й слот)
    public UnitProgress leaderProgress => (combatFormation != null && combatFormation.Length > 0) ? combatFormation[0] : null;

    [Header("Ресурсы забега")]
    public int Gold = 0;
    public List<FactionData> currentFactions = new List<FactionData>();

    [Header("Инвентарь и Карты")]
    public List<CardData> sessionCardPool = new List<CardData>();
    public List<ItemData> expeditionInventory = new List<ItemData>();

    [Header("Мета-прогрессия (Сохраняемое)")]
    public int globalGold = 0; // Золото для прокачки города
    public List<UnitProgress> globalHeroes = new List<UnitProgress>(); // ВЕСЬ ростер героев
    public List<ItemData> globalInventory = new List<ItemData>(); // Склад в городе

    [Header("Состояние игры")]
    public bool isMapPaused = false;
    public int currentExpeditionRound = 1;


    public static event System.Action OnInventoryChanged;
    public static event System.Action OnRoundChanged; // Новое событие для UI
    public static event System.Action OnMissionCompleted; // Событие при выполнении миссии

    // Вызывай этот метод, когда герой проходит стартовую клетку

    [Header("Экспедиция - Миссия")]
    public MissionObjectiveData[] missionPool; // Список всех возможных миссий (перетащи в инспекторе)
    public MissionObjectiveData currentMission { get; private set; }
    public int missionProgress { get; private set; }
    public Vector2Int startTilePosition { get; private set; } // Координаты тайла старта

    // Проверка: выполнена ли миссия?
    public bool IsMissionCompleted => currentMission != null && missionProgress >= currentMission.targetValue;

    public event Action OnNewLapStarted;
    // 1. Вызывается при загрузке карты экспедиции
    public void SetupRandomMission()
    {
        if (missionPool.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, missionPool.Length);
            currentMission = missionPool[randomIndex];
            missionProgress = 0;
            Debug.Log($"[Мисся] Выдано задание: {currentMission.missionName}. Цель: {currentMission.targetValue}");
        }
    }

    // 2. Запоминаем стартовый тайл (вызовем из Start_scene)
    public void RegisterStartTile(Vector2Int startPos)
    {
        startTilePosition = startPos;
    }

    // 3. Вызывай этот метод откуда угодно (например, из скрипта смерти врага: GameManager.Instance.AddMissionProgress(ObjectiveType.KillEnemies, 1))
    public void AddMissionProgress(ObjectiveType actionType, int amount)
    {
        if (IsMissionCompleted || currentMission == null) return;

        if (currentMission.type == actionType)
        {
            missionProgress += amount;
            if (IsMissionCompleted)
            {
                Debug.Log("🎉 Миссия выполнена! Вы можете вернуться на стартовый тайл для эвакуации.");
                OnMissionCompleted?.Invoke();
                 // Обновляем UI после изменения прогресса миссии
            }
        }
    }

    // Временная метка последнего засчитанного круга (в секундах)
    private float lastRoundTime = -10f;
    private const float MIN_TIME_BETWEEN_ROUNDS = 5f; // Защита: не чаще раза в 5 секунд

    public void CompleteExpeditionRound()
    {
        // Проверка: не слишком ли быстро мы крутимся?
        if (Time.time - lastRoundTime < MIN_TIME_BETWEEN_ROUNDS)
        {
            Debug.Log("[GameManager] Слишком быстро! Игнорируем вызов круга.");
            return;
        }

        lastRoundTime = Time.time;
        currentExpeditionRound++;

        Debug.Log($"[GameManager] Завершен круг №{currentExpeditionRound}");
        
        OnNewLapStarted?.Invoke();
        OnRoundChanged?.Invoke();
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Массив формации всегда должен быть инициализирован на 4 слота
        if (combatFormation == null || combatFormation.Length != 4)
        {
            combatFormation = new UnitProgress[4];
        }
    }

    // Метод, который вызывается, когда мы официально выходим из Города
    public void SetExpeditionSquad(UnitProgress[] newSquad, List<FactionData> factions)
    {
        combatFormation = newSquad;
        if (factions != null) currentFactions = factions;

        // --- ДОБАВЛЕНО: Новая экспедиция начинается с 1-го круга ---
        currentExpeditionRound = 1;

        PrepareSessionCardPool();
        Debug.Log($"[GameManager] Отряд принят из Города! Лидер: {leaderProgress?.heroName}");
    }

    public void PrepareSessionCardPool()
    {
        sessionCardPool.Clear();
        Debug.Log("[GameManager] Начинаем сборку пула карт экспедиции...");

        for (int i = 0; i < combatFormation.Length; i++)
        {
            UnitProgress unit = combatFormation[i];
            if (unit == null) continue;

            // 1. Пытаемся достать карты из шаблона HeroData
            if (unit.Template is HeroData heroData)
            {
                if (i == 0 && heroData.heroMainCards != null && heroData.heroMainCards.Count > 0)
                {
                    sessionCardPool.AddRange(heroData.heroMainCards);
                    Debug.Log($"[GameManager] Добавлены Main-карты шаблона от {unit.heroName}: {heroData.heroMainCards.Count} шт.");
                }
                if (heroData.heroSupportCards != null && heroData.heroSupportCards.Count > 0)
                {
                    sessionCardPool.AddRange(heroData.heroSupportCards);
                    Debug.Log($"[GameManager] Добавлены Support-карты шаблона от {unit.heroName}: {heroData.heroSupportCards.Count} шт.");
                }
            }

            // 2. Достаем карты из Универсального Фита-Класса (ClassFeatData)
            if (unit.classFeat != null && unit.classFeat is ClassFeatData classData)
            {
                // Если это ЛИДЕР (0-й слот), забираем его эксклюзивную LiderDeck
                if (i == 0 && classData.LiderDeck != null && classData.LiderDeck.Count > 0)
                {
                    sessionCardPool.AddRange(classData.LiderDeck);
                    Debug.Log($"[GameManager] <color=yellow>УСПЕХ!</color> Добавлена LiderDeck класса {classData.featName} от Лидера: {classData.LiderDeck.Count} шт.");
                }

                // Базовую колоду класса (HeroDeck) забираем у всех членов отряда
                if (classData.HeroDeck != null && classData.HeroDeck.Count > 0)
                {
                    sessionCardPool.AddRange(classData.HeroDeck);
                    Debug.Log($"[GameManager] Добавлена HeroDeck класса {classData.featName} от {unit.heroName}: {classData.HeroDeck.Count} шт.");
                }
            }
            else
            {
                Debug.LogWarning($"[GameManager] Предупреждение: У героя {unit.heroName} в слоте {i} нет Фита-Класса или он не настроен!");
            }
        }

        // 3. Карты Фракций врагов
        if (currentFactions != null && currentFactions.Count > 0)
        {
            foreach (FactionData faction in currentFactions)
            {
                if (faction != null && faction.factionCards != null && faction.factionCards.Count > 0)
                {
                    sessionCardPool.AddRange(faction.factionCards);
                    Debug.Log($"[GameManager] Добавлены карты вражеской фракции {faction.name}: {faction.factionCards.Count} шт.");
                }
            }
        }
        Debug.Log($"<color=lime>[GameManager]</color> Сборка завершена. Итоговый пул карт сессии: {sessionCardPool.Count} шт.");
    }

    // Вызывается из Building_Treasury и других источников лута
    public void AddLootToInventory(ItemData item)
    {
        if (item == null) return;

        expeditionInventory.Add(item);
        Debug.Log($"[LOOT DEBUG] Успешно добавлен предмет: {item.itemName}. Всего вещей: {expeditionInventory.Count}");

        OnInventoryChanged?.Invoke();
    }

    public void RemoveLootFromInventory(ItemData item)
    {
        if (expeditionInventory.Contains(item))
        {
            expeditionInventory.Remove(item);
            Debug.Log($"[GameManager] Предмет удален/использован: {item.name}");
            OnInventoryChanged?.Invoke(); // Обновляем UI после удаления
        }
    }

    // Этот метод теперь используется только для жесткого сброса (например, при загрузке)
    // или вызывается в конце экспедиции
    public void ClearExpeditionData()
    {
        combatFormation = new UnitProgress[4];
        sessionCardPool.Clear();
        expeditionInventory.Clear();
        currentFactions.Clear();
        Gold = 0;
        currentExpeditionRound = 1;

        // Сброс миссии
        currentMission = null;
        missionProgress = 0;
    }

    public void FinishExpedition(bool isSuccess)
    {
        Debug.Log($"[GameManager] Завершение экспедиции. Успех: {isSuccess}");

        if (isSuccess)
        {
            // 1. При УСПЕХЕ переносим заработанное золото в казну
            globalGold += Gold;

            // 2. При УСПЕХЕ переносим найденный лут на склад города
            globalInventory.AddRange(expeditionInventory);
            Debug.Log("[GameManager] Добыча успешно доставлена в город!");
        }
        else
        {
            Debug.LogWarning("[GameManager] Отряд сбежал или погиб! Вся добыча за эту экспедицию потеряна.");
            // Тут можно добавить штрафы, например, перенос только 10% золота
        }

        // 3. Вызываем наш универсальный метод очистки, чтобы не дублировать код
        ClearExpeditionData();

        // Герои уже лежат в globalHeroes, их опыт сохранится автоматически.
    }

    public void SwapHeroesInFormation(int slotA, int slotB)
    {
        UnitProgress temp = combatFormation[slotA];
        combatFormation[slotA] = combatFormation[slotB];
        combatFormation[slotB] = temp;

        Debug.Log($"[GameManager] Герои в слотах {slotA} и {slotB} поменялись местами.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isMapPaused = !isMapPaused;
            Debug.Log(isMapPaused ? "[GameManager] ТАКТИЧЕСКАЯ ПАУЗА" : "[GameManager] СНЯТИЕ ПАУЗЫ");
        }
    }
}