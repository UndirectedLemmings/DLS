using UnityEngine;
using System.Collections.Generic;

public class GlobalFactionDirector : MonoBehaviour
{
    private void OnEnable()
    {
        StartCoroutine(SubscribeToGameManager());
    }

    private System.Collections.IEnumerator SubscribeToGameManager()
    {
        // Ждем, пока синглтон GameManager инициализируется на сцене
        yield return new WaitUntil(() => GameManager.Instance != null);
        GameManager.Instance.OnNewLapStarted += CheckAllFactionsEscalation;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnNewLapStarted -= CheckAllFactionsEscalation;
        }
    }

    // --- ГЛОБАЛЬНАЯ ПРОВЕРКА ДЛЯ ВСЕХ ФРАКЦИЙ ---
    private void CheckAllFactionsEscalation()
    {
        // Берем список фракций напрямую из GameManager
        // Предполагается, что в GameManager у тебя есть поле, например: public List<FactionData> currentFactions;
        if (GameManager.Instance == null || GameManager.Instance.currentFactions == null)
        {
            Debug.LogWarning("[РЕЖИССЕР] Список фракций в GameManager пуст или не инициализирован!");
            return;
        }

        int currentRound = GameManager.Instance.currentExpeditionRound;
        List<FactionData> activeFactions = GameManager.Instance.currentFactions;

        // Пробегаемся по КАЖДОЙ фракции, участвующей в экспедиции
        foreach (FactionData faction in activeFactions)
        {
            if (faction == null || faction.factionEscalationFeat == null) continue;

            // Проверяем правила спавна чемпионов для КОНКРЕТНОЙ фракции
            foreach (var rule in faction.factionEscalationFeat.championRules)
            {
                if (currentRound >= rule.minRound)
                {
                    if (Random.Range(0f, 100f) <= rule.spawnChance)
                    {
                        // Пытаемся заспавнить босса этой конкретной фракции
                        TrySpawnChampionForFaction(faction, rule.championMob);

                        // Выходим из цикла правил этой фракции, чтобы не заспавнить несколько разных боссов одной расы за один круг
                        break;
                    }
                }
            }
        }
    }

    // ---УМНЫЙ СПАВН НА ДОРОГИ КОНКРЕТНОЙ ФРАКЦИИ ---
    private void TrySpawnChampionForFaction(FactionData faction, UnitData championTemplate)
    {
        if (championTemplate is not EnemyData enemyData) return;

        // 1. Находим вообще все куски дорог на карте
        RoadSegmentManager[] allRoads = FindObjectsByType<RoadSegmentManager>(FindObjectsSortMode.None);
        if (allRoads.Length == 0) return;

        // 2. Отфильтровываем только те дороги, которые принадлежат ИМЕННО ЭТОЙ фракции
        List<RoadSegmentManager> specificFactionRoads = new List<RoadSegmentManager>();
        foreach (var road in allRoads)
        {
            if (road.ownerFaction == faction)
            {
                specificFactionRoads.Add(road);
            }
        }

        RoadSegmentManager targetRoad = null;

        // 3. Выбираем точку спавна
        if (specificFactionRoads.Count > 0)
        {
            // Идеально: Спавним на случайную дорогу этой фракции
            targetRoad = specificFactionRoads[Random.Range(0, specificFactionRoads.Count)];
        }
        else
        {
            // Игрок уничтожил все дороги этой фракции! Но босс все равно должен прийти.
            // Спавним на абсолютно любую случайную дорогу на карте
            targetRoad = allRoads[Random.Range(0, allRoads.Length)];
            Debug.Log($"[РЕЖИССЕР] Дороги фракции {faction.factionName} уничтожены! Чемпион заспавнен на случайный сектор.");
        }

        if (targetRoad != null)
        {
            targetRoad.SpawnCreatureOnRoad(enemyData);
            Debug.LogWarning($"[РЕЖИССЕР] ВНИМАНИЕ! Из глубин эскалации появляется Чемпион фракции {faction.factionName}: {championTemplate.unitName}!");
        }
    }
}