using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;


public class FILL_MAP_v4 : MonoBehaviour
{



    [Header("Настройки сложности")]
    [Range(1, 10)] public int difficultyLevel = 1; // Уровень сложности карты

    [Header("Настройки фундаментов")]
    [Range(0, 5)] public int minFoundationsPerRoad = 1; // Минимальное число на одном отрезке пути
    [Range(1, 10)] public int maxFoundationsPerRoad = 3; // Максимальное число на одном отрезке пути

    [Header("Визуализация границ")]
    public LineRenderer borderLineRenderer;

    // Расширим карту, чтобы внешним маршрутам было где развернуться
    [Header("Настройки карты")]
    public int mapWidth = 40;
    public int mapHeight = 40;

    [Header("Постобработка")]
    public VoidDecorator voidDecorator;

    // Глобальный реестр зон влияния (Клетка дороги -> Тайлы её пустоты)
    public Dictionary<Vector3Int, UnityEngine.Tilemaps.TileBase[]> territoryMap = new Dictionary<Vector3Int, UnityEngine.Tilemaps.TileBase[]>();

    // Список для хранения объектов, чтобы мы могли их удалить при неудачной генерации
    private List<GameObject> tempMapObjects = new List<GameObject>();

    public static Dictionary<Vector3Int, ScriptableObject> cellOwners = new Dictionary<Vector3Int, ScriptableObject>();

    public Tilemap Map;
    public TileBase VoidTile;
    
    public TileBase foundationTile;// Тайл "особой зоны" для строительства
    [Header("Настройки Героя (Внутренний круг)")]
    public List<HeroData> availableHeroes;
    public HeroData selectedHero;          // Наш Лидер отряда
    public List<HeroData> activeSquad;     // ОСТАЛЬНОЙ ОТРЯД (спутники лидера, от 0 до 3 героев)

    [Header("Настройки Фракций (Внешний круг)")]
    public List<FactionData> availableFactions;
    public List<FactionData> activeFactions;

    public GameObject start;
    public GameObject signpost;


    public static HashSet<Vector3Int> FoundationCells = new HashSet<Vector3Int>(); // Реестр фундаментов
    public static Dictionary<Vector3Int, CoordinateSwitcher> GlobalWaypoints = new Dictionary<Vector3Int, CoordinateSwitcher>();
    private HashSet<Vector3Int> globalOccupiedCells = new HashSet<Vector3Int>();



    Vector3Int Vector_Start;
    private bool generation_roadmap() // Теперь это private bool!
    {
        Debug.Log("Генерация: Активация Внешнего Кольца");
        GlobalWaypoints.Clear();
        globalOccupiedCells.Clear();
        territoryMap.Clear();
        cellOwners.Clear();

        // --- ВЕРНУЛИ ЗАЛИВКУ ВОЙДОМ ---
        int overscan = 20;

        for (int x = -overscan; x < mapWidth + overscan; x++)
        {
            for (int y = -overscan; y < mapHeight + overscan; y++)
            {
                // Если используешь Vector3Int для координат:
                Vector3Int pos = new Vector3Int(x, y, 0);
                Map.SetTile(pos, VoidTile); // voidTile - переменная твоего тайла пустоты
            }
        }

        // Задаем отступы (сделаем их чуть меньше для компактной карты)
        // --- КОНТРОЛЬ ДЛИНЫ ДОРОГ ---
        // На 1 уровне margin=12 (короткие дороги в центре). На 5 уровне margin=4 (длинные дороги по краям).
        int margin = Mathf.Max(3, 14 - (difficultyLevel * 2));
        int randomOffset = 4;

        // 1. Левый нижний угол (Старт)
        Vector_Start = new Vector3Int(
            margin + Random.Range(0, randomOffset),
            margin + Random.Range(0, randomOffset), 0);
        // ИЗМЕНЕНО: Используем Map.GetCellCenterWorld для спавна
        GameObject start_Object = Instantiate(start, Map.GetCellCenterWorld(Vector_Start), Quaternion.identity);
        tempMapObjects.Add(start_Object);

        // 2. Левый верхний угол (Знак 1)
        Vector3Int Vector_signpost1 = new Vector3Int(
            margin + Random.Range(0, randomOffset),
            mapHeight - margin - Random.Range(0, randomOffset), 0);
        // ИЗМЕНЕНО: Используем Map.GetCellCenterWorld
        GameObject signpost1_Object = Instantiate(signpost, Map.GetCellCenterWorld(Vector_signpost1), Quaternion.identity);
        tempMapObjects.Add(signpost1_Object);

        // 3. Правый верхний угол (Знак 2)
        Vector3Int Vector_signpost2 = new Vector3Int(
            mapWidth - margin - Random.Range(0, randomOffset),
            mapHeight - margin - Random.Range(0, randomOffset), 0);
        // ИЗМЕНЕНО: Используем Map.GetCellCenterWorld
        GameObject signpost2_Object = Instantiate(signpost, Map.GetCellCenterWorld(Vector_signpost2), Quaternion.identity);
        tempMapObjects.Add(signpost2_Object);

        // 4. Правый нижний угол (Знак 3)
        Vector3Int Vector_signpost3 = new Vector3Int(
            mapWidth - margin - Random.Range(0, randomOffset),
            margin + Random.Range(0, randomOffset), 0);
        // ИЗМЕНЕНО: Используем Map.GetCellCenterWorld
        GameObject signpost3_Object = Instantiate(signpost, Map.GetCellCenterWorld(Vector_signpost3), Quaternion.identity);
        tempMapObjects.Add(signpost3_Object);

        // --- ВЫЧИСЛЯЕМ ЦЕНТР НАШЕГО ГОРОДА ---
        Vector2 loopCenter = new Vector2(mapWidth / 2f, mapHeight / 2f); // Центр теперь всегда стабилен

        // --- РАСПРЕДЕЛЕНИЕ ФРАКЦИЙ ПО КАРТЕ ---
        FactionData facS1 = null; // Отрезок: Старт -> Знак 1
        FactionData facS2 = null; // Отрезок: Знак 1 -> Знак 2
        FactionData facS3 = null; // Отрезок: Знак 2 -> Знак 3
        FactionData facS4 = null; // Отрезок: Знак 3 -> Старт (Обычно делаем безопасным)

        if (activeFactions != null && activeFactions.Count > 0)
        {
            if (activeFactions.Count == 1)
            {
                // 1 фракция: Занимает два центральных отрезка (Знак 1 -> 2 -> 3)
                facS2 = activeFactions[0];
                facS3 = activeFactions[0];
            }
            else if (activeFactions.Count == 2)
            {
                // 2 фракции: Делят центральные отрезки пополам
                facS2 = activeFactions[0];
                facS3 = activeFactions[1];
            }
            else if (activeFactions.Count >= 3)
            {
                // 3 фракции: Занимают всё, кроме последнего пути домой!
                facS1 = activeFactions[0];
                facS2 = activeFactions[1];
                facS3 = activeFactions[2];
            }
        }

        // --- ПРОВЕРКА УСПЕШНОСТИ ПУТЕЙ ---

        // 1. Выход из лагеря (Передаем facS1)
        if (!BuildSmartRoutes(start_Object.GetComponent<CoordinateSwitcher>(), Vector_Start, Vector_signpost1, loopCenter, facS1)) return false;
        GlobalWaypoints.Add(Vector_Start, start_Object.GetComponent<CoordinateSwitcher>());

        // 2. Первая трасса (Передаем facS2)
        if (!BuildSmartRoutes(signpost1_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost1, Vector_signpost2, loopCenter, facS2)) return false;
        GlobalWaypoints.Add(Vector_signpost1, signpost1_Object.GetComponent<CoordinateSwitcher>());

        // 3. Вторая трасса (Передаем facS3)
        if (!BuildSmartRoutes(signpost2_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost2, Vector_signpost3, loopCenter, facS3)) return false;
        GlobalWaypoints.Add(Vector_signpost2, signpost2_Object.GetComponent<CoordinateSwitcher>());

        // 4. Возвращение в лагерь (Передаем facS4)
        if (!BuildSmartRoutes(signpost3_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost3, Vector_Start, loopCenter, facS4)) return false;
        GlobalWaypoints.Add(Vector_signpost3, signpost3_Object.GetComponent<CoordinateSwitcher>());

        CameraMovement camScript = Camera.main.GetComponent<CameraMovement>();
        if (camScript != null)
        {
            camScript.SetupCameraForMap(40, 40); // Передай сюда реальные переменные ширины и высоты твоей карты
        }

        // Запускаем декорацию пустоты
        if (voidDecorator != null)
        {
            // Передаем массив тайлов героя последним аргументом
            voidDecorator.Decorate(mapWidth, mapHeight, globalOccupiedCells, territoryMap, selectedHero.territoryVoidTiles);
        }

        GridGameController.Instance.InitializeGrid(mapWidth, mapHeight);

        DrawMapBorder();

        return true; // Всё построилось идеально!
    }


    // ДОБАВИЛ ПАРАМЕТР: bool generatePathB = true
    private bool BuildSmartRoutes(CoordinateSwitcher switcher, Vector3Int startPoint, Vector3Int endPoint, Vector2 loopCenter, FactionData segmentFaction)
    {
        // 1. Внутренний круг А (Оставляем как было)
        List<Vector3Int> pathA = FindPathAStar(startPoint, endPoint, new HashSet<Vector3Int>(), false, loopCenter);
        if (pathA == null || pathA.Count == 0) return false;

        DrawAndRegisterPath(pathA, selectedHero.heroRoadTile, selectedHero.territoryVoidTiles, selectedHero);



        int minA = Mathf.Max(0, minFoundationsPerRoad + selectedHero.bonusFoundations);
        int maxA = Mathf.Max(minA, maxFoundationsPerRoad + selectedHero.bonusFoundations);
        GenerateFoundations(pathA, minA, maxA);

        switcher.pathA = pathA;

        // ==========================================
        // 2. ВНЕШНИЙ КРУГ "Б" (Земли Фракции)
        // ==========================================
        if (segmentFaction != null) // ПРОВЕРЯЕМ: ЕСТЬ ЛИ ФРАКЦИЯ НА ЭТОМ ОТРЕЗКЕ?
        {
            Vector3Int mergePoint = endPoint;
            if (pathA.Count > 3) mergePoint = pathA[pathA.Count - 3];

            HashSet<Vector3Int> thickObstacles = GetThickObstacles(pathA, startPoint, endPoint, mergePoint);

            List<Vector3Int> pathB = FindPathAStar(startPoint, mergePoint, thickObstacles, true, loopCenter);
            if (pathB == null || pathB.Count == 0) return false;

            if (pathB.Count > 0)
            {
                int mergeIndex = pathA.IndexOf(mergePoint);
                if (mergeIndex != -1)
                {
                    for (int i = mergeIndex + 1; i < pathA.Count; i++) pathB.Add(pathA[i]);
                }
            }

            // ИСПОЛЬЗУЕМ ДАННЫЕ ИМЕННО ЭТОЙ ФРАКЦИИ (segmentFaction)
            if (segmentFaction != null)
            {
                DrawAndRegisterPath(pathB, segmentFaction.factionRoadTile, segmentFaction.territoryVoidTiles, segmentFaction);
            }
            else
            {
                DrawAndRegisterPath(pathB, selectedHero.heroRoadTile, selectedHero.territoryVoidTiles, selectedHero);
            }

            int minB = Mathf.Max(0, minFoundationsPerRoad + segmentFaction.extraFoundations);
            int maxB = Mathf.Max(minB, maxFoundationsPerRoad + segmentFaction.extraFoundations);
            GenerateFoundations(pathB, minB, maxB);

            switcher.pathB = pathB;
        }
        else
        {
            // Безопасная зона (фракции нет)
            switcher.pathB = pathA;
        }

        return true;
    }

    // Создает буферную зону вокруг маршрута А
    private HashSet<Vector3Int> GetThickObstacles(List<Vector3Int> path, Vector3Int startPoint, Vector3Int endPoint, Vector3Int mergePoint)
    {
        HashSet<Vector3Int> thick = new HashSet<Vector3Int>();
        if (path == null) return thick;

        foreach (Vector3Int p in path)
        {
            // Если мы рядом со стартом, финишем ИЛИ точкой слияния — НЕ создаем препятствия!
            if (Vector3Int.Distance(p, startPoint) <= 2 ||
                Vector3Int.Distance(p, endPoint) <= 2 ||
                Vector3Int.Distance(p, mergePoint) <= 2)
            {
                continue; // Просто пропускаем эту клетку, оставляя зону чистой
            }

            // Добавляем саму клетку и 8 соседей вокруг нее (радиус 1)
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    thick.Add(new Vector3Int(p.x + x, p.y + y, 0));
                }
            }
        }
        return thick;
    }

    // ДОБАВЛЕНО: Новый аргумент ScriptableObject owner в конце
    void DrawAndRegisterPath(List<Vector3Int> path, UnityEngine.Tilemaps.TileBase currentTile, UnityEngine.Tilemaps.TileBase[] voidTiles, ScriptableObject owner)
    {
        foreach (Vector3Int p in path)
        {
            if (globalOccupiedCells.Contains(p) && currentTile != selectedHero.heroRoadTile) continue;

            Map.SetTile(p, currentTile);
            globalOccupiedCells.Add(p);

            // --- НОВОЕ: ЗАПИСЫВАЕМ ВЛАДЕЛЬЦА ТЕРРИТОРИИ ---
            if (owner != null)
            {
                // Проверяем, нет ли уже владельца у этой клетки (на случай перекрестков)
                if (!cellOwners.ContainsKey(p))
                {
                    cellOwners.Add(p, owner);
                }
                else if (owner is HeroData)
                {
                    // Герой имеет приоритет на перекрестках! Если клетка уже чья-то, 
                    // но сейчас мы рисуем дорогу героя, перезаписываем владельца
                    cellOwners[p] = owner;
                }
            }
            // ----------------------------------------------

            // Запоминаем тайлы пустоты для этой клетки
            if (voidTiles != null && voidTiles.Length > 0)
            {
                territoryMap[p] = voidTiles;
            }
        }
    }

    private class Node
    {
        public Vector3Int pos;
        public Node parent;
        public int gCost;
        public int hCost;
        public int fCost { get { return gCost + hCost; } }
    }

    // Измененный алгоритм с учетом центра масс
    private List<Vector3Int> FindPathAStar(Vector3Int startPos, Vector3Int targetPos, HashSet<Vector3Int> obstacles, bool isOuterRoute, Vector2 loopCenter)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();

        Node startNode = new Node { pos = startPos, gCost = 0, hCost = GetDistance(startPos, targetPos) };
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.pos);

            if (currentNode.pos == targetPos)
            {
                return RetracePath(startNode, currentNode);
            }

            Vector3Int[] neighbors = {
                new Vector3Int(currentNode.pos.x + 1, currentNode.pos.y, 0),
                new Vector3Int(currentNode.pos.x - 1, currentNode.pos.y, 0),
                new Vector3Int(currentNode.pos.x, currentNode.pos.y + 1, 0),
                new Vector3Int(currentNode.pos.x, currentNode.pos.y - 1, 0)
            };

            foreach (Vector3Int neighborPos in neighbors)
            {

                if (neighborPos.x < 0 || neighborPos.x > mapWidth || neighborPos.y < 0 || neighborPos.y > mapHeight) continue;
                if (closedSet.Contains(neighborPos)) continue;

                if (neighborPos != targetPos && (globalOccupiedCells.Contains(neighborPos) || obstacles.Contains(neighborPos)))
                    continue;

                // --- ЛОГИКА ОСЕВОГО ВХОДА (ВЕКТОРНОЕ ВЫРАВНИВАНИЕ) ---
                // Проверяем дистанцию до финальной цели (перекрестка)
                float distToTargetEntry = Vector3Int.Distance(neighborPos, targetPos);

                if (distToTargetEntry <= 3) // "Коридор сближения" за 3 клетки до знака 
                {
                    // Если текущая клетка уже стоит на одной линии с целью по X
                    if (currentNode.pos.x == targetPos.x)
                    {
                        // Запрещаем соседей, которые пытаются уйти с этой линии X
                        if (neighborPos.x != targetPos.x) continue;
                    }
                    // Если текущая клетка уже стоит на одной линии с целью по Y
                    else if (currentNode.pos.y == targetPos.y)
                    {
                        // Запрещаем соседей, которые пытаются уйти с этой линии Y
                        if (neighborPos.y != targetPos.y) continue;
                    }
                }

                // Базовая цена шага
                int moveCost = 15;

                // --- ДИНАМИЧЕСКИЙ ШТРАФ ЗА ПОВОРОТ ---
                if (currentNode.parent != null)
                {
                    Vector3Int currentDirection = currentNode.pos - currentNode.parent.pos;
                    Vector3Int nextDirection = neighborPos - currentNode.pos;

                    if (currentDirection != nextDirection)
                    {
                        // Смотрим, насколько мы близки к конечной цели этого маршрута
                        float distToTarget = Vector3Int.Distance(neighborPos, targetPos);

                        // Если до цели меньше 6 клеток — мы в "зоне перекрестка", маневрируем свободно (штраф 0 или 5).
                        // Если мы далеко — мы на "трассе", держим прямую линию (штраф 30).
                        int turnPenalty = distToTarget < 6 ? 0 : 30;

                        moveCost += turnPenalty;
                    }
                }

                // --- МАГИЯ ХАОСА (ШУМ ПЕРЛИНА) ---
                float noise = Mathf.PerlinNoise(neighborPos.x * 0.2f, neighborPos.y * 0.2f);
                moveCost += (int)(noise * 25); // Накидываем до 25 очков штрафа за "плохой" рельеф

                // --- МАГИЯ ОТТАЛКИВАНИЯ ---
                // Если мы строим внешний маршрут Б, мы наказываем алгоритм за попытку уйти внутрь кольца
                if (isOuterRoute)
                {
                    float distToCenter = Vector2.Distance(new Vector2(neighborPos.x, neighborPos.y), loopCenter);

                    // Если клетка ближе 12 юнитов к центру, накидываем гигантский штраф
                    float penalty = Mathf.Max(0, 15f - distToCenter);
                    moveCost += (int)(penalty * 5); // Чем ближе к центру, тем "дороже" туда наступить
                }

                int newMovementCostToNeighbor = currentNode.gCost + moveCost;

                Node neighborNode = openSet.Find(n => n.pos == neighborPos);
                if (neighborNode == null || newMovementCostToNeighbor < neighborNode.gCost)
                {
                    if (neighborNode == null)
                    {
                        neighborNode = new Node { pos = neighborPos };
                        openSet.Add(neighborNode);
                    }
                    neighborNode.gCost = newMovementCostToNeighbor;
                    neighborNode.hCost = GetDistance(neighborPos, targetPos);
                    neighborNode.parent = currentNode;
                }
            }
        }

        Debug.LogWarning($"Путь не найден! Возможно, карте не хватает места ({mapWidth}x{mapHeight}).");
        return new List<Vector3Int>();
    }

    private List<Vector3Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode.pos);
            currentNode = currentNode.parent;
        }
        path.Add(startNode.pos);
        path.Reverse();
        return path;
    }

    private int GetDistance(Vector3Int nodeA, Vector3Int nodeB)
    {
        int dstX = Mathf.Abs(nodeA.x - nodeB.x);
        int dstY = Mathf.Abs(nodeA.y - nodeB.y);
        return (dstX + dstY) * 10;
    }
public Vector3Int Get_Start_road()
    {
        return Vector_Start;
    }

    private void GenerateFoundations(List<Vector3Int> path, int minCount, int maxCount)
    {
        if (path == null || path.Count < 5) return;

        // 1. Вычисляем целевое количество фундаментов для этого куска дороги
        int targetCount = Random.Range(minCount, maxCount + 1);
        int placedCount = 0;

        // 2. Собираем все доступные клетки (с отступом от начала и конца, чтобы не застраивать перекрестки)
        List<Vector3Int> validCells = new List<Vector3Int>();
        for (int i = 2; i < path.Count - 2; i++)
        {
            validCells.Add(path[i]);
        }

        // 3. Пытаемся расставить нужное количество (с лимитом попыток от зависания)
        int attempts = 0;
        while (placedCount < targetCount && validCells.Count > 0 && attempts < 50)
        {
            attempts++;

            // Берем случайную доступную клетку
            int randomIndex = Random.Range(0, validCells.Count);
            Vector3Int cell = validCells[randomIndex];

            // 4. Проверяем, нет ли уже рядом другого фундамента
            bool isTooClose = false;
            foreach (Vector3Int existingFoundation in FoundationCells)
            {
                // Если расстояние меньше 1.5, значит клетки соседние (по прямой или диагонали)
                if (Vector3.Distance(cell, existingFoundation) < 1.5f)
                {
                    isTooClose = true;
                    break;
                }
            }

            // Если место свободно и вокруг нет соседей — строим фундамент
            if (!isTooClose)
            {
                Map.SetTile(cell, foundationTile);
                FoundationCells.Add(cell);
                placedCount++;
            }

            // В любом случае удаляем эту клетку из списка кандидатов, чтобы не проверять ее дважды
            validCells.RemoveAt(randomIndex);
        }
    }

    // ЭТОТ МЕТОД ТЕПЕРЬ НУЖНО ВЫЗЫВАТЬ ИЗ Start_scene ВМЕСТО generation_roadmap!
    public void StartGenerationWithRetries()
    {
        int maxAttempts = 10;
        for (int i = 1; i <= maxAttempts; i++)
        {
            Debug.Log($"--- Попытка генерации карты #{i} ---");
            CleanupMap();

            // Если генерация вернула true, значит всё построилось без тупиков
            if (generation_roadmap())
            {
                Debug.Log($"УСПЕХ! Карта сгенерирована (Попытка {i}).");
                return;
            }
            else
            {
                Debug.LogWarning($"Попытка {i} провалилась. Перестройка...");
            }
        }
        Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА: Не удалось сгенерировать карту за 10 попыток!");
    }

    private void CleanupMap()
    {
        GlobalWaypoints.Clear();
        globalOccupiedCells.Clear();
        FoundationCells.Clear();

        // Удаляем знаки и старт от неудачной генерации
        foreach (var obj in tempMapObjects)
        {
            if (obj != null) Destroy(obj);
        }
        tempMapObjects.Clear();
    }

    [Header("Текущий пул карт сессии")]
    public List<CardData> sessionCardPool = new List<CardData>();

    // Метод для подготовки пула (вызывай его в StartGenerationWithRetries)
    public void PrepareSessionCardPool()
    {
        sessionCardPool.Clear();

        // 1. Добавляем ГЛАВНЫЕ карты Лидера отряда
        if (selectedHero != null && selectedHero.heroMainCards != null)
        {
            sessionCardPool.AddRange(selectedHero.heroMainCards);

            // Опционально: Лидер может добавлять и свои неглавные карты тоже!
            if (selectedHero.heroSupportCards != null)
            {
                sessionCardPool.AddRange(selectedHero.heroSupportCards);
            }
        }

        // 2. ДОБАВЛЕНО: Пробегаемся по всему ОТРЯДУ и забираем их НЕГЛАВНЫЕ карты
        if (activeSquad != null && activeSquad.Count > 0)
        {
            foreach (HeroData companion in activeSquad)
            {
                if (companion != null && companion.heroSupportCards != null)
                {
                    sessionCardPool.AddRange(companion.heroSupportCards);
                    Debug.Log($"Спутник {companion.heroName} добавил свои карты поддержки в пул.");
                }
            }
        }

        // 3. Добавляем карты ВСЕХ активных фракций на карте (как в прошлом шаге)
        if (activeFactions != null && activeFactions.Count > 0)
        {
            foreach (FactionData faction in activeFactions)
            {
                if (faction != null && faction.factionCards != null)
                {
                    sessionCardPool.AddRange(faction.factionCards);
                }
            }
        }

        Debug.Log($"Пул карт сессии сформирован! Всего доступно видов карт: {sessionCardPool.Count}");
    }

    void DrawMapBorder()
    {
        if (borderLineRenderer == null)
        {
            Debug.LogError("DLS: Не назначен LineRenderer для границы карты в FILL_MAP_v4!");
            return;
        }

        // Нам нужны 4 угловые точки. 
        // Если карта генерируется от (0,0) до (mapWidth, mapHeight):

        Vector3[] corners = new Vector3[4];

        // ВАЖНО: мы ставим Z в -0.1f, чтобы рамка была чуть ближе к камере, 
        // чем тайлы на Z=0, и не "мерцала" сквозь них.
        float zOffset = -0.1f;

        corners[0] = new Vector3(0, 0, zOffset);                     // Левый нижний
        corners[1] = new Vector3(0, mapHeight, zOffset);             // Левый верхний
        corners[2] = new Vector3(mapWidth, mapHeight, zOffset);       // Правый верхний
        corners[3] = new Vector3(mapWidth, 0, zOffset);             // Правый нижний

        // Указываем LineRenderer, что у нас 4 точки
        borderLineRenderer.positionCount = 4;

        // Передаем массив координат
        borderLineRenderer.SetPositions(corners);

        // На всякий случай включаем loop через код
        borderLineRenderer.loop = true;
    }
}

