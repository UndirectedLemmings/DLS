using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FILL_MAP_v4 : MonoBehaviour
{
    [Header("Визуализация границ")]
    public LineRenderer borderLineRenderer;

    // Расширим карту, чтобы внешним маршрутам было где развернуться
    [Header("Настройки карты")]
    public int mapWidth = 40;
    public int mapHeight = 40;

    [Header("Постобработка")]
    public VoidDecorator voidDecorator;

    [Header("Настройки сложности")]
    [Range(1, 10)] public int difficultyLevel = 1; // Уровень сложности карты

    [Header("Настройки фундаментов")]
    [Range(0, 5)] public int minFoundationsPerRoad = 1; // Минимальное число на одном отрезке пути
    [Range(1, 10)] public int maxFoundationsPerRoad = 3; // Максимальное число на одном отрезке пути

   

    // --- НОВОЕ: ТРИ РАЗДЕЛЬНЫХ СЛОЯ ВМЕСТО ОДНОГО ---
    [Header("Слои Карты (Tilemaps)")]
    public Tilemap landscapeMap;   // Слой 1: Ландшафт (Void)
    public Tilemap roadsMap;       // Слой 2: Дороги
    public Tilemap foundationsMap; // Слой 3: Фундаменты
    // ------------------------------------------------
    [Header("тайлы карты")]
    public TileBase VoidTile;
    public TileBase foundationTile;// Тайл "особой зоны" для строительства// переместить
    public GameObject start;
    public GameObject signpost;


    [Header("Настройки Героя (Внутренний круг)")]
    public List<HeroData> availableHeroes;
    public HeroData activeLeader;          // Наш Лидер отряда
    public List<HeroData> activeSquad;     // ОСТАЛЬНОЙ ОТРЯД (спутники лидера, от 0 до 3 героев)

    [Header("Настройки Фракций (Внешний круг)")]
    public List<FactionData> activeFactions;

    public Dictionary<Vector3Int, UnityEngine.Tilemaps.TileBase[]> territoryMap = new Dictionary<Vector3Int, UnityEngine.Tilemaps.TileBase[]>();// Глобальный реестр зон влияния (Клетка дороги -> Тайлы её пустоты)
    public static Dictionary<Vector3Int, ScriptableObject> cellOwners = new Dictionary<Vector3Int, ScriptableObject>();// Список для хранения объектов, чтобы мы могли их удалить при неудачной генерации
    public static HashSet<Vector3Int> FoundationCells = new HashSet<Vector3Int>(); // Реестр фундаментов
    public static HashSet<Vector3Int> IntersectionCells = new HashSet<Vector3Int>();// Реестр всех перекрестков на карте
    public static Dictionary<Vector3Int, CoordinateSwitcher> GlobalWaypoints = new Dictionary<Vector3Int, CoordinateSwitcher>();
    private HashSet<Vector3Int> globalOccupiedCells = new HashSet<Vector3Int>();
    private List<GameObject> tempMapObjects = new List<GameObject>();

    Vector3Int Vector_Start;

    private bool generation_roadmap()
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
                Vector3Int pos = new Vector3Int(x, y, 0);
                // --- НОВОЕ: Рисуем пустоту только на слое ландшафта ---
                landscapeMap.SetTile(pos, VoidTile);
            }
        }

        // Задаем отступы (сделаем их чуть меньше для компактной карты)
        int margin = Mathf.Max(3, 14 - (difficultyLevel * 2));
        int randomOffset = 4;

        // 1. Левый нижний угол (Старт)
        Vector_Start = new Vector3Int(
            margin + Random.Range(0, randomOffset),
            margin + Random.Range(0, randomOffset), 0);
        // --- НОВОЕ: Берем координаты из слоя дорог ---
        GameObject start_Object = Instantiate(start, roadsMap.GetCellCenterWorld(Vector_Start), Quaternion.identity);
        tempMapObjects.Add(start_Object);

        // 2. Левый верхний угол (Знак 1)
        Vector3Int Vector_signpost1 = new Vector3Int(
            margin + Random.Range(0, randomOffset),
            mapHeight - margin - Random.Range(0, randomOffset), 0);
        GameObject signpost1_Object = Instantiate(signpost, roadsMap.GetCellCenterWorld(Vector_signpost1), Quaternion.identity);
        tempMapObjects.Add(signpost1_Object);

        // 3. Правый верхний угол (Знак 2)
        Vector3Int Vector_signpost2 = new Vector3Int(
            mapWidth - margin - Random.Range(0, randomOffset),
            mapHeight - margin - Random.Range(0, randomOffset), 0);
        GameObject signpost2_Object = Instantiate(signpost, roadsMap.GetCellCenterWorld(Vector_signpost2), Quaternion.identity);
        tempMapObjects.Add(signpost2_Object);

        // 4. Правый нижний угол (Знак 3)
        Vector3Int Vector_signpost3 = new Vector3Int(
            mapWidth - margin - Random.Range(0, randomOffset),
            margin + Random.Range(0, randomOffset), 0);
        GameObject signpost3_Object = Instantiate(signpost, roadsMap.GetCellCenterWorld(Vector_signpost3), Quaternion.identity);
        tempMapObjects.Add(signpost3_Object);

        // --- ВЫЧИСЛЯЕМ ЦЕНТР НАШЕГО ГОРОДА ---
        Vector2 loopCenter = new Vector2(mapWidth / 2f, mapHeight / 2f);

        // --- РАСПРЕДЕЛЕНИЕ ФРАКЦИЙ ПО КАРТЕ ---
        FactionData facS1 = null;
        FactionData facS2 = null;
        FactionData facS3 = null;
        FactionData facS4 = null;

        if (activeFactions != null && activeFactions.Count > 0)
        {
            if (activeFactions.Count == 1)
            {
                facS2 = activeFactions[0];
                facS3 = activeFactions[0];
            }
            else if (activeFactions.Count == 2)
            {
                facS2 = activeFactions[0];
                facS3 = activeFactions[1];
            }
            else if (activeFactions.Count >= 3)
            {
                facS2 = activeFactions[0];
                facS3 = activeFactions[1];
                facS4 = activeFactions[2];
            }
        }

        // --- ПРОВЕРКА УСПЕШНОСТИ ПУТЕЙ ---
        if (!BuildSmartRoutes(start_Object.GetComponent<CoordinateSwitcher>(), Vector_Start, Vector_signpost1, loopCenter, facS1)) return false;
        GlobalWaypoints.Add(Vector_Start, start_Object.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost1_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost1, Vector_signpost2, loopCenter, facS2)) return false;
        GlobalWaypoints.Add(Vector_signpost1, signpost1_Object.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost2_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost2, Vector_signpost3, loopCenter, facS3)) return false;
        GlobalWaypoints.Add(Vector_signpost2, signpost2_Object.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost3_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost3, Vector_Start, loopCenter, facS4)) return false;
        GlobalWaypoints.Add(Vector_signpost3, signpost3_Object.GetComponent<CoordinateSwitcher>());

        CameraMovement camScript = Camera.main.GetComponent<CameraMovement>();
        if (camScript != null)
        {
            camScript.SetupCameraForMap(40, 40);
        }

        if (voidDecorator != null)
        {
            voidDecorator.Decorate(mapWidth, mapHeight, globalOccupiedCells, territoryMap, activeLeader.territoryVoidTiles);
        }

        GridGameController.Instance.InitializeGrid(mapWidth, mapHeight);

        DrawMapBorder();

        return true;
    }


    private bool BuildSmartRoutes(CoordinateSwitcher switcher, Vector3Int startPoint, Vector3Int endPoint, Vector2 loopCenter, FactionData segmentFaction)
    {
        List<Vector3Int> pathA = FindPathAStar(startPoint, endPoint, new HashSet<Vector3Int>(), false, loopCenter);
        if (pathA == null || pathA.Count == 0) return false;

        DrawAndRegisterPath(pathA, activeLeader.heroRoadTile, activeLeader.territoryVoidTiles, activeLeader);

        int minA = Mathf.Max(0, minFoundationsPerRoad + activeLeader.bonusFoundations);
        int maxA = Mathf.Max(minA, maxFoundationsPerRoad + activeLeader.bonusFoundations);
        GenerateFoundations(pathA, minA, maxA);

        switcher.pathA = pathA;

        if (segmentFaction != null)
        {
            Vector3Int mergePoint = endPoint;
            if (pathA.Count > 3) mergePoint = pathA[pathA.Count - 3];

            IntersectionCells.Add(startPoint);
            IntersectionCells.Add(endPoint);
            IntersectionCells.Add(mergePoint);

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

            if (segmentFaction != null)
            {
                DrawAndRegisterPath(pathB, segmentFaction.factionRoadTile, segmentFaction.territoryVoidTiles, segmentFaction);
            }
            else
            {
                DrawAndRegisterPath(pathB, activeLeader.heroRoadTile, activeLeader.territoryVoidTiles, activeLeader);
            }

            int minB = Mathf.Max(0, minFoundationsPerRoad + segmentFaction.extraFoundations);
            int maxB = Mathf.Max(minB, maxFoundationsPerRoad + segmentFaction.extraFoundations);
            GenerateFoundations(pathB, minB, maxB);

            switcher.pathB = pathB;

            GameObject roadManagerObj = new GameObject($"RoadManager_{segmentFaction.name}");
            RoadSegmentManager roadManager = roadManagerObj.AddComponent<RoadSegmentManager>();
            roadManager.ownerFaction = segmentFaction;

            List<Vector2Int> roadCells2D = new List<Vector2Int>();
            foreach (Vector3Int cell in pathB)
            {
                roadCells2D.Add(new Vector2Int(cell.x, cell.y));
            }
            roadManager.roadCells = roadCells2D;
            roadManagerObj.transform.SetParent(this.transform);

            Debug.Log($"DLS: Менеджер дороги для фракции {segmentFaction.name} успешно создан! Клеток: {roadManager.roadCells.Count}");
        }
        else
        {
            switcher.pathB = pathA;
        }

        return true;
    }

    private HashSet<Vector3Int> GetThickObstacles(List<Vector3Int> path, Vector3Int startPoint, Vector3Int endPoint, Vector3Int mergePoint)
    {
        HashSet<Vector3Int> thick = new HashSet<Vector3Int>();
        if (path == null) return thick;

        foreach (Vector3Int p in path)
        {
            if (Vector3Int.Distance(p, startPoint) <= 2 ||
                Vector3Int.Distance(p, endPoint) <= 2 ||
                Vector3Int.Distance(p, mergePoint) <= 2)
            {
                continue;
            }

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

    void DrawAndRegisterPath(List<Vector3Int> path, UnityEngine.Tilemaps.TileBase currentTile, UnityEngine.Tilemaps.TileBase[] voidTiles, ScriptableObject owner)
    {
        foreach (Vector3Int p in path)
        {
            if (globalOccupiedCells.Contains(p) && currentTile != activeLeader.heroRoadTile) continue;

            // 1. Рисуем дорогу на слое дорог (Слой 1)
            roadsMap.SetTile(p, currentTile);
            globalOccupiedCells.Add(p);

            // 2. Регистрация владельца
            if (owner != null)
            {
                if (!cellOwners.ContainsKey(p))
                {
                    cellOwners.Add(p, owner);
                }
                else if (owner is HeroData)
                {
                    cellOwners[p] = owner;
                }
            }

            // 3. Заполняем словарь территорий и КЛАДЕМ БИОМ ПОД ДОРОГУ (Слой 0)
            if (voidTiles != null && voidTiles.Length > 0)
            {
                territoryMap[p] = voidTiles;

                
                TileBase randomBiomeTile = voidTiles[Random.Range(0, voidTiles.Length)];
                landscapeMap.SetTile(p, randomBiomeTile);
                // ---------------------------------------------------------------------
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

                float distToTargetEntry = Vector3Int.Distance(neighborPos, targetPos);

                if (distToTargetEntry <= 3)
                {
                    if (currentNode.pos.x == targetPos.x)
                    {
                        if (neighborPos.x != targetPos.x) continue;
                    }
                    else if (currentNode.pos.y == targetPos.y)
                    {
                        if (neighborPos.y != targetPos.y) continue;
                    }
                }

                int moveCost = 15;

                if (currentNode.parent != null)
                {
                    Vector3Int currentDirection = currentNode.pos - currentNode.parent.pos;
                    Vector3Int nextDirection = neighborPos - currentNode.pos;

                    if (currentDirection != nextDirection)
                    {
                        float distToTarget = Vector3Int.Distance(neighborPos, targetPos);
                        int turnPenalty = distToTarget < 6 ? 0 : 30;
                        moveCost += turnPenalty;
                    }
                }

                float noise = Mathf.PerlinNoise(neighborPos.x * 0.2f, neighborPos.y * 0.2f);
                moveCost += (int)(noise * 25);

                if (isOuterRoute)
                {
                    float distToCenter = Vector2.Distance(new Vector2(neighborPos.x, neighborPos.y), loopCenter);
                    float penalty = Mathf.Max(0, 15f - distToCenter);
                    moveCost += (int)(penalty * 5);
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

        int targetCount = Random.Range(minCount, maxCount + 1);
        int placedCount = 0;

        List<Vector3Int> validCells = new List<Vector3Int>();
        for (int i = 2; i < path.Count - 2; i++)
        {
            validCells.Add(path[i]);
        }

        int attempts = 0;
        while (placedCount < targetCount && validCells.Count > 0 && attempts < 50)
        {
            attempts++;

            int randomIndex = Random.Range(0, validCells.Count);
            Vector3Int cell = validCells[randomIndex];

            bool isTooClose = false;
            foreach (Vector3Int existingFoundation in FoundationCells)
            {
                if (Vector3.Distance(cell, existingFoundation) < 1.5f)
                {
                    isTooClose = true;
                    break;
                }
            }

            if (!isTooClose)
            {
                // --- НОВОЕ: Рисуем фундаменты только на слое фундаментов ---
                foundationsMap.SetTile(cell, foundationTile);
                FoundationCells.Add(cell);
                placedCount++;
            }

            validCells.RemoveAt(randomIndex);
        }
    }

    public void StartGenerationWithRetries()
    {
        int maxAttempts = 10;
        for (int i = 1; i <= maxAttempts; i++)
        {
            Debug.Log($"--- Попытка генерации карты #{i} ---");
            CleanupMap();

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

        // --- ИСПРАВЛЕНИЕ: ОЧИЩАЕМ ВСЕ СЛОИ ПЕРЕД НОВОЙ ПОПЫТКОЙ ---
        if (landscapeMap != null) landscapeMap.ClearAllTiles();
        if (roadsMap != null) roadsMap.ClearAllTiles();
        if (foundationsMap != null) foundationsMap.ClearAllTiles();
        // ---------------------------------------------------------

        // Удаляем знаки и старт от неудачной генерации
        foreach (var obj in tempMapObjects)
        {
            if (obj != null) Destroy(obj);
        }
        tempMapObjects.Clear();
    }

    [Header("Текущий пул карт сессии")]
    public List<CardData> sessionCardPool = new List<CardData>();

    public void PrepareSessionCardPool()
    {
        sessionCardPool.Clear();

        if (activeLeader != null && activeLeader.heroMainCards != null)
        {
            sessionCardPool.AddRange(activeLeader.heroMainCards);

            if (activeLeader.heroSupportCards != null)
            {
                sessionCardPool.AddRange(activeLeader.heroSupportCards);
            }
        }

        if (activeSquad != null && activeSquad.Count > 0)
        {
            foreach (HeroData companion in activeSquad)
            {
                if (companion != null && companion.heroSupportCards != null)
                {
                    sessionCardPool.AddRange(companion.heroSupportCards);
                    Debug.Log($"Спутник {companion.unitName} добавил свои карты поддержки в пул.");
                }
            }
        }

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

        Vector3[] corners = new Vector3[4];
        float zOffset = -0.1f;

        corners[0] = new Vector3(0, 0, zOffset);
        corners[1] = new Vector3(0, mapHeight, zOffset);
        corners[2] = new Vector3(mapWidth, mapHeight, zOffset);
        corners[3] = new Vector3(mapWidth, 0, zOffset);

        borderLineRenderer.positionCount = 4;
        borderLineRenderer.SetPositions(corners);
        borderLineRenderer.loop = true;
    }
}