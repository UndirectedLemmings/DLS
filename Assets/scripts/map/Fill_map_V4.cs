using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FILL_MAP_v4 : MonoBehaviour
{
    [Header("Визуализация границ")]
    public LineRenderer borderLineRenderer;

    [Header("Настройки карты")]
    public int mapWidth = 40;
    public int mapHeight = 40;

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
    public GameObject start;
    public GameObject signpost;

    [Header("Настройки Героя (Внутренний круг)")]
    public List<HeroData> availableHeroes;
    public HeroData activeLeader;
    public List<HeroData> activeSquad;

    [Header("Настройки Фракций (Внешний круг)")]
    public List<FactionData> activeFactions;

    // --- ОБНОВЛЕННЫЕ РЕЕСТРЫ СЕТКИ (Все в Vector2Int) ---
    public Dictionary<Vector2Int, UnityEngine.Tilemaps.TileBase[]> territoryMap = new Dictionary<Vector2Int, UnityEngine.Tilemaps.TileBase[]>();
    public static Dictionary<Vector2Int, ScriptableObject> cellOwners = new Dictionary<Vector2Int, ScriptableObject>();
    public static HashSet<Vector2Int> FoundationCells = new HashSet<Vector2Int>();
    public static HashSet<Vector2Int> IntersectionCells = new HashSet<Vector2Int>();
    public static Dictionary<Vector2Int, CoordinateSwitcher> GlobalWaypoints = new Dictionary<Vector2Int, CoordinateSwitcher>();
    private HashSet<Vector2Int> globalOccupiedCells = new HashSet<Vector2Int>();
    private List<GameObject> tempMapObjects = new List<GameObject>();

    private Vector2Int Vector_Start;

    private bool generation_roadmap()
    {
        Debug.Log("Генерация: Активация Внешнего Кольца");
        GlobalWaypoints.Clear();
        globalOccupiedCells.Clear();
        territoryMap.Clear();
        cellOwners.Clear();

        int overscan = 20;

        for (int x = -overscan; x < mapWidth + overscan; x++)
        {
            for (int y = -overscan; y < mapHeight + overscan; y++)
            {
                // Для заливки используем 3D вектор локально
                landscapeMap.SetTile(new Vector3Int(x, y, 0), VoidTile);
            }
        }

        int margin = Mathf.Max(3, 14 - (difficultyLevel * 2));
        int randomOffset = 4;

        // 1. Старт
        Vector_Start = new Vector2Int(
            margin + Random.Range(0, randomOffset),
            margin + Random.Range(0, randomOffset));
        GameObject start_Object = Instantiate(start, roadsMap.GetCellCenterWorld(new Vector3Int(Vector_Start.x, Vector_Start.y, 0)), Quaternion.identity);
        tempMapObjects.Add(start_Object);

        // 2. Знак 1
        Vector2Int Vector_signpost1 = new Vector2Int(
            margin + Random.Range(0, randomOffset),
            mapHeight - margin - Random.Range(0, randomOffset));
        GameObject signpost1_Object = Instantiate(signpost, roadsMap.GetCellCenterWorld(new Vector3Int(Vector_signpost1.x, Vector_signpost1.y, 0)), Quaternion.identity);
        tempMapObjects.Add(signpost1_Object);

        // 3. Знак 2
        Vector2Int Vector_signpost2 = new Vector2Int(
            mapWidth - margin - Random.Range(0, randomOffset),
            mapHeight - margin - Random.Range(0, randomOffset));
        GameObject signpost2_Object = Instantiate(signpost, roadsMap.GetCellCenterWorld(new Vector3Int(Vector_signpost2.x, Vector_signpost2.y, 0)), Quaternion.identity);
        tempMapObjects.Add(signpost2_Object);

        // 4. Знак 3
        Vector2Int Vector_signpost3 = new Vector2Int(
            mapWidth - margin - Random.Range(0, randomOffset),
            margin + Random.Range(0, randomOffset));
        GameObject signpost3_Object = Instantiate(signpost, roadsMap.GetCellCenterWorld(new Vector3Int(Vector_signpost3.x, Vector_signpost3.y, 0)), Quaternion.identity);
        tempMapObjects.Add(signpost3_Object);

        Vector2 loopCenter = new Vector2(mapWidth / 2f, mapHeight / 2f);

        FactionData facS1 = null;
        FactionData facS2 = null;
        FactionData facS3 = null;
        FactionData facS4 = null;

        if (activeFactions != null && activeFactions.Count > 0)
        {
            if (activeFactions.Count == 1) { facS2 = activeFactions[0]; facS3 = activeFactions[0]; }
            else if (activeFactions.Count == 2) { facS2 = activeFactions[0]; facS3 = activeFactions[1]; }
            else if (activeFactions.Count >= 3) { facS2 = activeFactions[0]; facS3 = activeFactions[1]; facS4 = activeFactions[2]; }
        }

        if (!BuildSmartRoutes(start_Object.GetComponent<CoordinateSwitcher>(), Vector_Start, Vector_signpost1, loopCenter, facS1)) return false;
        GlobalWaypoints.Add(Vector_Start, start_Object.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost1_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost1, Vector_signpost2, loopCenter, facS2)) return false;
        GlobalWaypoints.Add(Vector_signpost1, signpost1_Object.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost2_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost2, Vector_signpost3, loopCenter, facS3)) return false;
        GlobalWaypoints.Add(Vector_signpost2, signpost2_Object.GetComponent<CoordinateSwitcher>());

        if (!BuildSmartRoutes(signpost3_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost3, Vector_Start, loopCenter, facS4)) return false;
        GlobalWaypoints.Add(Vector_signpost3, signpost3_Object.GetComponent<CoordinateSwitcher>());

        CameraMovement camScript = Camera.main.GetComponent<CameraMovement>();
        if (camScript != null) camScript.SetupCameraForMap(40, 40);

        // Конвертация для VoidDecorator (чтобы не переписывать его сейчас)
        if (voidDecorator != null)
        {
            HashSet<Vector3Int> occupied3D = new HashSet<Vector3Int>();
            foreach (var p in globalOccupiedCells) occupied3D.Add(new Vector3Int(p.x, p.y, 0));

            Dictionary<Vector3Int, TileBase[]> territory3D = new Dictionary<Vector3Int, TileBase[]>();
            foreach (var kvp in territoryMap) territory3D.Add(new Vector3Int(kvp.Key.x, kvp.Key.y, 0), kvp.Value);

            voidDecorator.Decorate(mapWidth, mapHeight, occupied3D, territory3D, activeLeader.territoryVoidTiles);
        }

        GridGameController.Instance.InitializeGrid(mapWidth, mapHeight);
        DrawMapBorder();

        return true;
    }

    private bool BuildSmartRoutes(CoordinateSwitcher switcher, Vector2Int startPoint, Vector2Int endPoint, Vector2 loopCenter, FactionData segmentFaction)
    {
        List<Vector2Int> pathA = FindPathAStar(startPoint, endPoint, new HashSet<Vector2Int>(), false, loopCenter);
        if (pathA == null || pathA.Count == 0) return false;

        DrawAndRegisterPath(pathA, activeLeader.heroRoadTile, activeLeader.territoryVoidTiles, activeLeader);

        int minA = Mathf.Max(0, minFoundationsPerRoad + activeLeader.bonusFoundations);
        int maxA = Mathf.Max(minA, maxFoundationsPerRoad + activeLeader.bonusFoundations);
        GenerateFoundations(pathA, minA, maxA);

        switcher.pathA = pathA;

        if (segmentFaction != null)
        {
            Vector2Int mergePoint = endPoint;
            if (pathA.Count > 3) mergePoint = pathA[pathA.Count - 3];

            IntersectionCells.Add(startPoint);
            IntersectionCells.Add(endPoint);
            IntersectionCells.Add(mergePoint);

            HashSet<Vector2Int> thickObstacles = GetThickObstacles(pathA, startPoint, endPoint, mergePoint);

            List<Vector2Int> pathB = FindPathAStar(startPoint, mergePoint, thickObstacles, true, loopCenter);
            if (pathB == null || pathB.Count == 0) return false;

            if (pathB.Count > 0)
            {
                int mergeIndex = pathA.IndexOf(mergePoint);
                if (mergeIndex != -1)
                {
                    for (int i = mergeIndex + 1; i < pathA.Count; i++) pathB.Add(pathA[i]);
                }
            }

            DrawAndRegisterPath(pathB, segmentFaction.factionRoadTile, segmentFaction.territoryVoidTiles, segmentFaction);

            int minB = Mathf.Max(0, minFoundationsPerRoad + segmentFaction.extraFoundations);
            int maxB = Mathf.Max(minB, maxFoundationsPerRoad + segmentFaction.extraFoundations);
            GenerateFoundations(pathB, minB, maxB);

            switcher.pathB = pathB;

            GameObject roadManagerObj = new GameObject($"RoadManager_{segmentFaction.name}");
            RoadSegmentManager roadManager = roadManagerObj.AddComponent<RoadSegmentManager>();
            roadManager.ownerFaction = segmentFaction;
            roadManager.roadCells = new List<Vector2Int>(pathB);
            roadManagerObj.transform.SetParent(this.transform);
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

    void DrawAndRegisterPath(List<Vector2Int> path, TileBase currentTile, TileBase[] voidTiles, ScriptableObject owner)
    {
        foreach (Vector2Int p in path)
        {
            if (globalOccupiedCells.Contains(p) && currentTile != activeLeader.heroRoadTile) continue;

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

    private class Node
    {
        public Vector2Int pos;
        public Node parent;
        public int gCost;
        public int hCost;
        public int fCost { get { return gCost + hCost; } }
    }

    private List<Vector2Int> FindPathAStar(Vector2Int startPos, Vector2Int targetPos, HashSet<Vector2Int> obstacles, bool isOuterRoute, Vector2 loopCenter)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

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

            Vector2Int[] neighbors = {
                new Vector2Int(currentNode.pos.x + 1, currentNode.pos.y),
                new Vector2Int(currentNode.pos.x - 1, currentNode.pos.y),
                new Vector2Int(currentNode.pos.x, currentNode.pos.y + 1),
                new Vector2Int(currentNode.pos.x, currentNode.pos.y - 1)
            };

            foreach (Vector2Int neighborPos in neighbors)
            {
                if (neighborPos.x < 0 || neighborPos.x > mapWidth || neighborPos.y < 0 || neighborPos.y > mapHeight) continue;
                if (closedSet.Contains(neighborPos)) continue;

                if (neighborPos != targetPos && (globalOccupiedCells.Contains(neighborPos) || obstacles.Contains(neighborPos)))
                    continue;

                float distToTargetEntry = Vector2Int.Distance(neighborPos, targetPos);

                if (distToTargetEntry <= 3)
                {
                    if (currentNode.pos.x == targetPos.x && neighborPos.x != targetPos.x) continue;
                    else if (currentNode.pos.y == targetPos.y && neighborPos.y != targetPos.y) continue;
                }

                int moveCost = 15;

                if (currentNode.parent != null)
                {
                    Vector2Int currentDirection = currentNode.pos - currentNode.parent.pos;
                    Vector2Int nextDirection = neighborPos - currentNode.pos;

                    if (currentDirection != nextDirection)
                    {
                        float distToTarget = Vector2Int.Distance(neighborPos, targetPos);
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

    public Vector2Int Get_Start_road()
    {
        return Vector_Start;
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
            }

            validCells.RemoveAt(randomIndex);
        }
    }

    public void StartGenerationWithRetries()
    {
        int maxAttempts = 10;
        for (int i = 1; i <= maxAttempts; i++)
        {
            CleanupMap();
            if (generation_roadmap()) return;
        }
        Debug.LogError("КРИТИЧЕСКАЯ ОШИБКА: Не удалось сгенерировать карту за 10 попыток!");
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

    [Header("Текущий пул карт сессии")]
    public List<CardData> sessionCardPool = new List<CardData>();

    public void PrepareSessionCardPool()
    {
        sessionCardPool.Clear();

        if (activeLeader != null && activeLeader.heroMainCards != null)
        {
            sessionCardPool.AddRange(activeLeader.heroMainCards);
            if (activeLeader.heroSupportCards != null) sessionCardPool.AddRange(activeLeader.heroSupportCards);
        }

        if (activeSquad != null && activeSquad.Count > 0)
        {
            foreach (HeroData companion in activeSquad)
            {
                if (companion != null && companion.heroSupportCards != null)
                    sessionCardPool.AddRange(companion.heroSupportCards);
            }
        }

        if (activeFactions != null && activeFactions.Count > 0)
        {
            foreach (FactionData faction in activeFactions)
            {
                if (faction != null && faction.factionCards != null)
                    sessionCardPool.AddRange(faction.factionCards);
            }
        }
    }

    void DrawMapBorder()
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