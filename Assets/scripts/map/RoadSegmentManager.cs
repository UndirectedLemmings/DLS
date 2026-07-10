using System.Collections.Generic;
using UnityEngine;

public class RoadSegmentManager : MonoBehaviour
{
    [Header("Данные дороги")]
    public List<Vector2Int> roadCells = new List<Vector2Int>(); // Клетки этого участка дороги
    public FactionData ownerFaction; // Владелец дороги (фракция)

    [Header("Настройки карточного стакинга флагов")]
    [Range(0f, 1f)]
    [Tooltip("Шанс состыковаться в существующий флаг на этой дороге, если он есть (0.25 = 25%)")]
    public float stackChance = 0.25f;

    // --- МЕТОД СПАВНА И КАРТОЧНОГО СТАКИНГА ---
    // Вызывается напрямую из DwellingBuilding, когда постройка решает выпустить моба на дорогу
    public void SpawnCreatureOnRoad(EnemyData enemyToSpawn)
    {
        if (enemyToSpawn == null) return;

        List<Vector2Int> emptyCells = new List<Vector2Int>();
        List<Vector2Int> occupiedByEnemies = new List<Vector2Int>();

        // 1. Сортируем клетки нашей дороги через логическую сетку контроллера карты
        foreach (Vector2Int cell in roadCells)
        {
            GameObject existingEnemyObj = GridGameController.Instance.logic.GetEnemyAt(cell);
            if (existingEnemyObj != null)
            {
                // Если на клетке уже стоит объект с компонентом отряда (наш флаг встреч)
                if (existingEnemyObj.GetComponent<EnemySquad>() != null)
                {
                    occupiedByEnemies.Add(cell);
                }
            }
            else
            {
                emptyCells.Add(cell);
            }
        }

        // 2. СТАКИНГ: Проверяем шанс слияния с уже существующим флагом встречи
        if (occupiedByEnemies.Count > 0 && (emptyCells.Count == 0 || Random.value <= stackChance))
        {
            Vector2Int stackPos = occupiedByEnemies[Random.Range(0, occupiedByEnemies.Count)];
            GameObject existingEnemyObj = GridGameController.Instance.logic.GetEnemyAt(stackPos);

            if (existingEnemyObj != null)
            {
                EnemySquad squad = existingEnemyObj.GetComponent<EnemySquad>();
                if (squad != null)
                {
                    squad.AddEnemy(enemyToSpawn);
                    Debug.Log($"DLS: Стакинг сработал! Моб {enemyToSpawn.unitName} упал в отряд флага на клетке {stackPos}.");
                    return; // Успешно упали в стак, прерываем метод
                }
            }
        }

        // 3. ОБЫЧНЫЙ СПАВН: Создаем новый независимый флаг на пустой клетке дороги
        if (emptyCells.Count > 0)
        {
            Vector2Int spawnPos = emptyCells[Random.Range(0, emptyCells.Count)];
            Vector3 worldPos = GridGameController.Instance.GetWorldPosition(spawnPos);

            // Инстанцируем префаб моба (который и отображается как флаг/фишка встречи на глобальной карте)
            GameObject enemyObj = Instantiate(enemyToSpawn.enemyPrefab, worldPos, Quaternion.identity);

            // Инициализируем компонент EnemySquad на созданном флаге
            EnemySquad newSquad = enemyObj.GetComponent<EnemySquad>();
            if (newSquad != null)
            {
                newSquad.Initialize(enemyToSpawn);
            }

            // Регистрируем флаг в глобальной логической сетке
            GridGameController.Instance.logic.SetEnemyAt(spawnPos, enemyObj);

            Debug.Log($"DLS: Спавн нового флага на клетке {spawnPos}. Первый юнит отряда: {enemyToSpawn.unitName}");
        }
        else
        {
            Debug.LogWarning($"DLS: На дороге нет свободных мест и не сработал стакинг для {enemyToSpawn.unitName}!");
        }
    }
}