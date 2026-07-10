using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static FeatData;

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

    public void StartCombat(Character_move heroMove, List<UnitData> enemyUnits, Vector2Int enemyPos, GameObject enemySquadObj)
    {
        // 1. --- ОБНОВЛЕНО: Берем героев строго из формации GameManager ---
        for (int i = 0; i < heroTeam.Length; i++)
        {
            if (GameManager.Instance != null && GameManager.Instance.combatFormation[i] != null)
            {
                heroTeam[i] = new CombatUnit(GameManager.Instance.combatFormation[i], true, i);
            }
            else
            {
                heroTeam[i] = null; // Слот пуст
            }
        }

        enemyBackupQueue.Clear();
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i] == null) continue;

            // ВАЖНО: Создаем прогресс на лету, так как врагам не нужно сохранение
            UnitProgress tempProgress = new UnitProgress(enemyUnits[i]);

            if (i < 4)
                enemyTeam[i] = new CombatUnit(tempProgress, false, i);
            else
                enemyBackupQueue.Enqueue(enemyUnits[i]); // Очередь хранит шаблоны
        }

        // 3. Запуск интерфейса и цикла
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.ShowCombatWindow();
            CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
            CombatUIManager.Instance.ClearLog();
            CombatUIManager.Instance.DrawReinforcements(enemyBackupQueue);
        }

        StartCoroutine(BattleLoopRoutine(heroMove, enemyPos, enemySquadObj));
    }

    /*private void TriggerStartOfCombat()
    {
        foreach (var unit in playerUnits)
        {
            // Менеджер просто говорит: "Бой начался! Выполните свои действия!"
            unit.featController.ExecuteTriggers(FeatType.OnCombatStart, null);
        }
    }*/
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
                // Проверка на паузу
                yield return new WaitWhile(() => GameManager.Instance != null && GameManager.Instance.isMapPaused);

                if (activeFighter.IsDead) continue;

                // --- ВКЛЮЧАЕМ ИНДИКАЦИЮ АКТИВНОГО ЮНИТА ---
                activeFighter.SetActiveVisual(true);

                CombatUIManager.Instance.AddLogMessage($"--- Ходит {activeFighter.UnitName} ---");

                CombatUnit[] targetTeam = activeFighter.IsAttacker ? enemyTeam : heroTeam;
                CombatUnit target = GetSmartTarget(activeFighter, targetTeam);

                if (target != null)
                {
                    // 1. ПОДСВЕЧИВАЕМ ЦЕЛЬ
                    target.SetTargetVisual(true);

                    // 2. ДАЕМ ПАУЗУ, ЧТОБЫ ИГРОК ПОНЯЛ, КТО ЦЕЛЬ
                    CombatUIManager.Instance.AddLogMessage($"{activeFighter.UnitName} целится в {target.UnitName}...");
                    yield return new WaitForSeconds(0.8f);

                    // 3. АТАКУЕМ
                    ExecuteAttack(activeFighter, target);
                    CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);

                    // 4. ПАУЗА ПОСЛЕ УДАРА, ЧТОБЫ УСПЕТЬ УВИДЕТЬ РЕЗУЛЬТАТ
                    yield return new WaitForSeconds(0.5f);

                    // 5. УБИРАЕМ ПРИЦЕЛ
                    target.SetTargetVisual(false);
                }
                else
                {
                    CombatUIManager.Instance.AddLogMessage($"{activeFighter.UnitName} не видит целей!");
                }

                // --- ВЫКЛЮЧАЕМ ИНДИКАЦИЮ АКТИВНОГО ЮНИТА ---
                activeFighter.SetActiveVisual(false);

                yield return new WaitWhile(() => GameManager.Instance != null && GameManager.Instance.isMapPaused);
                yield return new WaitForSeconds(0.5f); // Немного отдыха перед следующим ходом
            }

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

        // Завершение боя
        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.HideCombatWindow();

        GridGameController.Instance.logic.SetEnemyAt(enemyPos, null);

        if (enemySquadObj != null)
            Destroy(enemySquadObj);

        if (hero != null)
            hero.ResumeMovement();
    }

    private void ExecuteAttack(CombatUnit attacker, CombatUnit target)
    {
        CombatUIManager.Instance.AddLogMessage($"{attacker.UnitName} атакует {target.UnitName}!");

        // 1. Узнаем, какие статы диктует оружие атакующего через его контроллер фитов
        CharacterStatType attackStatType = attacker.featController.CurrentAttackStat;
        CharacterStatType defenseStatType = attacker.featController.CurrentDefenseStat;

        // 2. Получаем числовые значения этих характеристик для обоих юнитов
        int attackerValue = attacker.GetBattleStatValue(attackStatType);
        int targetValue = target.GetBattleStatValue(defenseStatType);

        // --- БРОСКИ НА ПОПАДАНИЕ ---
        string attackRolls, defenseRolls;

        // Используем динамически полученную атакущую характеристику
        int attackSuccesses = DiceRoller.RollForSuccesses(
            attackerValue,
            attacker.CurrentWeaponBonusDice,
            out attackRolls
        );

        // Используем динамически полученную целевую характеристику защиты
        int defenseSuccesses = DiceRoller.RollForSuccesses(
            targetValue,
            0,
            out defenseRolls
        );

        // Выводим адаптивные результаты в лог, чтобы игрок понимал, какие статы сработали
        CombatUIManager.Instance.AddLogMessage($"Атака ({TranslateStatName(attackStatType)} {attackerValue}): {attackSuccesses} усп. {attackRolls}");
        CombatUIManager.Instance.AddLogMessage($"Защита ({TranslateStatName(defenseStatType)} {targetValue}): {defenseSuccesses} усп. {defenseRolls}");

        if (attackSuccesses > defenseSuccesses)
        {
            // Попадание!
            int netHits = attackSuccesses - defenseSuccesses;

            // --- БРОСКИ НА УРОН ---
            string damageRolls;
            int damageSuccesses = DiceRoller.RollForSuccesses(attacker.BattleStrength, 0, out damageRolls);

            CombatUIManager.Instance.AddLogMessage($"Урон (Сила {attacker.BattleStrength}): {damageSuccesses} усп. {damageRolls}");

            int finalDamage = damageSuccesses + (netHits - 1);

            if (finalDamage > 0)
            {
                CombatUIManager.Instance.AddLogMessage($"<color=red>{target.UnitName} получает {finalDamage} ран!</color>");
                target.TakeWounds(finalDamage);
            }
            else
            {
                CombatUIManager.Instance.AddLogMessage("Броня поглотила урон!");
            }
        }
        else
        {
            CombatUIManager.Instance.AddLogMessage($"{target.UnitName} успешно защищается от атаки!");
        }
    }

    // Небольшой вспомогательный метод для красивого вывода статов в текстовый лог боя
    private string TranslateStatName(CharacterStatType statType)
    {
        switch (statType)
        {
            case CharacterStatType.Strength: return "Сила";
            case CharacterStatType.Endurance: return "Выносливость";
            case CharacterStatType.Will: return "Воля";
            case CharacterStatType.Wisdom: return "Мудрость";
            case CharacterStatType.Agility: return "Ловкость";
            case CharacterStatType.Perception: return "Восприятие";
            default: return "Ловкость";
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

    private CombatUnit GetSmartTarget(CombatUnit attacker, CombatUnit[] enemyTeam)
    {
        TargetPriority priority = attacker.featController.GetTargetPriority();

        CombatUnit bestTarget = null;
        int currentBestValue = -1; // Для поиска макс/мин HP

        for (int i = 0; i < enemyTeam.Length; i++)
        {
            CombatUnit potentialTarget = enemyTeam[i];

            // Игнорируем пустые слоты и мертвецов
            if (potentialTarget == null || potentialTarget.IsDead) continue;

            switch (priority)
            {
                case TargetPriority.Frontline:
                    // Возвращаем первого же попавшегося живого
                    return potentialTarget;

                case TargetPriority.Backline:
                    // Просто перезаписываем цель. В итоге останется самый последний живой
                    bestTarget = potentialTarget;
                    break;

                case TargetPriority.LowestHP:
                    // Ищем наименьшее здоровье
                    if (bestTarget == null || potentialTarget.HealthyEP < currentBestValue)
                    {
                        bestTarget = potentialTarget;
                        currentBestValue = potentialTarget.HealthyEP;
                    }
                    break;

                case TargetPriority.HighestHP:
                    // Ищем наибольшее здоровье (или выносливость)
                    if (bestTarget == null || potentialTarget.HealthyEP > currentBestValue)
                    {
                        bestTarget = potentialTarget;
                        currentBestValue = potentialTarget.HealthyEP;
                    }
                    break;
            }
        }

        // Возвращаем найденную цель (даже если Backline/HP ничего не нашли, fallback будет null)
        return bestTarget;
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
                UnitData nextEnemyData = enemyBackupQueue.Dequeue();
                UnitProgress nextProgress = new UnitProgress(nextEnemyData); // Тоже создаем прогресс
                enemyTeam[i] = new CombatUnit(nextProgress, false, i);
                uiNeedsUpdate = true;

                CombatUIManager.Instance.AddLogMessage($"<color=orange>Подкрепление! {nextEnemyData.unitName} выходит на поле!</color>");
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