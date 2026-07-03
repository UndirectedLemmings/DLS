using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Start_scene : MonoBehaviour
{
    public Tilemap Tilemap;
    public Character_move character; // префаб персонажа
    public FILL_MAP_v4 FMT; // генератор карты
    public TMP_Text round;

    private Character_move character_clone;

    // ИСПРАВЛЕНО: На глобальной карте мы храним живой прогресс лидера, а не боевой юнит
    public UnitProgress LeaderProgress { get; private set; }

    public void Start()
    {
        if (FMT != null)
        {
            FMT.StartGenerationWithRetries();
        }
        else
        {
            Debug.LogError("Забыл перетащить FMT");
            return;
        }

        // --- Принимаем Vector2Int из обновленного генератора ---
        Vector2Int startVector = FMT.Get_Start_road();

        // --- Создаем Vector3Int локально ТОЛЬКО для запроса позиции у Tilemap ---
        Vector3Int tilemapPos = new Vector3Int(startVector.x, startVector.y, 0);

        // Спавним префаб героя на карте
        character_clone = Instantiate(character, Tilemap.GetCellCenterWorld(tilemapPos), UnityEngine.Quaternion.Euler(45, 0, 0));

        // Передаем карту
        character_clone.Tilemap = Tilemap;

        // Отправляем в первую точку
        character_clone.StartJourney(startVector);

        // ====================================================================
        // --- НОВОЕ: ИНТЕГРАЦИЯ ЭВАКУАЦИИ И МИССИЙ ---
        if (GameManager.Instance != null)
        {
            // Регистрируем стартовый тайл в GameManager
            GameManager.Instance.RegisterStartTile(startVector);

            // Выбираем случайную миссию на этот забег
            GameManager.Instance.SetupRandomMission();
        }

        // Автоматически находим UI Эвакуации и передаем ему нашего живого клона героя
        ExtractionUIManager extractionUI = FindFirstObjectByType<ExtractionUIManager>();
        if (extractionUI != null)
        {
            extractionUI.playerCharacter = character_clone;
            Debug.Log("[СТАРТ СЦЕНЫ] Герой успешно привязан к интерфейсу эвакуации.");
        }
        // ====================================================================

        // ====================================================================
        // --- 1. ПРИВЯЗКА ЛИДЕРА ИЗ GameManager ---
        LeaderProgress = GameManager.Instance.combatFormation[0];

        if (LeaderProgress != null)
        {
            Debug.Log($"[СТАРТ СЦЕНЫ] Назначен Leader: {LeaderProgress.Template.unitName}");

            // --- 2. ТРИГГЕРИМ ФИТЫ НАЧАЛА ПРИКЛЮЧЕНИЯ ДЛЯ ВСЕГО ОТРЯДА ---
            foreach (UnitProgress unit in GameManager.Instance.combatFormation)
            {
                if (unit != null)
                {
                    // Создаем глобальный контроллер для каждого (чтобы пассивки и статы работали у всех)
                    unit.overworldFeats = new FeatController(unit.GetAllActiveFeats(), null);

                    // УМНАЯ ПРОВЕРКА: Проверяем, совпадает ли текущий юнит с лидером
                    bool isLeader = (unit == LeaderProgress);
                    unit.overworldFeats.TriggerAdventureStartFeats(unit, isLeader);
                }
            }

            Debug.Log("<color=lime>[СТАРТ СЦЕНЫ]</color> Глобальные фиты успешно запущены для всего отряда.");
        }
        else
        {
            Debug.LogError("<color=red>[СТАРТ СЦЕНЫ]</color> Ошибка: В GameManager нет лидера!");
        }
        // ====================================================================
    }
}