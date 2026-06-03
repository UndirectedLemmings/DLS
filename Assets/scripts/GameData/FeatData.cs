using UnityEngine;
using System.Collections.Generic;

// 1. Природа фита (Что это физически делает?)
public enum FeatNature
{
    Effect,     // Временный статус (баффы/дебаффы с таймером)
    Skill,      // Активное действие (заклинания, способности ИИ)
    Property    // Постоянная особенность (статы, пассивки, теги)
}

// 2. Категория свойства (Откуда берется? Используется если Nature == Property)
public enum PropertyCategory
{
    None,       // Для Effect и Skill
    Faction,    // Особенности целой фракции/расы
    Class,      // Базовый класс (Воин, Волхв, Охотник)
    Item,       // Бонус от экипировки
    Ability     // Умение (для пассивок и проверок на карте)
}

// 3. Домен умения (Где применяется? Используется если PropertyCategory == Ability)
public enum FeatDomain
{
    None,       // Для не-умений
    Historical, // Динамически полученные (например, вампиризм)
    Military,   // Боевые тренировки и владение оружием
    Survival,   // Выживание в экспедиции
    Knowledge   // Знания для интеллектуальных проверок
}

// Триггеры для CombatManager (Когда это срабатывает в бою?)
public enum FeatType
{
    PassiveStats,   // Пассивное изменение характеристик (здоровье, урон)
    BattleStart,    // Срабатывает один раз в начале боя (например, щит Лидера)
    OnAttack,       // Модификатор при атаке (например, энергетический резонанс)
    OnDamageTaken,   // Реакция на урон (например, шипы или броня)
    OnAdventureStart //глобальный триггер для карты экспедиции
}

[CreateAssetMenu(fileName = "NewFeat", menuName = "Combat/Feat")]
public class FeatData : ScriptableObject
{
    public string featName;
    [TextArea(2, 4)]
    public string description;

    [Header("Новая Таксономия")]
    public FeatNature nature;
    [Tooltip("Имеет смысл, если Nature = Property")]
    public PropertyCategory propertyCategory;
    [Tooltip("Имеет смысл, если PropertyCategory = Ability")]
    public FeatDomain domain;

    [Header("Боевые триггеры")]
    public FeatType triggerType;

    [Header("Зеркальные свойства (Теги)")]
    [Tooltip("Тег этого фита, например: Poison, Regen, Ranged")]
    public string effectTag;
    [Tooltip("Список тегов, которые этот фит уничтожает при наложении")]
    public List<string> cancelsTags = new List<string>();

    [Header("Модификаторы Характеристик")]
    public int bonusEndurance;
    public int bonusDamage;
    public int damageReduction;

    // Полиморфный метод для уникальной логики фитов
    public virtual void ExecuteEffect(CombatUnit owner, CombatUnit target = null)
    {
        Debug.Log($"<color=cyan>кака.");

        // По умолчанию базовые фиты (как броня или ХП) ничего тут не делают,
        // они работают пассивно через FeatController[cite: 8].
    }
}