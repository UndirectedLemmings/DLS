using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Character_move : MonoBehaviour
{
    [Header("Глобальные настройки")]
    public GlobalSettingsData gameSettings;

    public Tilemap Tilemap;
    public float crossroadWaitTime = 0.5f;

    private float currentSpeed = 5f;

    private bool isMoving = false;
    private bool isWaiting = false;
    private int lapsCount = 0;

    private List<Vector2Int> currentPath;
    private int waypointIndex = 0;
    private Vector2Int startNode;
    private HandManager HandManager;

    // --- ДОБАВЛЕНО: Надежный счетчик шагов вместо флага ---
    private int stepsSinceLastLap = 0;

    // Память последней посещенной клетки
    private Vector2Int lastVisitedCell = new Vector2Int(-9999, -9999);

    public Vector2Int currentPosition { get; private set; }
    private ExpeditionExitController exitUI;

    private void Awake()
    {
        if (gameSettings != null)
        {
            currentSpeed = gameSettings.heroMoveSpeed;
        }
        else
        {
            Debug.LogWarning("ВНИМАНИЕ: Файл GlobalSettingsData не назначен в Character_move! Использую скорость по умолчанию.");
            currentSpeed = 5f;
        }
    }

    private void Start()
    {
        HandManager = FindFirstObjectByType<HandManager>();
        exitUI = FindFirstObjectByType<ExpeditionExitController>();
    }

    public void StartJourney(Vector2Int startPos)
    {
        startNode = startPos;
        currentPosition = startPos;

        // Сбрасываем счетчики при старте
        stepsSinceLastLap = 0;
        lastVisitedCell = new Vector2Int(-9999, -9999);

        RequestNextRoute(startPos);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isMapPaused)
            return;

        if (!isMoving || isWaiting || currentPath == null || currentPath.Count == 0)
            return;

        // Защита от выхода за границы массива
        if (waypointIndex >= currentPath.Count) return;

        Vector2Int targetCell2D = currentPath[waypointIndex];
        GameObject enemyObj = GridGameController.Instance.logic.GetEnemyAt(targetCell2D);

        if (enemyObj != null)
        {
            isMoving = false;

            List<UnitData> enemyUnits = new List<UnitData>();
            EnemySquad enemySquadComponent = enemyObj.GetComponent<EnemySquad>();

            if (enemySquadComponent != null)
            {
                foreach (var member in enemySquadComponent.accumulatedEnemies)
                    if (member != null) enemyUnits.Add(member);
            }

            CombatManager.Instance.StartCombat(this, enemyUnits, targetCell2D, enemyObj);
            return;
        }

        Vector3 targetWorldPos = Tilemap.GetCellCenterWorld(new Vector3Int(targetCell2D.x, targetCell2D.y, 0));
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) <= 0.001f)
        {
            currentPosition = targetCell2D;

            // Выполняем логику клетки ТОЛЬКО если мы на нее физически пришли с другой клетки
            if (currentPosition != lastVisitedCell)
            {
                lastVisitedCell = currentPosition;

                // === ГЛАВНЫЙ ФИКС: Считаем реальные физические шаги ===
                stepsSinceLastLap++;

                CheckForBuilding(targetCell2D);

                if (exitUI != null && GameManager.Instance != null)
                {
                    if (targetCell2D == GameManager.Instance.startTilePosition)
                        exitUI.OnSteppedOnStartTile();
                    else
                        exitUI.OnLeftStartTile();
                }
            }

            waypointIndex++;

            if (waypointIndex >= currentPath.Count)
            {
                StartCoroutine(HandleCrossroadRoutine(targetCell2D));
            }
        }
    }

    public void ResumeMovement()
    {
        isMoving = true;
    }

    private IEnumerator HandleCrossroadRoutine(Vector2Int crossroadCell)
    {
        isWaiting = true;

        // УДАЛЕНО: Сюда больше не нужно лезть с логикой lapsCount++, 
        // потому что теперь это делает Building_Start.OnHeroVisit()

        if (crossroadWaitTime > 0) yield return new WaitForSeconds(crossroadWaitTime);

        RequestNextRoute(crossroadCell);
        isWaiting = false;
    }

    private void RequestNextRoute(Vector2Int currentGridPos)
    {
        if (FILL_MAP_v4.GlobalWaypoints.TryGetValue(currentGridPos, out CoordinateSwitcher switcher))
        {
            currentPath = switcher.GetActivePath();
            waypointIndex = 0;

            if (currentPath != null && currentPath.Count > 1 && currentPath[0] == currentPosition)
            {
                waypointIndex = 1;
            }

            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    public int Round() => lapsCount;

    private void CheckForBuilding(Vector2Int cellPos)
    {
        var instances = GridGameController.Instance.logic.buildingInstances;
        if (instances.TryGetValue(cellPos, out IBuildingLogic building))
        {
            // Герой просто «стучит» в здание, а здание само решает, Старт это или что-то другое
            building.OnHeroVisit(this);
        }
    }
}