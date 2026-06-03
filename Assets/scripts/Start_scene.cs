using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class Start_scene : MonoBehaviour
{
    public Tilemap Tilemap;
    public Character_move character; // префаб персонажа
    public FILL_MAP_v4 FMT; // генератор карты
    public Text round;

    private Character_move character_clone;

    // НОВОЕ: Храним живой CombatUnit нашего лидера на глобальной карте
    public CombatUnit LeaderCombatUnit { get; private set; }

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

        // --- ИСПРАВЛЕНО 1: Принимаем Vector2Int из обновленного генератора ---
        Vector2Int startVector = FMT.Get_Start_road();

        // --- ИСПРАВЛЕНО 2: Создаем Vector3Int локально ТОЛЬКО для запроса позиции у Tilemap ---
        Vector3Int tilemapPos = new Vector3Int(startVector.x, startVector.y, 0);

        // Спавним префаб героя на карте
        character_clone = Instantiate(character, Tilemap.GetCellCenterWorld(tilemapPos), UnityEngine.Quaternion.Euler(45, 0, 0));

        // Передаем карту
        character_clone.Tilemap = Tilemap;

        // Передаем 2D-координаты старта в обновленный мозг героя
        character_clone.StartJourney(startVector);

        // ====================================================================
        // МАГИЯ ИНИЦИАЛИЗАЦИИ ФИТОВ ПРИ СТАРТЕ ПРИКЛЮЧЕНИЯ
        // ====================================================================

        // 1. Проверяем, назначен ли лидер в генераторе карт (FMT)
        if (FMT.activeLeader != null)
        {
            // 2. Создаем для него полноценный CombatUnit в памяти.
            // Передаем список фитов из activeLeader.activeFeats и ссылку на самого себя.
            // Передаем FMT.activeLeader, помечаем как союзника (true) и ставим в слот 0
            LeaderCombatUnit = new CombatUnit(FMT.activeLeader, true, 0);

            Debug.Log($"<color=lime>[СТАРТ СЦЕНЫ]</color> Живой CombatUnit успешно создан для лидера: {LeaderCombatUnit.BaseData.unitName}");

            // 3. ФИЗИЧЕСКИ запускаем триггер фитов начала приключения!
            if (LeaderCombatUnit.featController != null)
            {
                LeaderCombatUnit.featController.TriggerAdventureStartFeats();
            }
        }
        else
        {
            Debug.LogError("<color=red>[СТАРТ СЦЕНЫ]</color> Ошибка: В FILL_MAP_v4 не задан activeLeader! Невозможно инициализировать фиты.");
        }
        // ====================================================================
    }
}
