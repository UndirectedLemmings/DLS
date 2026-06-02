using UnityEngine;

public enum FeatType
{
    PassiveStats,   // Пассивное изменение характеристик (здоровье, урон)
    BattleStart,    // Срабатывает один раз в начале боя (например, щит)
    OnAttack,       // Модификатор при атаке (например, вампиризм)
    OnDamageTaken   // Реакция на урон (например, шипы или броня)
}

[CreateAssetMenu(fileName = "NewFeat", menuName = "Combat/Feat")]
public class FeatData : ScriptableObject
{
    public string featName;
    [TextArea(2, 4)]
    public string description;
    public FeatType triggerType;

    [Header("Stat Modifiers (For Passive/Aura)")]
    public int bonusEndurance;
    public int bonusDamage;
    public int damageReduction;

    [Header("Effect Tags")]
    public bool isFactionTrait; // Пометка, если это глобальный фит фракции
    public bool isHeroTrait;    // Пометка, если это уникальная черта героя
}