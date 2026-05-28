using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    // Виртуальные слоты Арены (по 4 с каждой стороны)
    private CombatUnit[] heroTeam = new CombatUnit[4];
    private CombatUnit[] enemyTeam = new CombatUnit[4];

    // Очередь для орды противников, которые не поместились в первые 4 слота (система волн)
    private Queue<UnitData> enemyBackupQueue = new Queue<UnitData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ИСПРАВЛЕНО: Теперь метод принимает ровно 5 параметров, которые посылает Character_move
    public void StartCombat(Character_move heroMove, List<UnitData> playerHeroes, List<UnitData> enemyUnits, Vector2Int enemyPos, GameObject enemySquadObj)
    {
        // 1. Распределяем героев по 4 слотам арены
        for (int i = 0; i < heroTeam.Length; i++)
        {
            if (i < playerHeroes.Count && playerHeroes[i] != null)
                heroTeam[i] = new CombatUnit(playerHeroes[i], true, i);
            else
                heroTeam[i] = null; // Слот пуст, если героев меньше 4
        }

        // 2. Очищаем очередь подкреплений и распределяем орду врагов
        enemyBackupQueue.Clear();
        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i] == null) continue;

            if (i < 4)
            {
                // Первые 4 моба сразу занимают активные боевые слоты
                enemyTeam[i] = new CombatUnit(enemyUnits[i], false, i);
            }
            else
            {
                // Все остальные (5-й, 6-й и т.д.) уходят в запас ожидать своей волны
                enemyBackupQueue.Enqueue(enemyUnits[i]);
            }
        }

        // Если врагов пришло изначально меньше 4, явно зануляем оставшиеся активные слоты
        for (int i = enemyUnits.Count; i < 4; i++)
        {
            enemyTeam[i] = null;
        }

        // 3. Открываем интерфейс окна боя
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.ShowCombatWindow();
            // СРАЗУ РИСУЕМ АРЕНУ
            CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
            // Очищаем старый лог и рисуем очередь подкреплений
            CombatUIManager.Instance.ClearLog();
            CombatUIManager.Instance.DrawReinforcements(enemyBackupQueue);
        }

        // 4. Запускаем корутину реального боевого цикла вместо заглушки Mockup
        StartCoroutine(BattleLoopRoutine(heroMove, enemyPos, enemySquadObj));
    }

    private IEnumerator BattleLoopRoutine(Character_move hero, Vector2Int enemyPos, GameObject enemySquadObj)
    {
        bool isCombatOver = false;

        while (!isCombatOver)
        {
            // Получаем отсортированную очередь инициативы на текущий раунд (восприятие + 1d10)
            List<CombatUnit> turnQueue = GetSortedInitiativeQueue(heroTeam, enemyTeam);

            // Отрисовываем обновленный таймлайн иконок на UI
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.DrawTimeline(turnQueue);
            }

            // Цикл ходов участников в этом раунде
            foreach (CombatUnit activeFighter in turnQueue)
            {
                // Если бойца убили до того, как до него дошел ход — он пропускает его
                if (activeFighter.IsDead) continue;

                CombatUIManager.Instance.AddLogMessage($"--- Ходит {activeFighter.BaseData.unitName} ---");

                // Простейший выбор цели по умолчанию (зеркальный слот напротив)
                CombatUnit target = null;
                if (activeFighter.IsAttacker)
                {
                    // Ищем живую мишень среди врагов (начиная со слота напротив)
                    target = FindTargetForUnit(activeFighter.SlotIndex, enemyTeam);
                }
                else
                {
                    // Враг ищет живую мишень среди героев
                    target = FindTargetForUnit(activeFighter.SlotIndex, heroTeam);
                }

                // Если цель найдена, атакуем её
                if (target != null)
                {
                    ExecuteAttack(activeFighter, target);
                }

                yield return new WaitForSeconds(1.2f); // Пауза, чтобы лог читался плавно
            }

            // 1. Убираем трупы и выводим подкрепления
            HandleReinforcements();

            // 2. Проверяем, не закончился ли бой
            if (CheckCombatEnd())
            {
                isCombatOver = true;
            }
            else
            {
                CombatUIManager.Instance.AddLogMessage("--- НОВЫЙ РАУНД ---");
                yield return new WaitForSeconds(1.0f); // Пауза между раундами
            }
        } // Конец while (!isCombatOver)

        // --- ЛОГИКА ЗАВЕРШЕНИЯ БОЯ ---
        // (Остается как было)
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.HideCombatWindow();
        GridGameController.Instance.logic.SetEnemyAt(enemyPos, null);
        Destroy(enemySquadObj);
        if (hero != null) hero.ResumeMovement();
        isCombatOver = true;


        // --- ЗАВЕРШЕНИЕ БОЯ ---
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.HideCombatWindow();
        }

        Debug.Log("DLS: Бой окончен.");

        // Очищаем клетку и уничтожаем флаг на карте
        GridGameController.Instance.logic.SetEnemyAt(enemyPos, null);
        Destroy(enemySquadObj);

        // Возвращаем герою возможность идти дальше
        if (hero != null)
        {
            hero.ResumeMovement();
        }
    }

    // Поиск цели: пытается выбрать зеркальный слот, если пуст — ищет любого живого в команде
    private CombatUnit FindTargetForUnit(int slotIndex, CombatUnit[] targetTeam)
    {
        // ИСПРАВЛЕНО: ищем ЖИВУЮ цель напротив (!targetTeam[slotIndex].IsDead)
        if (targetTeam[slotIndex] != null && !targetTeam[slotIndex].IsDead)
            return targetTeam[slotIndex];

        // Если напротив никого нет или там труп, ищем первого попавшегося ЖИВОГО врага
        for (int i = 0; i < targetTeam.Length; i++)
        {
            if (targetTeam[i] != null && !targetTeam[i].IsDead)
                return targetTeam[i];
        }

        // Если живых не осталось
        return null;
    }

    // Соревновательная проверка d10 (Roll Under) по твоим правилам SRD
    private void ExecuteAttack(CombatUnit attacker, CombatUnit defender)
    {
        // Атакующий кидает кубы от Силы
        int attackSuccesses = DiceRoller.RollForSuccesses(attacker.TotalStrength, 0);

        // Защитник кидает кубы от Ловкости (Уклонение)
        int defenseSuccesses = DiceRoller.RollForSuccesses(defender.TotalAgility, 0);

        int wounds = attackSuccesses - defenseSuccesses;

        if (wounds > 0)
        {
            defender.TakeWounds(wounds);
            CombatUIManager.Instance.AddLogMessage($" {attacker.BaseData.unitName} наносит {wounds} ранений по {defender.BaseData.unitName}!");

            if (defender.IsDead)
            {
                CombatUIManager.Instance.AddLogMessage($" {defender.BaseData.unitName} погибает!");
            }
        }
        else
        {
            CombatUIManager.Instance.AddLogMessage($" {defender.BaseData.unitName} успешно защищается!");
        }
    }

    // Сортировка инициативы по правилам: Восприятие + 1d10 -> Разрешение ничьих
    private List<CombatUnit> GetSortedInitiativeQueue(CombatUnit[] heroes, CombatUnit[] enemies)
    {
        List<CombatUnit> activeFighters = new List<CombatUnit>();

        foreach (var hero in heroes)
        {
            // ИСПРАВЛЕНО: добавили восклицательный знак (!hero.IsDead)
            if (hero != null && !hero.IsDead)
            {
                hero.RollInitiative();
                activeFighters.Add(hero);
            }
        }

        foreach (var enemy in enemies)
        {
            // ИСПРАВЛЕНО: добавили восклицательный знак (!enemy.IsDead)
            if (enemy != null && !enemy.IsDead)
            {
                enemy.RollInitiative();
                activeFighters.Add(enemy);
            }
        }

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
            // ИСПРАВЛЕНО: проверяем, что герой НЕ мертв
            if (hero != null && !hero.IsDead) isHeroAlive = true;

        bool isEnemyAlive = false;
        foreach (var enemy in enemyTeam)
            // ИСПРАВЛЕНО: проверяем, что враг НЕ мертв
            if (enemy != null && !enemy.IsDead) isEnemyAlive = true;

        // Если на арене врагов нет, но они есть в очереди - бой продолжается!
        if (enemyBackupQueue.Count > 0) isEnemyAlive = true;

        return !isHeroAlive || !isEnemyAlive;
    }

    private void HandleReinforcements()
    {
        bool UI_NeedsUpdate = false;

        // 1. Проверяем врагов
        for (int i = 0; i < enemyTeam.Length; i++)
        {
            // Если слот пуст или боец мертв...
            if (enemyTeam[i] != null && enemyTeam[i].IsDead)
            {
                UI_NeedsUpdate = true; // Враг умер, нужно обновить UI
                enemyTeam[i] = null;   // Очищаем слот от трупа
            }

            // Если слот пуст и есть резерв - выпускаем
            if (enemyTeam[i] == null && enemyBackupQueue.Count > 0)
            {
                UnitData nextEnemy = enemyBackupQueue.Dequeue();
                enemyTeam[i] = new CombatUnit(nextEnemy, false, i);
                UI_NeedsUpdate = true;

                CombatUIManager.Instance.AddLogMessage($"<color=orange> Подкрепление! {nextEnemy.unitName} вступает в бой!</color>");
            }
        }

        // 2. Очищаем слоты героев от павших
        for (int i = 0; i < heroTeam.Length; i++)
        {
            if (heroTeam[i] != null && heroTeam[i].IsDead)
            {
                heroTeam[i] = null;
                UI_NeedsUpdate = true; // ИСПРАВЛЕНО: Теперь смерть героя тоже дает команду обновить UI!
            }
        }

        // 3. Обновляем картинки на арене, если были изменения
        if (UI_NeedsUpdate && CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.DrawReinforcements(enemyBackupQueue);
            CombatUIManager.Instance.UpdateArena(heroTeam, enemyTeam);
        }
    }


}