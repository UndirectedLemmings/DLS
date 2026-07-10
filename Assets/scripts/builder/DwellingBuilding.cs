using System.Collections.Generic;
using System.Text;
using UnityEngine;

// --- ВОТ ЭТОТ БЛОК БЫЛ ПОТЕРЯН ---
[System.Serializable]
public struct DwellingSpawnEntry
{
    [Tooltip("Ссылка на шаблон моба (EnemyData)")]
    public EnemyData creature;

    [Tooltip("Максимальное количество мобов этого типа, пытающихся добавиться в пул")]
    public int count;

    [Range(0f, 100f)]
    [Tooltip("Шанс спавна для КАЖДОГО отдельного моба из count (в процентах)")]
    public float spawnChance;
}
// ---------------------------------

public class DwellingBuilding : MonoBehaviour, IBuildingLogic, IMapInteractable
{
    [Header("Настройки Жилища")]
    [Tooltip("Список существ и их количества для заселения прилегающей дороги")]
    public List<DwellingSpawnEntry> creaturesToSpawn;

    private RoadSegmentManager attachedRoad;
    private bool isInitialized = false;

    // --- ПОДПИСКА НА СОБЫТИЯ ---
    private void OnEnable()
    {
        // Когда объект включается, подписываемся на событие нового круга
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewLapStarted += SpawnWave;
        }
        StartCoroutine(SubscribeToGameManager());
    }

    private System.Collections.IEnumerator SubscribeToGameManager()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        GameManager.Instance.OnNewLapStarted += SpawnWave;
    }

    private void OnDisable()
    {
        // Обязательно отписываемся при удалении/выключении здания, чтобы не было утечек памяти!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewLapStarted -= SpawnWave;
        }
    }

    // --- ИНИЦИАЛИЗАЦИЯ (Один раз при постройке) ---
    public void InitializeAt(Vector2Int cellPosition)
    {
        attachedRoad = FindRoadSegment(cellPosition);

        if (attachedRoad == null)
        {
            Debug.LogWarning($"DLS: {gameObject.name} - Не удалось найти дорогу для привязки жилища!");
            return;
        }

        isInitialized = true;

        // Делаем первый бесплатный спавн мобов сразу при постройке/генерации
        SpawnWave();
    }

    // --- ГЕНЕРАЦИЯ МОБОВ (Вызывается каждый круг) ---
    private void SpawnWave()
    {
        if (!isInitialized || attachedRoad == null || creaturesToSpawn == null || creaturesToSpawn.Count == 0) return;

        Debug.Log($"[Dwelling] {gameObject.name} генерирует отряд...");

        string factionName = attachedRoad.ownerFaction != null ? attachedRoad.ownerFaction.name : "Неизвестно";

        foreach (var entry in creaturesToSpawn)
        {
            if (entry.creature != null && entry.count > 0)
            {
                int attemptsSucceeded = 0;

                for (int i = 0; i < entry.count; i++)
                {
                    if (Random.Range(0f, 100f) <= entry.spawnChance)
                    {
                        attachedRoad.SpawnCreatureOnRoad(entry.creature);
                        attemptsSucceeded++;
                    }
                }

                if (attemptsSucceeded > 0)
                {
                    Debug.Log($"DLS: [Новый круг] Жилище выпустило {entry.creature.unitName} (x{attemptsSucceeded}) на дорогу фракции {factionName}.");
                }
            }
        }
    }
    public string GetDescription()
    {
        if (creaturesToSpawn == null || creaturesToSpawn.Count == 0)
            return "Разрушенное жилище";

        StringBuilder descriptionBuilder = new StringBuilder("Жилище фракции\nПризывает:");
        bool hasCreatures = false;

        foreach (var entry in creaturesToSpawn)
        {
            if (entry.creature != null && entry.count > 0)
            {
                if (entry.spawnChance >= 100f)
                    descriptionBuilder.Append($"\n• {entry.creature.unitName} x{entry.count}");
                else
                    descriptionBuilder.Append($"\n• {entry.creature.unitName} x{entry.count} (Шанс: {entry.spawnChance}%)");

                hasCreatures = true;
            }
        }

        return hasCreatures ? descriptionBuilder.ToString() : "Разрушенное жилище (пусто)";
    }

    public void OnHeroVisit(Character_move hero)
    {
        // Пассивное здание
    }

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
}