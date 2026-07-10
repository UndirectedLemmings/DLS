using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactionEscalation", menuName = "Game Data/Faction Escalation")]
public class FactionEscalationFeatData : FeatData
{
    [Header("Эскалация Фитов (Мутации)")]
    [Tooltip("Список фитов, которые моб может получить на определенных кругах")]
    public List<EscalationFeatRule> featRules;

    [Header("Эскалация Мобoв (Чемпионы)")]
    [Tooltip("Список мобов, которые могут появиться на карте вместо обычных")]
    public List<EscalationMobRule> championRules;

    [Header("Фракционный лут")]
    [Tooltip("Лут, который добавляется к пулу при смерти обладателя этого фита")]
    public List<LootEntry> factionLoot;

    // Этот метод сработает при старте боя (или при инициализации юнита)
    public override void ExecuteEffect(CombatUnit unit)
    {
        // Узнаем текущий круг экспедиции
        int currentRound = GameManager.Instance.currentExpeditionRound;

        // Проверяем все правила мутаций
        foreach (var rule in featRules)
        {
            if (currentRound >= rule.minRound)
            {
                // Считаем шанс: (например, 10% * 3 круг = 30%)
                float totalChance = rule.chanceMultiplierPerRound * currentRound;

                // Бросаем кубик (от 0 до 100)
                if (Random.Range(0f, 100f) <= totalChance)
                {
                    // Выдаем бонусный фит!
                    // ИСПРАВЛЕНО: обращаемся к featController
                    if (unit.featController != null)
                    {
                        unit.featController.AddEquipmentFeat(rule.bonusFeat);

                        // ИСПРАВЛЕНО: обращаемся к featName, а не cardName
                        Debug.Log($"[Эскалация] {unit.UnitName} мутировал! Получен фит: {rule.bonusFeat.featName}");
                    }
                }
            }
        }
    }
}

// --- СТРУКТУРЫ ПРАВИЛ ---
[System.Serializable]
public struct EscalationFeatRule
{
    public string ruleName;
    public int minRound;
    public float chanceMultiplierPerRound;
    public FeatData bonusFeat;
}

[System.Serializable]
public struct EscalationMobRule
{
    public string ruleName;
    public int minRound;
    [Range(0, 100)] public float spawnChance;
    public UnitData championMob;
}