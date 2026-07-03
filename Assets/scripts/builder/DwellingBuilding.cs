using UnityEngine;

public class DwellingBuilding : MonoBehaviour, IBuildingLogic, IMapInteractable
{
    [Header("Настройки Жилища")]
    public EnemyData spawnedCreature;

    private RoadSegmentManager attachedRoad;

    public string GetDescription()
    {
        if (spawnedCreature != null)
            return $"Жилище фракции\nПризывает: {spawnedCreature.unitName}";
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
            Debug.Log($"DLS: Жилище активно! {spawnedCreature.unitName} добавлен в пул дороги фракции {factionName}.");
        }
        else
        {
            Debug.LogWarning($"DLS: {gameObject.name} - Не удалось найти дорогу или не назначено существо!");
        }
    }

    // --- ДОБАВЛЕНО: ОБЯЗАТЕЛЬНАЯ ЗАГЛУШКА ДЛЯ ИНТЕРФЕЙСА IBuildingLogic ---
    // Метод оставляем абсолютно пустым, так как герои не взаимодействуют с жилищами напрямую.
    // Это нужно только для того, чтобы проект успешно компилировался!
    public void OnHeroVisit(Character_move hero)
    {
        // Пассивное здание. Ничего не происходит при шаге героя по координатам здания.
    }

    // --- НОВАЯ УМНАЯ ЛОГИКА ПОИСКА ДОРОГИ ---
    private RoadSegmentManager FindRoadSegment(Vector2Int pos)
    {
        RoadSegmentManager[] allManagers = FindObjectsByType<RoadSegmentManager>(FindObjectsSortMode.None);
        RoadSegmentManager closestManager = null;
        float minDistance = float.MaxValue;

        foreach (RoadSegmentManager manager in allManagers)
        {
            foreach (Vector2Int roadCell in manager.roadCells)
            {
                float dist = Vector2.Distance(pos, roadCell);

                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestManager = manager;
                }
            }
        }

        return closestManager;
    }

    private void OnDestroy()
    {
        if (attachedRoad != null && spawnedCreature != null)
        {
            attachedRoad.RemoveCreatureFromPool(spawnedCreature);
        }
    }
}