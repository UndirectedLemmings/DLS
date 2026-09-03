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

    private int currentRoundIndex = 0;
    private bool isReactionAttackInProgress = false;

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

        TriggerCombatStartFeats(heroTeam);
        TriggerCombatStartFeats(enemyTeam);

        // 3. Запуск интерфейса и цикла
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.ShowCombatWindow();
            CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
            CombatUIManager.Instance.ClearLog();
            CombatUIManager.Instance.DrawReinforcements(enemyBackupQueue);

            // Лог состава команд при старте боя
            CombatUIManager.Instance.AddLogMessage("Состав героев:");
            for (int i = 0; i < heroTeam.Length; i++)
            {
                var u = heroTeam[i];
                if (u != null)
                {
                    string row = (i < 2) ? "передний" : "задний";
                    CombatUIManager.Instance.AddLogMessage($"[{i}] {u.UnitName} ({row} ряд) — Сила:{u.BattleStrength}, Ловкость:{u.BattleAgility}, HP:{u.HealthyEP}+{u.TiredEP}/{u.BattleEndurance}");
                }
            }
            CombatUIManager.Instance.AddLogMessage("Состав врагов:");
            for (int i = 0; i < enemyTeam.Length; i++)
            {
                var u = enemyTeam[i];
                if (u != null)
                {
                    string row = (i < 2) ? "передний" : "задний";
                    CombatUIManager.Instance.AddLogMessage($"[{i}] {u.UnitName} ({row} ряд) — Сила:{u.BattleStrength}, Ловкость:{u.BattleAgility}, HP:{u.HealthyEP}+{u.TiredEP}/{u.BattleEndurance}");
                }
            }
        }

        StartCoroutine(BattleLoopRoutine(heroMove, enemyPos, enemySquadObj));
    }

    private void TriggerCombatStartFeats(CombatUnit[] units)
    {
        if (units == null) return;

        foreach (var unit in units)
        {
            if (unit == null || unit.featController == null) continue;
            unit.featController.ExecuteTriggers(FeatType.OnBattleStart);
        }
    }

    private void TriggerPhaseForUnit(CombatUnit unit, FeatType phase, CombatTriggerContext context = null)
    {
        if (unit == null || unit.IsDead || unit.featController == null) return;
        unit.featController.ExecuteTriggers(phase, context);
    }

    private void TriggerPhaseForTeam(CombatUnit[] team, FeatType phase, CombatTriggerContext context = null)
    {
        if (team == null) return;
        foreach (var unit in team)
            TriggerPhaseForUnit(unit, phase, context);
    }

    private void TriggerOtherUnitsPhase(CombatUnit activeUnit, FeatType phase, CombatTriggerContext context = null)
    {
        foreach (var unit in heroTeam)
        {
            if (unit == null || unit == activeUnit || unit.IsDead || unit.featController == null) continue;
            unit.featController.ExecuteTriggers(phase, context);
        }

        foreach (var unit in enemyTeam)
        {
            if (unit == null || unit == activeUnit || unit.IsDead || unit.featController == null) continue;
            unit.featController.ExecuteTriggers(phase, context);
        }
    }

    private IEnumerator BattleLoopRoutine(Character_move hero, Vector2Int enemyPos, GameObject enemySquadObj)
    {
        bool isCombatOver = false;

        while (!isCombatOver)
        {
            currentRoundIndex++;
            var roundContext = new CombatTriggerContext
            {
                CombatManager = this,
                IsReaction = false
            };

            TriggerPhaseForTeam(heroTeam, FeatType.OnRoundStart, roundContext);
            TriggerPhaseForTeam(enemyTeam, FeatType.OnRoundStart, roundContext);

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

                var turnContext = new CombatTriggerContext
                {
                    Source = activeFighter,
                    CombatManager = this,
                    IsReaction = false
                };

                TriggerPhaseForUnit(activeFighter, FeatType.OnTurnStart, turnContext);
                TriggerOtherUnitsPhase(activeFighter, FeatType.OnOtherUnitTurnStart, turnContext);

                // --- ВКЛЮЧАЕМ ИНДИКАЦИЮ АКТИВНОГО ЮНИТА ---
                activeFighter.SetActiveVisual(true);

                string activeRow = activeFighter.SlotIndex < 2 ? "передний" : "задний";
                CombatUIManager.Instance.AddLogMessage($"--- Ходит {activeFighter.UnitName} (слот {activeFighter.SlotIndex}, {activeRow} ряд) ---");
                CombatUIManager.Instance.AddLogMessage($"Статусы: HP {activeFighter.HealthyEP} | Усталость {activeFighter.TiredEP} | Раны {activeFighter.WoundedEP} / Выносливость {activeFighter.BattleEndurance}");

                CombatUnit[] targetTeam = activeFighter.IsAttacker ? enemyTeam : heroTeam;
                CombatUnit target = GetSmartTarget(activeFighter, targetTeam);

                if (target != null)
                {
                    // 1. ПОДСВЕЧИВАЕМ ЦЕЛЬ
                    target.SetTargetVisual(true);

                    // 2. ДАЕМ ПАУЗУ, ЧТОБЫ ИГРОК ПОНЯЛ, КТО ЦЕЛЬ
                    string targetRow = target.SlotIndex < 2 ? "передний" : "задний";
                    CombatUIManager.Instance.AddLogMessage($"{activeFighter.UnitName} (слот {activeFighter.SlotIndex}) целится в {target.UnitName} (слот {target.SlotIndex}, {targetRow} ряд)...");
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

                TriggerPhaseForUnit(activeFighter, FeatType.OnTurnEnd, turnContext);
                TriggerOtherUnitsPhase(activeFighter, FeatType.OnOtherUnitTurnEnd, turnContext);

                // --- ВЫКЛЮЧАЕМ ИНДИКАЦИЮ АКТИВНОГО ЮНИТА ---
                activeFighter.SetActiveVisual(false);

                yield return new WaitWhile(() => GameManager.Instance != null && GameManager.Instance.isMapPaused);
                yield return new WaitForSeconds(0.5f); // Немного отдыха перед следующим ходом
            }

            HandleReinforcements();

            TriggerPhaseForTeam(heroTeam, FeatType.OnRoundEnd, roundContext);
            TriggerPhaseForTeam(enemyTeam, FeatType.OnRoundEnd, roundContext);

            if (CheckCombatEnd())
            {
                isCombatOver = true;
            }
            else
            {
                CombatUIManager.Instance.AddLogMessage($"--- НОВЫЙ РАУНД ({currentRoundIndex + 1}) ---");
                yield return new WaitForSeconds(1.0f);
            }
        }

        TriggerPhaseForTeam(heroTeam, FeatType.OnBattleEnd, new CombatTriggerContext { CombatManager = this });
        TriggerPhaseForTeam(enemyTeam, FeatType.OnBattleEnd, new CombatTriggerContext { CombatManager = this });

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
        string attackerSide = attacker.IsAttacker ? "Герой" : "Враг";
        string targetSide = target.IsAttacker ? "Герой" : "Враг";
        string attackerRow = attacker.SlotIndex < 2 ? "передний" : "задний";
        string targetRow = target.SlotIndex < 2 ? "передний" : "задний";
        CombatUIManager.Instance.AddLogMessage($"{attackerSide} {attacker.UnitName} (слот {attacker.SlotIndex}, {attackerRow} ряд) атакует {targetSide} {target.UnitName} (слот {target.SlotIndex}, {targetRow} ряд)!");

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
        CombatUIManager.Instance.AddLogMessage($"Атака ({TranslateStatName(attackStatType)} {attackerValue}) + кубов:{attacker.CurrentWeaponBonusDice}: {attackSuccesses} усп. {attackRolls}");
        CombatUIManager.Instance.AddLogMessage($"Защита ({TranslateStatName(defenseStatType)} {targetValue}): {defenseSuccesses} усп. {defenseRolls}");

        if (attackSuccesses > defenseSuccesses)
        {
            // Попадание!
            int netHits = attackSuccesses - defenseSuccesses;

            // XP за успешную атаку — только героям, за использованную атакующую характеристику
            if (attacker.IsAttacker)
                AwardXpForSuccesses(attacker, attackStatType, attackSuccesses);

            // --- БРОСКИ НА УРОН ---
            string damageRolls;
            int damageSuccesses = DiceRoller.RollForSuccesses(attacker.BattleStrength, 0, out damageRolls);

            CombatUIManager.Instance.AddLogMessage($"Урон (Сила {attacker.BattleStrength}): {damageSuccesses} усп. {damageRolls}");

            int finalDamage = damageSuccesses + (netHits - 1);

            // XP за успешный урон (броски Силы) — только героям
            if (attacker.IsAttacker && damageSuccesses > 0)
                AwardXpForSuccesses(attacker, CharacterStatType.Strength, damageSuccesses);

            if (finalDamage > 0)
            {
                CombatUIManager.Instance.AddLogMessage($"<color=red>{target.UnitName} получает {finalDamage} ран!</color>");
                target.TakeWounds(finalDamage);
                // Показываем текущее состояние цели после получения ран
                CombatUIManager.Instance.AddLogMessage($"Статусы {target.UnitName}: HP {target.HealthyEP} | Усталость {target.TiredEP} | Раны {target.WoundedEP} / Выносливость {target.BattleEndurance}");
            }
            else
            {
                CombatUIManager.Instance.AddLogMessage("Броня поглотила урон!");
            }

            var hitContext = new CombatTriggerContext
            {
                Source = attacker,
                Target = target,
                CombatManager = this,
                IsReaction = isReactionAttackInProgress,
                AttackSuccesses = attackSuccesses,
                DefenseSuccesses = defenseSuccesses,
                FinalDamage = finalDamage
            };

            // События успешного попадания
            TriggerPhaseForUnit(attacker, FeatType.OnSuccessfulHit, hitContext);
            TriggerPhaseForUnit(target, FeatType.OnSuccessfulHitTaken, hitContext);

            // Контратака: только не в реакционной атаке, только если цель жива
            TryCounterattack(target, attacker, hitContext);
        }
        else
        {
            CombatUIManager.Instance.AddLogMessage($"{target.UnitName} успешно защищается от атаки!");

            // XP защищавшемуся герою за успешную защиту
            if (target.IsAttacker && defenseSuccesses > 0)
                AwardXpForSuccesses(target, defenseStatType, defenseSuccesses);
        }
    }

    /// <summary>
    /// Начисляет XP герою за успешные броски. +1 XP за каждый успех.
    /// Работает только для героев (IsAttacker), не для врагов.
    /// </summary>
    private void AwardXpForSuccesses(CombatUnit unit, CharacterStatType stat, int successes)
    {
        if (!unit.IsAttacker || GameManager.Instance == null) return;

        // Находим UnitProgress героя по слоту
        UnitProgress progress = GameManager.Instance.combatFormation[unit.SlotIndex];
        if (progress == null) return;

        bool levelUp = progress.AddXP(stat, successes);
        if (levelUp)
            CombatUIManager.Instance.AddLogMessage(
                $"<color=yellow>★ {unit.UnitName}: +1 к {TranslateStatName(stat)}! (прирост от опыта)</color>");
    }

    private void TryCounterattack(CombatUnit defender, CombatUnit originalAttacker, CombatTriggerContext hitContext)
    {
        if (isReactionAttackInProgress) return;
        if (defender == null || originalAttacker == null) return;
        if (defender.IsDead || originalAttacker.IsDead) return;
        if (defender.featController == null) return;

        FeatData counterFeat = defender.featController.FindFirstFeatByTag("Counterattack");
        if (counterFeat == null) return;

        int chance = Mathf.Clamp(counterFeat.reactionChance, 0, 100);
        if (chance <= 0) chance = 35;

        if (Random.Range(0, 100) >= chance)
        {
            CombatUIManager.Instance.AddLogMessage($"{defender.UnitName} пытается контратаковать, но не успевает ({chance}%).");
            return;
        }

        CombatUIManager.Instance.AddLogMessage($"<color=orange>{defender.UnitName} выполняет контратаку!</color>");

        isReactionAttackInProgress = true;
        ExecuteAttack(defender, originalAttacker);
        isReactionAttackInProgress = false;

        CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
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