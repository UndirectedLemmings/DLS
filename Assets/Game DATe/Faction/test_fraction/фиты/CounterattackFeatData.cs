using UnityEngine;

[CreateAssetMenu(fileName = "CounterattackFeat", menuName = "Combat/Feats/Military/Counterattack")]
public class CounterattackFeatData : FeatData
{
    [Header("Настройки контратаки")]
    public int damageBonus = 0; // Доп урон от контратаки, если нужен

    // Допустим, у нас в FeatType есть триггер OnAttacked или OnTakeDamage
    public override void ExecuteEffect(CombatUnit unit)
    {
        // Логика зависит от твоей боевки. 
        // Обычно мы здесь говорим: 
        // 1. Проверь, кто атаковал
        // 2. Нанеси ему базовый урон юнита + damageBonus
        Debug.Log($"[Контратака] {unit.Progress.Template.unitName} контратакует!");

        // Пример (если у тебя есть ссылка на последнюю цель или метод атаки):
        // CombatManager.Instance.DealDamage(unit, unit.lastAttacker, unit.Progress.Strength + damageBonus);
    }
}