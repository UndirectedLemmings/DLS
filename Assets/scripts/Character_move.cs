using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Character_move : MonoBehaviour
{
    public Tilemap Tilemap;
    public float speed = 5f;
    public float crossroadWaitTime = 1.0f; // Время ожидания на перекрестке в секундах

    private bool isMoving = false;
    private bool isWaiting = false;
    private int lapsCount = 0; // Бывший 'R', счетчик кругов

    private List<Vector3Int> currentPath;
    private int waypointIndex = 0;
    private Vector3Int startNode;

    private Vector3Int lastCheckedCell;
    private HandManager HandManager;
     

    private void Start()
    {
        // Находим BuildManager один раз при старте, чтобы не нагружать игру каждый ход
        HandManager = FindFirstObjectByType<HandManager>();
    }

    public void StartJourney(Vector3Int startPos)
    {
        startNode = startPos;
        RequestNextRoute(startPos);
    }

    private void Update()
    {
        // Если мы не двигаемся, ждем на перекрестке или пути нет — выходим
        if (!isMoving || isWaiting || currentPath == null || currentPath.Count == 0)
            return;

        // Определяем текущую целевую клетку и ее мировые координаты
        Vector3Int targetCell = currentPath[waypointIndex];
        Vector3 targetWorldPos = Tilemap.GetCellCenterWorld(targetCell);

        // --- ДВИЖЕНИЕ СТРОГО ПО ПРЯМОЙ ---
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

        // --- ПРОВЕРКА ДОСТИЖЕНИЯ ЦЕНТРА КЛЕТКИ ---
        if (Vector3.Distance(transform.position, targetWorldPos) <= 0.001f)
        {
            // Как только точно встали на клетку, проверяем фундамент
            CheckForFoundation(targetCell);

            waypointIndex++; // Берем следующую точку пути

            // Если точки в текущем маршруте закончились — мы на перекрестке
            if (waypointIndex >= currentPath.Count)
            {
                StartCoroutine(HandleCrossroadRoutine(targetCell));
            }
        }
    }

    private IEnumerator HandleCrossroadRoutine(Vector3Int crossroadCell)
    {
        isWaiting = true; // Блокируем Update движения

        // Если вернулись на стартовую ноду — засчитываем круг
        if (crossroadCell == startNode)
        {
            lapsCount++;
        }

        // Ждем заданное время, если оно больше нуля
        if (crossroadWaitTime > 0)
        {
            yield return new WaitForSeconds(crossroadWaitTime);
        }

        // Запрашиваем новый маршрут у перекрестка
        RequestNextRoute(crossroadCell);

        isWaiting = false; // Разблокируем движение по новому пути
    }

    private void RequestNextRoute(Vector3Int currentGridPos)
    {
        if (FILL_MAP_v4.GlobalWaypoints.TryGetValue(currentGridPos, out CoordinateSwitcher switcher))
        {
            currentPath = switcher.GetActivePath();
            waypointIndex = 0;

            // ПРИМЕЧАНИЕ: Если генератор путей (GetActivePath) возвращает маршрут, 
            // где ПЕРВАЯ точка — это текущий перекресток, раскомментируй строку ниже, 
            // чтобы герой не пытался идти в ту точку, где уже стоит:
            // if (currentPath.Count > 0 && currentPath[0] == currentGridPos) waypointIndex = 1;

            isMoving = true;
        }
        else
        {
            isMoving = false;
            Debug.LogWarning($"Тупик! На клетке {currentGridPos} нет знака. Отряд потерялся!");
        }
    }

    public int Round()
    {
        return lapsCount;
    }

    private void CheckForFoundation(Vector3Int cellPos)
    {
        // Выдаем карту, если клетка новая и содержит фундамент
        if (cellPos != lastCheckedCell && FILL_MAP_v4.FoundationCells.Contains(cellPos))
        {
            lastCheckedCell = cellPos;
            if (HandManager != null)
            {
                HandManager.GiveRandomCardFromPool();
            }
        }
    }
}