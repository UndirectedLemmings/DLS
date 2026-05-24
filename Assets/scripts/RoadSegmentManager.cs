using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadSegmentManager : MonoBehaviour
{
    [Header("Road Data")]
    public List<Vector2Int> roadCells = new List<Vector2Int>(); // Сюда FILL_MAP_v4 передаст координаты дороги
    public FactionData ownerFaction;

    [Header("Spawn Settings")]
    public float spawnInterval = 10f;

    // НОВОЕ: Шанс стакинга (например, 25%)
    [Range(0f, 1f)]
    public float stackChance = 0.25f;

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
        List<Vector2Int> emptyCells = new List<Vector2Int>();
        List<Vector2Int> occupiedByEnemies = new List<Vector2Int>();

        foreach (Vector2Int cell in roadCells)
        {
            Vector3Int v3Cell = new Vector3Int(cell.x, cell.y, 0);

            // --- ОБНОВЛЕННАЯ ПРОВЕРКА ---
            // Если тут фундамент, ИЛИ здание, ИЛИ перекресток — пропускаем!
            if (FILL_MAP_v4.FoundationCells.Contains(v3Cell) ||
                GridGameController.Instance.logic.buildingsOnMap.Contains(cell) ||
                FILL_MAP_v4.IntersectionCells.Contains(v3Cell)) // <--- Добавили проверку перекрестка
            {
                continue;
            }

            // 2. Разделяем клетки
            if (GridGameController.Instance.logic.GetEnemyAt(cell) != null)
            {
                occupiedByEnemies.Add(cell);
            }
            else
            {
                emptyCells.Add(cell);
            }
        }

        if (emptyCells.Count == 0 && occupiedByEnemies.Count == 0) return;

        EnemyData enemyToSpawn = spawnPool[Random.Range(0, spawnPool.Count)];

        // --- ЛОГИКА УСИЛЕНИЯ (СТАКИНГА) ---
        if (Random.value <= stackChance && occupiedByEnemies.Count > 0)
        {
            Vector2Int stackPos = occupiedByEnemies[Random.Range(0, occupiedByEnemies.Count)];
            GameObject existingEnemyObj = GridGameController.Instance.logic.GetEnemyAt(stackPos);

            if (existingEnemyObj != null)
            {
                // Ищем наш новый компонент и добавляем в него моба
                EnemySquad squad = existingEnemyObj.GetComponent<EnemySquad>();
                if (squad != null)
                {
                    squad.AddEnemy(enemyToSpawn);
                }
            }
            return; // Завершаем метод
        }

        // --- ЛОГИКА ОБЫЧНОГО СПАВНА ---
        if (emptyCells.Count > 0)
        {
            Vector2Int spawnPos = emptyCells[Random.Range(0, emptyCells.Count)];
            Vector3 worldPos = GridGameController.Instance.GetWorldPosition(spawnPos);

            // Инстанциируем префаб
            GameObject enemyObj = Instantiate(enemyToSpawn.enemyPrefab, worldPos, Quaternion.identity);

            // НОВОЕ: Инициализируем отряд первым бойцом
            EnemySquad newSquad = enemyObj.GetComponent<EnemySquad>();
            if (newSquad != null)
            {
                newSquad.Initialize(enemyToSpawn);
            }

            // Регистрируем в сетке
            GridGameController.Instance.logic.SetEnemyAt(spawnPos, enemyObj);

            Debug.Log($"DLS: Заспавнен новый отряд {enemyToSpawn.enemyName} на координатах {spawnPos}");
        }
    }
}