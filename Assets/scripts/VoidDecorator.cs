using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class VoidDecorator : MonoBehaviour
{
    [Header("Ссылки")]
    public Tilemap targetTilemap;

    [Header("Глубокий Космос (Края карты)")]
    public TileBase[] deepVoidTiles;

    [Header("Настройки Влияния")]
    public int territorySpreadRadius = 8;
    public Vector2 mapCenter = new Vector2(20f, 20f);

    [Header("Ограничение Героя")]
    public float heroOutwardBuffer = 1.5f;

    public void Decorate(int mapWidth, int mapHeight, HashSet<Vector3Int> occupiedCells, Dictionary<Vector3Int, TileBase[]> territoryMap, TileBase[] heroTerritoryTiles)
    {
        for (int x = -mapWidth; x < mapWidth * 2; x++)
        {
            for (int y = -mapHeight; y < mapHeight * 2; y++)
            {
                Vector3Int currentPos = new Vector3Int(x, y, 0);

                if (occupiedCells.Contains(currentPos)) continue;

                float minDistance = float.MaxValue;
                TileBase[] closestVoidTiles = null;

                // Вычисляем дистанцию от пустой клетки до центра один раз, для оптимизации
                float cellDistToCenter = Vector2.Distance(mapCenter, new Vector2(currentPos.x, currentPos.y));

                // Ищем ближайшую дорогу
                foreach (var roadCell in territoryMap)
                {
                    bool isHeroRoad = (roadCell.Value == heroTerritoryTiles);

                    // УМНАЯ ЛОГИКА: Если это дорога Героя, и клетка лежит "снаружи" от неё
                    if (isHeroRoad)
                    {
                        float roadDistToCenter = Vector2.Distance(mapCenter, new Vector2(roadCell.Key.x, roadCell.Key.y));

                        // Если клетка дальше от центра, чем дорога Героя (+ буфер) — Герой на неё не претендует!
                        if (cellDistToCenter > roadDistToCenter + heroOutwardBuffer)
                        {
                            continue; // Просто пропускаем эту дорогу, идём искать другие
                        }
                    }

                    // Стандартный замер дистанции (Вороной)
                    float dist = Vector3Int.Distance(currentPos, roadCell.Key);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestVoidTiles = roadCell.Value;
                    }
                }

                // Отрисовка
                if (closestVoidTiles != null && minDistance <= territorySpreadRadius)
                {
                    TileBase randomTerritory = closestVoidTiles[Random.Range(0, closestVoidTiles.Length)];
                    targetTilemap.SetTile(currentPos, randomTerritory);
                }
                else if (deepVoidTiles != null && deepVoidTiles.Length > 0)
                {
                    TileBase randomDeep = deepVoidTiles[Random.Range(0, deepVoidTiles.Length)];
                    targetTilemap.SetTile(currentPos, randomDeep);
                }
            }
        }
    }
}