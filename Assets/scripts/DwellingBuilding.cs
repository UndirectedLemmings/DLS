using UnityEngine;

public class DwellingBuilding : MonoBehaviour, IBuildingLogic, IMapInteractable
{
    [Header("Настройки Жилища")]
    public EnemyData spawnedCreature;

    private RoadSegmentManager attachedRoad;

    public string GetDescription()
    {
        if (spawnedCreature != null)
            return $"Жилище фракции\nПризывает: {spawnedCreature.enemyName}";
        return "Разрушенное жилище";
    }

    public void InitializeAt(Vector2Int cellPosition)
    {
        // 1. Ищем ближайшую к зданию дорогу
        attachedRoad = FindRoadSegment(cellPosition);

        // 2. Если дорога найдена и существо назначено в инспекторе - добавляем в пул
        if (attachedRoad != null && spawnedCreature != null)
        {
            attachedRoad.AddCreatureToPool(spawnedCreature);

            // Выводим красивый лог с именем фракции для проверки
            string factionName = attachedRoad.ownerFaction != null ? attachedRoad.ownerFaction.name : "Неизвестно";
            Debug.Log($"DLS: Жилище активно! {spawnedCreature.enemyName} добавлен в пул дороги фракции {factionName}.");
        }
        else
        {
            Debug.LogWarning($"DLS: {gameObject.name} - Не удалось найти дорогу или не назначено существо!");
        }
    }

    // --- НОВАЯ УМНАЯ ЛОГИКА ПОИСКА ДОРОГИ ---
    private RoadSegmentManager FindRoadSegment(Vector2Int pos)
    {
        RoadSegmentManager[] allManagers = FindObjectsByType<RoadSegmentManager>(FindObjectsSortMode.None);
        RoadSegmentManager closestManager = null;
        float minDistance = float.MaxValue;

        // Перебираем всех менеджеров на карте
        foreach (RoadSegmentManager manager in allManagers)
        {
            // У каждого менеджера проверяем все его клетки дороги
            foreach (Vector2Int roadCell in manager.roadCells)
            {
                // Считаем дистанцию от нашего фундамента до клетки дороги
                float dist = Vector2.Distance(pos, roadCell);

                // Если эта клетка ближе, чем всё, что мы находили раньше — запоминаем
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestManager = manager;
                }
            }
        }

        // Возвращаем менеджера, у которого оказалась самая близкая к нам клетка дороги
        return closestManager;
    }

    private void OnDestroy()
    {
        // Если здание сносят, честно забираем своего моба из пула
        if (attachedRoad != null && spawnedCreature != null)
        {
            attachedRoad.RemoveCreatureFromPool(spawnedCreature);
        }
    }
}