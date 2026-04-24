using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FILL_MAP_v4 : MonoBehaviour
{
    public Tilemap Map;
    public Tile Void;
    public Tile road;
    public GameObject start;
    public GameObject signpost;

    public static Dictionary<Vector3Int, CoordinateSwitcher> GlobalWaypoints = new Dictionary<Vector3Int, CoordinateSwitcher>();
    private HashSet<Vector3Int> globalOccupiedCells = new HashSet<Vector3Int>();

    // Расширим карту, чтобы внешним маршрутам было где развернуться
    private int mapWidth = 64;
    private int mapHeight = 64;

     public void Start()
     {
         Vector3Int map_vector = new Vector3Int();
         for (map_vector.x = 0; map_vector.x <= mapWidth; map_vector.x++)
         {
             for (map_vector.y = 0; map_vector.y <= mapHeight; map_vector.y++)
             {
                 Map.SetTile(map_vector, Void);
             }
         }

     }
    Vector3Int Vector_Start;
    public void generation_roadmap()
    {
        Debug.Log("Генерация: Активация Внешнего Кольца");
        GlobalWaypoints.Clear();
        globalOccupiedCells.Clear();

        int road_leath = 10;
        int road_min = 10; // Немного отодвинул от краев (0,0), чтобы было место для внешнего круга

        Vector_Start = new Vector3Int(Random.Range(road_min, road_leath), Random.Range(road_min, road_leath));
        GameObject start_Object = Instantiate(start, (Vector3)Vector_Start, Quaternion.identity);

        Vector3Int Vector_signpost1 = new Vector3Int(Random.Range((road_min), (road_min + road_leath)), Random.Range((int)(Vector_Start.y + road_min), (int)(Vector_Start.y + road_leath)));
        GameObject signpost1_Object = Instantiate(signpost, Vector_signpost1, Quaternion.identity);

        Vector3Int Vector_signpost2 = new Vector3Int(Random.Range((int)(Vector_signpost1.x + road_min), (int)(Vector_signpost1.x + road_leath)), Random.Range((int)(Vector_signpost1.y), (int)(Vector_signpost1.y + road_leath)));
        GameObject signpost2_Object = Instantiate(signpost, Vector_signpost2, Quaternion.identity);

        Vector3Int Vector_signpost3 = new Vector3Int(Random.Range((int)(Vector_signpost2.x + road_min), (int)(Vector_signpost2.x + road_leath)), Random.Range((int)(Vector_signpost2.y - road_leath), (int)(Vector_signpost2.y - road_min)));
        GameObject signpost3_Object = Instantiate(signpost, Vector_signpost3, Quaternion.identity);

        // --- ВЫЧИСЛЯЕМ ЦЕНТР НАШЕГО ГОРОДА ---
        Vector2 loopCenter = new Vector2(
            (Vector_Start.x + Vector_signpost1.x + Vector_signpost2.x + Vector_signpost3.x) / 4f,
            (Vector_Start.y + Vector_signpost1.y + Vector_signpost2.y + Vector_signpost3.y) / 4f
        );

        BuildSmartRoutes(start_Object.GetComponent<CoordinateSwitcher>(), Vector_Start, Vector_signpost1, loopCenter);
        GlobalWaypoints.Add(Vector_Start, start_Object.GetComponent<CoordinateSwitcher>());

        BuildSmartRoutes(signpost1_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost1, Vector_signpost2, loopCenter);
        GlobalWaypoints.Add(Vector_signpost1, signpost1_Object.GetComponent<CoordinateSwitcher>());

        BuildSmartRoutes(signpost2_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost2, Vector_signpost3, loopCenter);
        GlobalWaypoints.Add(Vector_signpost2, signpost2_Object.GetComponent<CoordinateSwitcher>());

        BuildSmartRoutes(signpost3_Object.GetComponent<CoordinateSwitcher>(), Vector_signpost3, Vector_Start, loopCenter);
        GlobalWaypoints.Add(Vector_signpost3, signpost3_Object.GetComponent<CoordinateSwitcher>());
    }


    private void BuildSmartRoutes(CoordinateSwitcher switcher, Vector3Int startPoint, Vector3Int endPoint, Vector2 loopCenter)
    {
        // 1. Внутренний (основной) круг А
        List<Vector3Int> pathA = FindPathAStar(startPoint, endPoint, new HashSet<Vector3Int>(), false, loopCenter);
        DrawAndRegisterPath(pathA); // Сразу рисуем, чтобы он стал препятствием для Б

        // 2. Генерируем "толстые" стены из пути А
        HashSet<Vector3Int> thickObstacles = GetThickObstacles(pathA, startPoint, endPoint);

        // 3. Внешний круг Б (передаем true, чтобы включить страх центра)
        List<Vector3Int> pathB = FindPathAStar(startPoint, endPoint, thickObstacles, true, loopCenter);
        DrawAndRegisterPath(pathB);

        switcher.pathA = pathA;
        switcher.pathB = pathB;
    }

    // Создает буферную зону вокруг маршрута А
    private HashSet<Vector3Int> GetThickObstacles(List<Vector3Int> path, Vector3Int startPoint, Vector3Int endPoint)
    {
        HashSet<Vector3Int> thick = new HashSet<Vector3Int>();
        if (path == null) return thick;

        foreach (Vector3Int p in path)
        {
            // Не блокируем клетки в радиусе 2х шагов от перекрестков, иначе выход будет замурован
            if (Vector3Int.Distance(p, startPoint) <= 2 || Vector3Int.Distance(p, endPoint) <= 2)
            {
                thick.Add(p);
                continue;
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
        thick.Remove(startPoint);
        thick.Remove(endPoint);
        return thick;
    }

    private void DrawAndRegisterPath(List<Vector3Int> path)
    {
        if (path == null) return;
        foreach (Vector3Int p in path)
        {
            Map.SetTile(p, road);
            globalOccupiedCells.Add(p);
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

                // Базовая цена шага
                int moveCost = 15;

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
}