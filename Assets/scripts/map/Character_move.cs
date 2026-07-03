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

    // Внутренняя переменная для скорости (берется из gameSettings)
    private float currentSpeed = 5f;

    private bool isMoving = false;
    private bool isWaiting = false;
    private int lapsCount = 0;

    private List<Vector2Int> currentPath;
    private int waypointIndex = 0;
    private Vector2Int startNode;
    private HandManager HandManager;

    // --- ДОБАВЛЕНО: Публичное свойство текущей позиции для UI Эвакуации ---
    public Vector2Int currentPosition { get; private set; }
    private ExpeditionExitController exitUI;
    private void Awake()
    {
        // Безопасное получение настроек при старте игры
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

        // Находим интерфейс один раз при появлении героя, чтобы не искать каждый шаг
        exitUI = FindFirstObjectByType<ExpeditionExitController>();
    }

    public void StartJourney(Vector2Int startPos)
    {
        startNode = startPos;
        currentPosition = startPos; // Устанавливаем начальную позицию
        RequestNextRoute(startPos);
    }

    private void Update()
    {
        // Проверяем, не включена ли пауза
        if (GameManager.Instance != null && GameManager.Instance.isMapPaused)
            return;

        if (!isMoving || isWaiting || currentPath == null || currentPath.Count == 0)
            return;

        Vector2Int targetCell2D = currentPath[waypointIndex];
        GameObject enemyObj = GridGameController.Instance.logic.GetEnemyAt(targetCell2D);

        if (enemyObj != null)
        {
            isMoving = false;

            // CombatManager сам возьмет формацию напрямую из GameManager!
            // Собираем только врагов из объекта, в который мы врезались
            List<UnitData> enemyUnits = new List<UnitData>();
            EnemySquad enemySquadComponent = enemyObj.GetComponent<EnemySquad>();

            if (enemySquadComponent != null)
            {
                foreach (var member in enemySquadComponent.squadMembers)
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

            // Здания (включая фундаменты 0-го уровня) сами решают, что делать с героем
            CheckForBuilding(targetCell2D);

            // === ДОБАВЛЕНО: Push-логика для интерфейса выхода ===
            if (exitUI != null && GameManager.Instance != null)
            {
                if (targetCell2D == GameManager.Instance.startTilePosition)
                {
                    // Сообщаем UI, что мы на старте
                    exitUI.OnSteppedOnStartTile();
                }
                else
                {
                    // Сообщаем UI, что мы ушли со старта
                    exitUI.OnLeftStartTile();
                }
            }
            // ====================================================

            // Только после проверок переключаем индекс на следующую точку
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

        if (crossroadCell == startNode)
        {
            lapsCount++;
            // --- ДОБАВЛЕНО: Сообщаем GameManager'у, что круг завершен! ---
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteExpeditionRound();
            }
        }

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
            isMoving = true;
        }
        else
        {
            isMoving = false;
        }
    }

    public int Round() => lapsCount;

    // --- ЛОГИКА ПОСЕЩЕНИЯ СООРУЖЕНИЙ ---
    private void CheckForBuilding(Vector2Int cellPos)
    {
        if (GridGameController.Instance == null || GridGameController.Instance.logic == null)
        {
            Debug.LogWarning("[MOVE DEBUG] GridGameController или его логика отсутствуют на сцене!");
            return;
        }

        var instances = GridGameController.Instance.logic.buildingInstances;
        if (instances == null)
        {
            Debug.LogWarning("[MOVE DEBUG] Словарь buildingInstances в LogicalGrid равен null!");
            return;
        }

        if (instances.TryGetValue(cellPos, out IBuildingLogic building))
        {
            building.OnHeroVisit(this);
        }
    }
}