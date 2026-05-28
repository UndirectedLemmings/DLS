using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Character_move : MonoBehaviour
{
    public Tilemap Tilemap;
    public float speed = 5f;
    public float crossroadWaitTime = 1.0f;

    private bool isMoving = false;
    private bool isWaiting = false;
    private int lapsCount = 0;

    private List<Vector2Int> currentPath;
    private int waypointIndex = 0;
    private Vector2Int startNode;
    private Vector2Int lastCheckedCell;
    private HandManager HandManager;

    private void Start()
    {
        HandManager = FindFirstObjectByType<HandManager>();
    }

    public void StartJourney(Vector2Int startPos)
    {
        startNode = startPos;
        RequestNextRoute(startPos);
    }

    private void Update()
    {
        if (!isMoving || isWaiting || currentPath == null || currentPath.Count == 0)
            return;

        Vector2Int targetCell2D = currentPath[waypointIndex];
        GameObject enemyObj = GridGameController.Instance.logic.GetEnemyAt(targetCell2D);

        if (enemyObj != null)
        {
            isMoving = false;
            List<UnitData> playerUnits = new List<UnitData>();
            FILL_MAP_v4 mapGen = FindFirstObjectByType<FILL_MAP_v4>();

            if (mapGen != null)
            {
                if (mapGen.activeLeader != null) playerUnits.Add(mapGen.activeLeader);
                foreach (var companion in mapGen.activeSquad)
                    if (companion != null) playerUnits.Add(companion);
            }

            List<UnitData> enemyUnits = new List<UnitData>();
            EnemySquad enemySquadComponent = enemyObj.GetComponent<EnemySquad>();

            if (enemySquadComponent != null)
            {
                foreach (var member in enemySquadComponent.squadMembers)
                    if (member != null) enemyUnits.Add(member);
            }

            CombatManager.Instance.StartCombat(this, playerUnits, enemyUnits, targetCell2D, enemyObj);
            return;
        }

        Vector3 targetWorldPos = Tilemap.GetCellCenterWorld(new Vector3Int(targetCell2D.x, targetCell2D.y, 0));
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWorldPos) <= 0.001f)
        {
            CheckForFoundation(targetCell2D);
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
        if (crossroadCell == startNode) lapsCount++;

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

    private void CheckForFoundation(Vector2Int cellPos)
    {
        if (cellPos != lastCheckedCell && FILL_MAP_v4.FoundationCells.Contains(cellPos))
        {
            lastCheckedCell = cellPos;
            if (HandManager != null) HandManager.GiveRandomCardFromPool();
        }
    }
}