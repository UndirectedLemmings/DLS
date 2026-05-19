using UnityEngine;

public class DwellingBuilding : MonoBehaviour, IBuildingLogic
{
    [Header("Dwelling Logic")]
    public EnemyData spawnedCreature;

    private RoadSegmentManager attachedRoad;

    // Реализуем метод интерфейса
    public void InitializeAt(Vector2Int cellPosition)
    {
        // 1. Здание само ищет сегмент дороги, к которому относится эта клетка
        // (Предполагается, что у тебя будет метод для поиска сегмента по координате)
        attachedRoad = FindRoadSegment(cellPosition);

        // 2. Если дорога найдена и существо задано - пополняем пул
        if (attachedRoad != null && spawnedCreature != null)
        {
            attachedRoad.AddCreatureToPool(spawnedCreature);
            Debug.Log($"Жилище активно! {spawnedCreature.enemyName} добавлен в пул дороги.");
        }
    }

    private RoadSegmentManager FindRoadSegment(Vector2Int pos)
    {
        // Здесь будет твоя логика поиска нужного RoadSegmentManager
        // Например, запрос к глобальному контроллеру территорий
        return null; // Заглушка
    }

    private void OnDestroy()
    {
        if (attachedRoad != null && spawnedCreature != null)
        {
            attachedRoad.RemoveCreatureFromPool(spawnedCreature);
        }
    }
}