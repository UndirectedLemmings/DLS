using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    private CombatUnit[] heroTeam = new CombatUnit[4];
    private CombatUnit[] enemyTeam = new CombatUnit[4];
    private Queue<UnitData> enemyBackupQueue = new Queue<UnitData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartCombat(Character_move heroMove, List<UnitData> playerHeroes, List<UnitData> enemyUnits, Vector2Int enemyPos, GameObject enemySquadObj)
    {
        for (int i = 0; i < heroTeam.Length; i++)
        {
            if (i < playerHeroes.Count && playerHeroes[i] != null)
                heroTeam[i] = new CombatUnit(playerHeroes[i], true, i);
            else
                heroTeam[i] = null;
        }

        enemyBackupQueue.Clear();
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i] == null) continue;

            if (i < 4)
                enemyTeam[i] = new CombatUnit(enemyUnits[i], false, i);
            else
                enemyBackupQueue.Enqueue(enemyUnits[i]);
        }

        for (int i = enemyUnits.Count; i < 4; i++)
        {
            enemyTeam[i] = null;
        }

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.ShowCombatWindow();
            CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
            CombatUIManager.Instance.ClearLog();
            CombatUIManager.Instance.DrawReinforcements(enemyBackupQueue);
        }

        StartCoroutine(BattleLoopRoutine(heroMove, enemyPos, enemySquadObj));
    }

    private void TriggerStartOfCombat()
    {
        foreach (var unit in playerUnits)
        {
            // Менеджер просто говорит: "Бой начался! Выполните свои действия!"
            unit.featController.ExecuteTriggers(FeatType.OnCombatStart, null);
        }
    }
    private IEnumerator BattleLoopRoutine(Character_move hero, Vector2Int enemyPos, GameObject enemySquadObj)
    {
        bool isCombatOver = false;

        while (!isCombatOver)
        {
            List<CombatUnit> turnQueue = GetSortedInitiativeQueue(heroTeam, enemyTeam);

            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.DrawTimeline(turnQueue);
            }

            foreach (CombatUnit activeFighter in turnQueue)
            {
                if (activeFighter.IsDead) continue;

                CombatUIManager.Instance.AddLogMessage($"--- Ходит {activeFighter.BaseData.unitName} ---");

                CombatUnit target = null;
                if (activeFighter.IsAttacker)
                    target = FindTargetForUnit(activeFighter.SlotIndex, enemyTeam);
                else
                    target = FindTargetForUnit(activeFighter.SlotIndex, heroTeam);

                if (target != null)
                {
                    ExecuteAttack(activeFighter, target);
                    // Сразу обновляем визуал аренных слотов после атаки
                    CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
                }

                yield return new WaitForSeconds(1.2f);
            }

            // Важно: сначала проверяем окончание, подкрепления выйдут в начале следующего раунда или в фазе обработки
            HandleReinforcements();

            if (CheckCombatEnd())
            {
                isCombatOver = true;
            }
            else
            {
                CombatUIManager.Instance.AddLogMessage("--- НОВЫЙ РАУНД ---");
                yield return new WaitForSeconds(1.0f);
            }
        }

        // ОЧИЩЕННЫЙ БЛОК ЗАВЕРШЕНИЯ (Без дубликатов)
        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.HideCombatWindow();

        GridGameController.Instance.logic.SetEnemyAt(enemyPos, null);

        if (enemySquadObj != null)
            Destroy(enemySquadObj);

        if (hero != null)
            hero.ResumeMovement();

        Debug.Log("DLS: Бой успешно завершен, объекты зачищены.");
    }

    private CombatUnit FindTargetForUnit(int slotIndex, CombatUnit[] targetTeam)
    {
        if (slotIndex < targetTeam.Length && targetTeam[slotIndex] != null && !targetTeam[slotIndex].IsDead)
            return targetTeam[slotIndex];

        for (int i = 0; i < targetTeam.Length; i++)
        {
            if (targetTeam[i] != null && !targetTeam[i].IsDead)
                return targetTeam[i];
        }
        return null;
    }

    private void ExecuteAttack(CombatUnit attacker, CombatUnit defender)
    {
        int attackSuccesses = DiceRoller.RollForSuccesses(attacker.TotalStrength, 0);
        int defenseSuccesses = DiceRoller.RollForSuccesses(defender.TotalAgility, 0);
        int wounds = attackSuccesses - defenseSuccesses;

        if (wounds > 0)
        {
            defender.TakeWounds(wounds);
            CombatUIManager.Instance.AddLogMessage($"{attacker.BaseData.unitName} наносит {wounds} ран по {defender.BaseData.unitName}!");

            if (defender.IsDead)
                CombatUIManager.Instance.AddLogMessage($"{defender.BaseData.unitName} погибает!");
        }
        else
        {
            CombatUIManager.Instance.AddLogMessage($"{defender.BaseData.unitName} парирует атаку!");
        }
    }

    private List<CombatUnit> GetSortedInitiativeQueue(CombatUnit[] heroes, CombatUnit[] enemies)
    {
        List<CombatUnit> activeFighters = new List<CombatUnit>();

        foreach (var hero in heroes)
            if (hero != null && !hero.IsDead) { hero.RollInitiative(); activeFighters.Add(hero); }

        foreach (var enemy in enemies)
            if (enemy != null && !enemy.IsDead) { enemy.RollInitiative(); activeFighters.Add(enemy); }

        activeFighters.Sort((a, b) =>
        {
            int initCompare = b.InitiativeRoll.CompareTo(a.InitiativeRoll);
            if (initCompare != 0) return initCompare;
            if (a.IsAttacker && !b.IsAttacker) return -1;
            if (!a.IsAttacker && b.IsAttacker) return 1;
            return a.SlotIndex.CompareTo(b.SlotIndex);
        });

        return activeFighters;
    }

    private bool CheckCombatEnd()
    {
        bool isHeroAlive = false;
        foreach (var hero in heroTeam)
            if (hero != null && !hero.IsDead) isHeroAlive = true;

        bool isEnemyAlive = false;
        foreach (var enemy in enemyTeam)
            if (enemy != null && !enemy.IsDead) isEnemyAlive = true;

        if (enemyBackupQueue.Count > 0) isEnemyAlive = true;

        return !isHeroAlive || !isEnemyAlive;
    }

    private void HandleReinforcements()
    {
        bool uiNeedsUpdate = false;

        for (int i = 0; i < enemyTeam.Length; i++)
        {
            if (enemyTeam[i] != null && enemyTeam[i].IsDead)
            {
                enemyTeam[i] = null;
                uiNeedsUpdate = true;
            }

            if (enemyTeam[i] == null && enemyBackupQueue.Count > 0)
            {
                UnitData nextEnemy = enemyBackupQueue.Dequeue();
                enemyTeam[i] = new CombatUnit(nextEnemy, false, i);
                uiNeedsUpdate = true;

                CombatUIManager.Instance.AddLogMessage($"<color=orange>Подкрепление! {nextEnemy.unitName} выходит на поле!</color>");
            }
        }

        for (int i = 0; i < heroTeam.Length; i++)
        {
            if (heroTeam[i] != null && heroTeam[i].IsDead)
            {
                heroTeam[i] = null;
                uiNeedsUpdate = true;
            }
        }

        if (uiNeedsUpdate && CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.DrawReinforcements(enemyBackupQueue);
            CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
        }
    }
}