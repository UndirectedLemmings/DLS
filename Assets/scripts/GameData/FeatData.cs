using UnityEngine;
using System.Collections.Generic;

public enum FeatCategory
{
    Generic,        // Обычный перк
    Class,          // Классовый фит (Воин, Маг, Стрелок)
    Faction,        // Фракционная особенность
    Hero,           // Уникальная черта героя
    StatusEffect    // Временный бафф/дебафф (Яд, Щит)
}
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

    [Header("Категоризация")]
    public FeatCategory category; // <-- Наша новая система категорий
    public FeatType triggerType;

    [Header("Зеркальные свойства (Теги)")]
    [Tooltip("Тег этого фита, например: Poison, Regen, Fire, Ice")]
    public string effectTag;
    [Tooltip("Список тегов, которые этот фит уничтожает при наложении")]
    public List<string> cancelsTags = new List<string>();

    [Header("Stat Modifiers (For Passive/Aura)")]
    public int bonusEndurance;
    public int bonusDamage;
    public int damageReduction;

    [Header("Effect Tags")]
    public bool isFactionTrait; // Пометка, если это глобальный фит фракции
    public bool isHeroTrait;    // Пометка, если это уникальная черта героя

    public virtual void ExecuteEffect(CombatUnit owner, CombatUnit target = null)
    {
        // По умолчанию базовые фиты (как броня или ХП) ничего тут не делают,
        // они работают пассивно через FeatController[cite: 8].
    }
}