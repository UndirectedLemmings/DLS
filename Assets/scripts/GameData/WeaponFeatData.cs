using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Feat", menuName = "Combat/Weapon Feat")]
public class WeaponFeatData : FeatData
{

    [Tooltip("Роль оружия: кого этот юнит будет пытаться ударить первым")]
    public TargetPriority preferredTarget;

    [Header("Эффекты при попадании")]
    [Tooltip("Шанс срабатывания эффекта (0-100)")]
    public int effectChance = 0;
    public string effectDescription;

    // Переопределяем логику фита, чтобы он применял бонусы к статам
    public override void ExecuteEffect(CombatUnit unit)
    {
        // Применяем статы через контроллер
        unit.combatBonusStrength += bonusStrength;
        unit.combatBonusAgility += bonusAgility;

        Debug.Log($"[Weapon] {unit.UnitName} экипировал {this.featName}.");
    }
}