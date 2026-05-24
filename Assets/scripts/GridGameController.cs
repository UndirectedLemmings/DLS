using UnityEngine;
using UnityEngine.Tilemaps;

public class GridGameController : MonoBehaviour
{
    public static GridGameController Instance { get; private set; }

    public Tilemap tilemap;
    public Vector3Int originCell = Vector3Int.zero;

    public LogicalGrid logic { get; private set; }

    // Переменные теперь только для чтения извне, они задаются из генератора
    public int gridWidth { get; private set; }
    public int gridHeight { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Найдено несколько GridGameController! Удаляю дубликат.");
            Destroy(gameObject);
            return;
        }

        // ВАЖНО: Мы больше не создаем logic здесь, ждем команды от FILL_MAP_v4
    }

    // --- НОВЫЙ МЕТОД ---
    // Эту функцию должен вызвать FILL_MAP_v4, когда определится с размерами
    public void InitializeGrid(int mapWidth, int mapHeight)
    {
        gridWidth = mapWidth;
        gridHeight = mapHeight;

        logic = new LogicalGrid(gridWidth, gridHeight);
        Debug.Log($"Логическая сетка успешно инициализирована генератором. Размер: {gridWidth}x{gridHeight}");
    }

    void Update()
    {
        // Добавляем защиту от кликов до генерации карты
        if (logic == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cell = tilemap.WorldToCell(world);

            Vector2Int lp = new Vector2Int(cell.x - originCell.x, cell.y - originCell.y);

            if (logic.InBounds(lp))
                Debug.Log($"Логическая клетка: {lp.x},{lp.y}");
            else
                Debug.Log("Вне игровой зоны");
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Пускаем луч в 2D пространстве в точку клика
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                // Пытаемся найти на объекте НАШ УНИВЕРСАЛЬНЫЙ ИНТЕРФЕЙС
                IMapInteractable interactableObj = hit.collider.GetComponent<IMapInteractable>();

                if (interactableObj != null)
                {
                    // Объект поддерживает подсказки! Получаем текст.
                    string infoText = interactableObj.GetDescription();

                    // Пока выводим в консоль
                    Debug.Log($"DLS-ПОДСКАЗКА: \n{infoText}");
                }
            }
        }
    }

    public Vector3 GetWorldPosition(Vector2Int logicPos)
    {
        Vector3Int cellPos = new Vector3Int(logicPos.x + originCell.x, logicPos.y + originCell.y, 0);
        return tilemap.GetCellCenterWorld(cellPos);
    }
}