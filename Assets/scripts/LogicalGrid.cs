using System.Collections.Generic;
using UnityEngine;

public class LogicalGrid
{
    // Переименовали переменные
    private int gridWidth;
    private int gridHeight;

    public Dictionary<Vector2Int, GameObject> enemiesOnMap = new Dictionary<Vector2Int, GameObject>();

    // Конструктор принимает значения и записывает их в новые переменные
    public LogicalGrid(int w, int h)
    {
        gridWidth = w;
        gridHeight = h;
    }

    // Обновляем проверку на новые переменные
    public bool InBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    public bool IsCellEmptyAndValid(Vector2Int pos)
    {
        if (!InBounds(pos)) return false;

        if (enemiesOnMap.ContainsKey(pos) && enemiesOnMap[pos] != null)
        {
            return false;
        }

        return true;
    }

    public void SetEnemyAt(Vector2Int pos, GameObject enemyObj)
    {
        if (InBounds(pos))
        {
            enemiesOnMap[pos] = enemyObj;
        }
    }

    public GameObject GetEnemyAt(Vector2Int pos)
    {
        if (enemiesOnMap.ContainsKey(pos) && enemiesOnMap[pos] != null)
        {
            return enemiesOnMap[pos];
        }

        return null;
    }
}