using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Character_move : MonoBehaviour
{
    public Tilemap Tilemap;
    public float speed = 2f;
    public float turnSpeed = 10f; // Скорость поворота героя
    private int R = 0; // счетчик кругов

    private List<Vector3Int> currentPath;
    private int waypointIndex = 0;
    private Vector3Int startNode; // Запоминаем старт, чтобы знать, когда засчитывать круг
    private bool isMoving = false;

    // Этот метод мы вызовем из спавнера, чтобы "пнуть" героя в путь
    public void StartJourney(Vector3Int startPos)
    {
        startNode = startPos;
        // Запрашиваем первый путь от стартовой точки
        RequestNextRoute(startPos);
    }

    public void Update()
    {
        // Если пути нет или мы стоим — ничего не делаем
        if (!isMoving || currentPath == null || currentPath.Count == 0) return;

        // Переводим координату клетки в мировые координаты
        Vector3 targetWorldPos = Tilemap.CellToWorld(currentPath[waypointIndex]);

        // --- ДВИЖЕНИЕ ---
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

        // --- ВРАЩЕНИЕ (То, что ты просил!) ---
      /*  Vector3 direction = targetWorldPos - transform.position;
        if (direction != Vector3.zero)
        {
            // Вычисляем, куда нужно смотреть
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Важная магия: сохраняем твой наклон в 45 градусов по X, чтобы моделька не "ложилась" лицом в пол
            targetRotation = Quaternion.Euler(45, targetRotation.eulerAngles.y, 0);

            // Плавно крутим персонажа
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }*/

        // --- ПРОВЕРКА ДОСТИЖЕНИЯ ТОЧКИ ---
        if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
        {
            waypointIndex++; // Берем следующую клетку пути

            // Если дошли до конца текущего куска дороги
            if (waypointIndex >= currentPath.Count)
            {
                isMoving = false;
                Vector3Int currentCell = currentPath[currentPath.Count - 1];

                // СЧЕТЧИК КРУГОВ: Если мы пришли обратно на стартовую клетку — это круг!
                if (currentCell == startNode)
                {
                    R++;
                }

                // Запрашиваем новый маршрут у перекрестка, на котором стоим
                RequestNextRoute(currentCell);
            }
        }
    }

    // Запрос маршрута из нашего Глобального Реестра
    private void RequestNextRoute(Vector3Int currentGridPos)
    {
        // Находим перекресток по координатам (FILL_MAP_v3 - это скрипт генерации карты)
        if (FILL_MAP_v4.GlobalWaypoints.TryGetValue(currentGridPos, out CoordinateSwitcher switcher))
        {
            currentPath = switcher.GetActivePath(); // Берем активный в данный момент путь
            waypointIndex = 0;
            isMoving = true;
        }
        else
        {
            Debug.LogWarning($"Тупик! На клетке {currentGridPos} нет знака. Герой потерялся!");
        }
    }

    public int Round()
    {
        return R;
    }
}