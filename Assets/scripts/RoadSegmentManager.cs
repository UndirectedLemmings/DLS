using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadSegmentManager : MonoBehaviour
{
    [Header("Road Data")]
    public List<Vector2Int> roadCells = new List<Vector2Int>(); // Сюда FILL_MAP_v4 передаст координаты дороги
    public FactionData ownerFaction;

    [Header("Spawn Settings")]
    public float spawnInterval = 10f; // Как часто дорога пытается заспавнить моба

    // Пул существ дороги
    [SerializeField] private List<EnemyData> spawnPool = new List<EnemyData>();

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    // Методы для модификации пула извне
    public void AddCreatureToPool(EnemyData creature)
    {
        if (creature != null)
        {
            spawnPool.Add(creature);
            Debug.Log($"В пул дороги добавлен: {creature.enemyName}");
        }
    }

    public void RemoveCreatureFromPool(EnemyData creature)
    {
        if (spawnPool.Contains(creature))
        {
            spawnPool.Remove(creature);
            Debug.Log($"Из пула дороги удален: {creature.enemyName}");
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            // Если пул пуст, спавнить некого
            if (spawnPool.Count == 0) continue;

            SpawnRandomCreature();
        }
    }

    private void SpawnRandomCreature()
    {
        // Ищем свободные клетки на этой дороге
        List<Vector2Int> emptyCells = new List<Vector2Int>();
        foreach (Vector2Int cell in roadCells)
        {
            // ПРЕДПОЛОЖЕНИЕ: У твоего LogicalGrid есть метод проверки
            if (GridGameController.Instance.logic.IsCellEmptyAndValid(cell))
            {
                emptyCells.Add(cell);
            }
        }

        if (emptyCells.Count == 0) return; // Нет места для спавна

        // Выбираем случайную пустую клетку
        Vector2Int spawnPos = emptyCells[Random.Range(0, emptyCells.Count)];

        // Берем случайного моба из пула
        EnemyData enemyToSpawn = spawnPool[Random.Range(0, spawnPool.Count)];

        // Инстанциируем префаб
        // ПРЕДПОЛОЖЕНИЕ: У тебя есть метод перевода Grid координат в World координаты
        Vector3 worldPos = GridGameController.Instance.GetWorldPosition(spawnPos);
        GameObject enemyObj = Instantiate(enemyToSpawn.enemyPrefab, worldPos, Quaternion.identity);

        // Регистрируем врага в сетке (помечаем hasEnemy = true)
        GridGameController.Instance.logic.SetEnemyAt(spawnPos, enemyObj);

        Debug.Log($"Заспавнен {enemyToSpawn.enemyName} на координатах {spawnPos}");
    }
}