using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FILL_MAP_v4 : MonoBehaviour
{
    [Header("Глобальные настройки")]
    public GlobalSettingsData gameSettings;

    [Header("Визуализация границ")]
    public LineRenderer borderLineRenderer;

    [Header("Постобработка")]
    public VoidDecorator voidDecorator;

    [Header("Настройки сложности")]
    [Range(1, 10)] public int difficultyLevel = 1;

    [Header("Настройки фундаментов")]
    [Range(0, 5)] public int minFoundationsPerRoad = 1;
    [Range(1, 10)] public int maxFoundationsPerRoad = 3;

    [Header("Слои Карты (Tilemaps)")]
    public Tilemap landscapeMap;
    public Tilemap roadsMap;
    public Tilemap foundationsMap;

    [Header("Тайлы карты")]
    public TileBase VoidTile;
    public TileBase foundationTile;
    public GameObject foundationPrefab; // <-- ДОБАВЛЕНО
    public GameObject startPrefab;
    public GameObject signpostPrefab;

    // Настройки Героя (Внутренний круг)
    public List<HeroData> availableHeroes { get; private set; }
    public HeroData activeLeader { get; private set; }

    // --- ДОБАВЛЕНО: Для хранения данных класса (Бродяга/Воин) ---
    public TileBase[] activeHeroVoidTiles { get; private set; }
    public TileBase activeHeroRoadTile { get; private set; }

    // Настройки Фракций (Внешний круг)
    public List<FactionData> activeFactions { get; private set; }

    // --- РЕЕСТРЫ СЕТКИ ---
    public Dictionary<Vector2Int, TileBase[]> territoryMap = new Dictionary<Vector2Int, TileBase[]>();
    public static Dictionary<Vector2Int, ScriptableObject> cellOwners = new Dictionary<Vector2Int, ScriptableObject>();
    public static HashSet<Vector2Int> FoundationCells = new HashSet<Vector2Int>();
    public static HashSet<Vector2Int> IntersectionCells = new HashSet<Vector2Int>();
    public static Dictionary<Vector2Int, CoordinateSwitcher> GlobalWaypoints = new Dictionary<Vector2Int, CoordinateSwitcher>();

    private HashSet<Vector2Int> globalOccupiedCells = new HashSet<Vector2Int>();
    private List<GameObject> tempMapObjects = new List<GameObject>();

    private Vector2Int startCell;
    private int mapWidth;
    private int mapHeight;

    private void Awake()
    {
        // 1. Безопасное получение настроек карты
        if (gameSettings != null)
        {
            mapWidth = gameSettings.mapSize.x;
            mapHeight = gameSettings.mapSize.y;
        }
        else
        {
            Debug.LogError("ВНИМАНИЕ: Файл GlobalSettingsData не назначен в инспекторе FILL_MAP_v4!");
            mapWidth = 40;
            mapHeight = 40;
        }

        // 2. ИНТЕГРАЦИЯ С GAMEMANAGER
        if (GameManager.Instance != null)
        {
            // ИСПРАВЛЕНО: Берем живого лидера из формации, чтобы учесть его Класс (Бродяга и т.д.)
            if (GameManager.Instance.combatFormation != null && GameManager.Instance.combatFormation.Length > 0)
            {
                UnitProgress leaderProgress = GameManager.Instance.combatFormation[0];
                if (leaderProgress != null)
                {
                    // Сохраняем шаблон
                    activeLeader = leaderProgress.Template as HeroData;

                    // Читаем тайлы пустоты с учетом класса
                    activeHeroVoidTiles = leaderProgress.GetCurrentTerritoryTiles();

                    // --- ИСПРАВЛЕНО: Умное чтение дороги из класса лидера ---
                    activeHeroRoadTile = leaderProgress.GetCurrentRoadTile();

                    // 🚨 ТРЕВОГА ДЛЯ ТЕБЯ: Если тайл не нашелся, игра громко об этом скажет
                    if (activeHeroRoadTile == null)
                    {
                        Debug.LogError($"<color=red>[FILL_MAP]</color> КРИТИЧЕСКАЯ ОШИБКА: У лидера {leaderProgress.heroName} нет тайла дороги! Зайди в Инспектор его класса ({leaderProgress.classFeat?.name}) и добавь RuleTile в массив Class Territory Road Tiles.");
                    }
                }
            }
            // --- ВОТ ОНО: Забираем фракции напрямую из GameManager ---
            if (GameManager.Instance.currentFactions != null && GameManager.Instance.currentFactions.Count > 0)
            {
                activeFactions = new List<FactionData>(GameManager.Instance.currentFactions);
                Debug.Log($"[FILL_MAP] Загружено фракций из GameManager: {activeFactions.Count}");
            }

            // Просим GameManager собрать колоду на основе его собственных данных
            GameManager.Instance.PrepareSessionCardPool();

            Debug.Log($"[FILL_MAP] Данные экспедиции загружены из GameManager. Лидер: {activeLeader.name}");
        }
        else
        {
            Debug.LogWarning("[FILL_MAP] GameManager не найден. Используются тестовые герои и фракции из Инспектора.");
        }
    }

    public void StartGenerationWithRetries()
    {
        int maxAttempts = 10;
        for (int i = 1; i <= maxAttempts; i++)
        {
            CleanupMap();
            if (GenerateRoadmap()) return;
        }
        Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА: Не удалось сгенерировать карту за 10 попыток!");
    }

    private bool GenerateRoadmap()
    {
        Debug.Log("Генерация: Активация Внешнего Кольца");
        GlobalWaypoints.Clear();
        globalOccupiedCells.Clear();
        territoryMap.Clear();
        cellOwners.Clear();

        GridGameController.Instance.InitializeGrid(mapWidth, mapHeight);

        // Заливка фона
        int overscan = 20;
        for (int x = -overscan; x < mapWidth + overscan; x++)
        {
            for (int y = -overscan; y < mapHeight + overscan; y++)
            {
                landscapeMap.SetTile(new Vector3Int(x, y, 0), VoidTile);
            }
        }

        int margin = Mathf.Max(3, 14 - (difficultyLevel * 2));
        int randomOffset = 4;

        // Генерация ключевых точек (узлов)
        startCell = new Vector2Int(margin + Random.Range(0, randomOffset), margin + Random.Range(0, randomOffset));
        GameObject startObj = Instantiate(startPrefab, roadsMap.GetCellCenterWorld(new Vector3Int(startCell.x, startCell.y, 0)), Quaternion.identity);
        tempMapObjects.Add(startObj);

        IBuildingLogic startLogic = startObj.GetComponent<IBuildingLogic>();
        if (startLogic != null && GridGameController.Instance != null && GridGameController.Instance.logic != null)
        {
            startLogic.InitializeAt(startCell);
            GridGameController.Instance.logic.buildingInstances[startCell] = startLogic;
            Debug.Log($"[FILL_MAP] Стартовая точка успешно зарегистрирована в реестре на клетке {startCell}!");
        }

        Vector2Int signpost1Cell = new Vector2Int(margin + Random.Range(0, randomOffset), mapHeight - margin - Random.Range(0, randomOffset));
        GameObject signpost1Obj = Instantiate(signpostPrefab, roadsMap.GetCellCenterWorld(new Vector3Int(signpost1Cell.x, signpost1Cell.y, 0)), Quaternion.identity);
        tempMapObjects.Add(signpost1Obj);

        Vector2Int signpost2Cell = new Vector2Int(mapWidth - margin - Random.Range(0, randomOffset), mapHeight - margin - Random.Range(0, randomOffset));
        GameObject signpost2Obj = Instantiate(signpostPrefab, roadsMap.GetCellCenterWorld(new Vector3Int(signpost2Cell.x, signpost2Cell.y, 0)), Quaternion.identity);
        tempMapObjects.Add(signpost2Obj);

        Vector2Int signpost3Cell = new Vector2Int(mapWidth - margin - Random.Range(0, randomOffset), margin + Random.Range(0, randomOffset));
        GameObject signpost3Obj = Instantiate(signpostPrefab, roadsMap.GetCellCenterWorld(new Vector3Int(signpost3Cell.x, signpost3Cell.y, 0)), Quaternion.identity);
        tempMapObjects.Add(signpost3Obj);

        // Распределение фракций
        Vector2 loopCenter = new Vector2(mapWidth / 2f, mapHeight / 2f);
        FactionData facS1 = null, facS2 = null, facS3 = null, facS4 = null;

        if (activeFactions != null && activeFactions.Count > 0)
        {
            facS2 = activeFactions[0];
            facS3 = activeFactions.Count > 1 ? activeFactions[1] : activeFactions[0];
            facS4 = activeFactions.Count > 2 ? activeFactions[2] : null;
        }

        // Построение путей
        if (!BuildSmartRoutes(startObj.GetComponent<CoordinateSwitcher>(), startCell, signpost1Cell, loopCenter, facS1)) return false;
        GlobalWaypoints.Add(startCell, startObj.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost1Obj.GetComponent<CoordinateSwitcher>(), signpost1Cell, signpost2Cell, loopCenter, facS2)) return false;
        GlobalWaypoints.Add(signpost1Cell, signpost1Obj.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost2Obj.GetComponent<CoordinateSwitcher>(), signpost2Cell, signpost3Cell, loopCenter, facS3)) return false;
        GlobalWaypoints.Add(signpost2Cell, signpost2Obj.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost3Obj.GetComponent<CoordinateSwitcher>(), signpost3Cell, startCell, loopCenter, facS4)) return false;
        GlobalWaypoints.Add(signpost3Cell, signpost3Obj.GetComponent<CoordinateSwitcher>());

        // Настройка камеры
        if (Camera.main.TryGetComponent(out CameraMovement camScript))
        {
            camScript.SetupCameraForMap(mapWidth, mapHeight);
        }

        // Декорирование и финальная инициализация
        if (voidDecorator != null)
        {
            HashSet<Vector3Int> occupied3D = new HashSet<Vector3Int>();
            foreach (var p in globalOccupiedCells) occupied3D.Add(new Vector3Int(p.x, p.y, 0));

            Dictionary<Vector3Int, TileBase[]> territory3D = new Dictionary<Vector3Int, TileBase[]>();
            foreach (var kvp in territoryMap) territory3D.Add(new Vector3Int(kvp.Key.x, kvp.Key.y, 0), kvp.Value);

            // ИСПРАВЛЕНО: Передаем activeHeroVoidTiles вместо activeLeader.territoryVoidTiles
            voidDecorator.Decorate(mapWidth, mapHeight, occupied3D, territory3D, activeHeroVoidTiles);
        }

        DrawMapBorder();

        return true;
    }

    private bool BuildSmartRoutes(CoordinateSwitcher switcher, Vector2Int startPoint, Vector2Int endPoint, Vector2 loopCenter, FactionData segmentFaction)
    {
        List<Vector2Int> pathA = FindPathAStar(startPoint, endPoint, new HashSet<Vector2Int>(), false, loopCenter);
        if (pathA == null || pathA.Count == 0) return false;
        // ИСПРАВЛЕНО: Передаем activeHeroVoidTiles вместо activeLeader.territoryVoidTiles
        DrawAndRegisterPath(pathA, activeHeroRoadTile, activeHeroVoidTiles, activeLeader);

        int minA = Mathf.Max(0, minFoundationsPerRoad + activeLeader.bonusFoundations);
        int maxA = Mathf.Max(minA, maxFoundationsPerRoad + activeLeader.bonusFoundations);
        GenerateFoundations(pathA, minA, maxA);

        switcher.pathA = pathA;

        if (segmentFaction != null)
        {
            Vector2Int mergePoint = pathA.Count > 3 ? pathA[pathA.Count - 3] : endPoint;

            IntersectionCells.Add(startPoint);
            IntersectionCells.Add(endPoint);
            IntersectionCells.Add(mergePoint);

            HashSet<Vector2Int> thickObstacles = GetThickObstacles(pathA, startPoint, endPoint, mergePoint);
            List<Vector2Int> pathB = FindPathAStar(startPoint, mergePoint, thickObstacles, true, loopCenter);

            if (pathB == null || pathB.Count == 0) return false;

            int mergeIndex = pathA.IndexOf(mergePoint);
            if (mergeIndex != -1)
            {
                for (int i = mergeIndex + 1; i < pathA.Count; i++) pathB.Add(pathA[i]);
            }

            DrawAndRegisterPath(pathB, segmentFaction.factionRoadTile, segmentFaction.territoryVoidTiles, segmentFaction);

            int minB = Mathf.Max(0, minFoundationsPerRoad + segmentFaction.extraFoundations);
            int maxB = Mathf.Max(minB, maxFoundationsPerRoad + segmentFaction.extraFoundations);
            GenerateFoundations(pathB, minB, maxB);

            switcher.pathB = pathB;

            GameObject roadManagerObj = new GameObject($"RoadManager_{segmentFaction.name}");
            roadManagerObj.transform.SetParent(this.transform);

            RoadSegmentManager roadManager = roadManagerObj.AddComponent<RoadSegmentManager>();
            roadManager.ownerFaction = segmentFaction;
            roadManager.roadCells = new List<Vector2Int>(pathB);
        }
        else
        {
            switcher.pathB = pathA;
        }

        return true;
    }

    private HashSet<Vector2Int> GetThickObstacles(List<Vector2Int> path, Vector2Int startPoint, Vector2Int endPoint, Vector2Int mergePoint)
    {
        HashSet<Vector2Int> thick = new HashSet<Vector2Int>();
        if (path == null) return thick;

        foreach (Vector2Int p in path)
        {
            if (Vector2Int.Distance(p, startPoint) <= 2 ||
                Vector2Int.Distance(p, endPoint) <= 2 ||
                Vector2Int.Distance(p, mergePoint) <= 2) continue;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    thick.Add(new Vector2Int(p.x + x, p.y + y));
                }
            }
        }
        return thick;
    }

    private void DrawAndRegisterPath(List<Vector2Int> path, TileBase currentTile, TileBase[] voidTiles, ScriptableObject owner)
    {
        foreach (Vector2Int p in path)
        {
            // ИСПРАВЛЕНО: Теперь сравниваем с новой переменной activeHeroRoadTile!
            // Это позволит центральной дороге героя перекрывать чужие пути
            if (globalOccupiedCells.Contains(p) && currentTile != activeHeroRoadTile) continue;

            roadsMap.SetTile(new Vector3Int(p.x, p.y, 0), currentTile);
            globalOccupiedCells.Add(p);

            if (owner != null)
            {
                if (!cellOwners.ContainsKey(p)) cellOwners.Add(p, owner);
                else if (owner is HeroData) cellOwners[p] = owner;
            }

            if (voidTiles != null && voidTiles.Length > 0)
            {
                territoryMap[p] = voidTiles;
                TileBase randomBiomeTile = voidTiles[Random.Range(0, voidTiles.Length)];
                landscapeMap.SetTile(new Vector3Int(p.x, p.y, 0), randomBiomeTile);
            }
        }
    }

    // --- A* PATHFINDING ---

    private class Node
    {
        public Vector2Int pos;
        public Node parent;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;
    }

    private List<Vector2Int> FindPathAStar(Vector2Int startPos, Vector2Int targetPos, HashSet<Vector2Int> obstacles, bool isOuterRoute, Vector2 loopCenter)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node { pos = startPos, gCost = 0, hCost = GetDistance(startPos, targetPos) };
        openSet.Add(startNode);

        Vector2Int[] directions = {
            Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down
        };

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
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

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = currentNode.pos + dir;

                if (neighborPos.x < 0 || neighborPos.x > mapWidth || neighborPos.y < 0 || neighborPos.y > mapHeight) continue;
                if (closedSet.Contains(neighborPos)) continue;
                if (neighborPos != targetPos && (globalOccupiedCells.Contains(neighborPos) || obstacles.Contains(neighborPos))) continue;

                float distToTargetEntry = Vector2Int.Distance(neighborPos, targetPos);
                if (distToTargetEntry <= 3)
                {
                    if (currentNode.pos.x == targetPos.x && neighborPos.x != targetPos.x) continue;
                    if (currentNode.pos.y == targetPos.y && neighborPos.y != targetPos.y) continue;
                }

                int moveCost = 15;

                if (currentNode.parent != null)
                {
                    Vector2Int currentDirection = currentNode.pos - currentNode.parent.pos;
                    Vector2Int nextDirection = neighborPos - currentNode.pos;

                    if (currentDirection != nextDirection)
                    {
                        float distToTarget = Vector2Int.Distance(neighborPos, targetPos);
                        moveCost += distToTarget < 6 ? 0 : 30; // Turn penalty
                    }
                }

                moveCost += (int)(Mathf.PerlinNoise(neighborPos.x * 0.2f, neighborPos.y * 0.2f) * 25);

                if (isOuterRoute)
                {
                    float distToCenter = Vector2.Distance(neighborPos, loopCenter);
                    moveCost += (int)(Mathf.Max(0, 15f - distToCenter) * 5);
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
        return new List<Vector2Int>();
    }

    private List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
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

    private int GetDistance(Vector2Int nodeA, Vector2Int nodeB)
    {
        int dstX = Mathf.Abs(nodeA.x - nodeB.x);
        int dstY = Mathf.Abs(nodeA.y - nodeB.y);
        return (dstX + dstY) * 10;
    }

    // --- UTILS ---

    public Vector2Int Get_Start_road()
    {
        return startCell;
    }

    private void GenerateFoundations(List<Vector2Int> path, int minCount, int maxCount)
    {
        if (path == null || path.Count < 5) return;

        int targetCount = Random.Range(minCount, maxCount + 1);
        int placedCount = 0;

        List<Vector2Int> validCells = new List<Vector2Int>();
        for (int i = 2; i < path.Count - 2; i++) validCells.Add(path[i]);

        int attempts = 0;
        while (placedCount < targetCount && validCells.Count > 0 && attempts < 50)
        {
            attempts++;
            int randomIndex = Random.Range(0, validCells.Count);
            Vector2Int cell = validCells[randomIndex];

            bool isTooClose = false;
            foreach (Vector2Int existingFoundation in FoundationCells)
            {
                if (Vector2Int.Distance(cell, existingFoundation) < 1.5f)
                {
                    isTooClose = true;
                    break;
                }
            }

            if (!isTooClose)
            {
                foundationsMap.SetTile(new Vector3Int(cell.x, cell.y, 0), foundationTile);
                FoundationCells.Add(cell);
                placedCount++;

                // --- НОВОЕ: Спавним Здание 0-го уровня и заносим в реестр ---
                if (foundationPrefab != null)
                {
                    Vector3 spawnPos = foundationsMap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
                    spawnPos.z = -0.1f; // Чуть выше тайлов

                    GameObject foundObj = Instantiate(foundationPrefab, spawnPos, Quaternion.identity);
                    tempMapObjects.Add(foundObj); // Добавляем сюда, чтобы генератор сам удалял их при рестарте карты

                    IBuildingLogic logic = foundObj.GetComponent<IBuildingLogic>();
                    if (logic != null && GridGameController.Instance != null && GridGameController.Instance.logic != null)
                    {
                        logic.InitializeAt(cell);
                        GridGameController.Instance.logic.buildingInstances[cell] = logic;
                    }
                }
            }

            validCells.RemoveAt(randomIndex);
        }
    }

    private void CleanupMap()
    {
        GlobalWaypoints.Clear();
        globalOccupiedCells.Clear();
        FoundationCells.Clear();

        if (landscapeMap != null) landscapeMap.ClearAllTiles();
        if (roadsMap != null) roadsMap.ClearAllTiles();
        if (foundationsMap != null) foundationsMap.ClearAllTiles();

        foreach (var obj in tempMapObjects)
        {
            if (obj != null) Destroy(obj);
        }
        tempMapObjects.Clear();
    }

    private void DrawMapBorder()
    {
        if (borderLineRenderer == null) return;

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