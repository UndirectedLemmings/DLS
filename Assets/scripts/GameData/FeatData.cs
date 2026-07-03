using UnityEngine;
using System.Collections.Generic;

// 1. Природа фита (Что это физически делает?)
public enum FeatNature
{
    Effect,     // Временный статус (баффы/дебаффы с таймером)
    Skill,      // Активное действие (заклинания, способности ИИ)
    Property    // Постоянная особенность (статы, пассивки, теги)
}

public enum CharacterStatType
{
    Strength,
    Endurance,
    Will,
    Wisdom,
    Agility,
    Perception
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
    PassiveStats,
    OnBattleStart,
    OnTurnStart,
    OnAttack,       // Модификатор при атаке (например, энергетический резонанс)
    OnDamageTaken,  // Реакция на урон (например, шипы или броня)
    OnAdventureStart// глобальный триггер для карты экспедиции
}


[CreateAssetMenu(fileName = "NewFeat", menuName = "Combat/Feat")]

public class FeatData : ScriptableObject
{
    public string featName;
    [TextArea(2, 4)]
    public string description;

    [Header("Ограничения")]
    [Tooltip("Если true, этот фит сработает при старте приключения только в том случае, если герой — Лидер отряда.")]
    public bool leaderOnly = false;
    // ИСПРАВЛЕНО: Добавлено поле для иконки, чтобы UI мог её отрисовать
    public Sprite icon;

    [Header("Новая Таксономия")]
    public FeatNature nature;

    // ИСПРАВЛЕНО: Переименовано из propertyCategory в category
    [Tooltip("Имеет смысл, если Nature = Property")]
    public PropertyCategory category;

    [Tooltip("Имеет смысл, если PropertyCategory = Ability")]
    public FeatDomain domain;

    [Header("Боевые триггеры")]
    public FeatType triggerType;

    [Header("Зеркальные свойства (Теги)")]
    [Tooltip("Тег этого фита, например: Poison, Regen, Ranged")]
    public string effectTag;
    [Tooltip("Список тегов, которые этот фит уничтожает при наложении")]
    public List<string> cancelsTags = new List<string>();

    [Header("Кастомные Боевые Характеристики")]
    [Tooltip("Если true, это свойство изменит базовые статы атаки и защиты в бою")]
    public bool overridesCombatStats = false;

    [Tooltip("Какая характеристика атакующего используется для броска на попадание")]
    public CharacterStatType attackStat = CharacterStatType.Agility;

    [Tooltip("Какая характеристика цели используется для броска на защиту")]
    public CharacterStatType defenseStat = CharacterStatType.Agility;
    public enum TargetPriority
    {
        Frontline,  // Бьет первого живого в ряду (Классика: Воины, Мечи, Топоры)
        LowestHP,   // Ищет самого слабого (Добивание: Застрельщики, Кинжалы)
        HighestHP,  // Ищет самого здорового (Убийцы гигантов: Арбалеты, Копья)
        Backline    // Бьет с конца ряда (Снайперы: Луки, Магия)
    }

    [Header("Боевое поведение (Для оружия/классов)")]
    public TargetPriority targetPriority = TargetPriority.Frontline;


    [Header("Модификаторы Характеристик")]
    public int bonusStrength;
    public int bonusEndurance;
    public int bonusWill;
    public int bonusWisdom;
    public int bonusAgility;
    public int bonusPerception;
    public int bonusDamage;
    public int damageReduction;
    public int bonusDiceCount;

    [Header("Лут с фита (опционально)")]
    public bool dropsLoot; // Галочка: роняет ли этот фит спец-лут?
    public LootEntry featLoot; // Настройка того, что именно падает

    // Полиморфный метод для уникальной логики фитов
    public virtual void ExecuteEffect(CombatUnit unit) { }

    public virtual void ExecuteAdventureStartEffect(UnitProgress overworldProgress) { }
}