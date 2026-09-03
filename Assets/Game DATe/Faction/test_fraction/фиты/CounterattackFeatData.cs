using UnityEngine;

[CreateAssetMenu(fileName = "CounterattackFeat", menuName = "Combat/Feats/Military/Counterattack")]
public class CounterattackFeatData : FeatData
{
    [Header("Настройки контратаки")]
    [Range(0, 100)] public int counterChancePercent = 35;
    public int damageBonus = 0; // пока не используется напрямую, заложено под будущую доработку

    private void OnValidate()
    {
        effectTag = "Counterattack";
        triggerType = FeatType.OnSuccessfulHitTaken;
        reactionChance = counterChancePercent;
    }

    public override void ExecuteEffect(CombatUnit unit, CombatTriggerContext context)
    {
        // Реальное выполнение контратаки происходит в CombatManager.TryCounterattack,
        // здесь оставляем совместимость и отладочный след.
        if (unit != null)
            Debug.Log($"[Контратака] Триггер получен у {unit.UnitName}.");
    }
}