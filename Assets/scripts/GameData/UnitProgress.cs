using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitProgress
{
    public UnitData Template { get; private set; }

    [Header("Идентификация")]
    public string heroName; // Имя, которое можно изменить (например, при найме)
    public FeatData classFeat; //Ссылка на карточку фита-класса (Искатель, Воин и т.д.)
    public Sprite runtimePortrait; // Спрайт, который будет отображаться в UI

    [Tooltip("Отметьте этот пункт в Инспекторе во время Play-mode, чтобы сделать героя Лидером отряда")]
    public bool isLeader;

    [Header("Прогрессия характеристик (XP)")]
    public int strengthXP;
    public int enduranceXP;
    public int willXP;
    public int wisdomXP;
    public int agilityXP;
    public int perceptionXP;

    [Header("Экипировка")]
    public ItemData equippedWeapon;
    public ItemData equippedArmor;
    public ItemData equippedAccessory;

    [Header("Таланты")]
    public List<FeatData> activeFeats;

    [HideInInspector] public FeatController overworldFeats;

    // --- ИТОГОВЫЕ ХАРАКТЕРИСТИКИ ДЛЯ МИРА И ПРОВЕРОК (База + Шмот + ФИТЫ) ---
    // Формула: База из шаблона + Бонус напрямую от предмета + Бонус от контроллера фитов

    // XP → бонус стата. Прогрессивный порог:
    // стоимость очка N = (base + feat_bonus + N) * 10
    // Т.е. с ростом стата прокачка дорожает (коэффициент 0.1 как делитель).

    public int XpBonusStrength    => ComputeXpBonus(strengthXP,  Template.baseStrength    + (overworldFeats != null ? overworldFeats.BonusStrength    : 0));
    public int XpBonusEndurance   => ComputeXpBonus(enduranceXP, Template.baseEndurance   + (overworldFeats != null ? overworldFeats.BonusEndurance   : 0));
    public int XpBonusWill        => ComputeXpBonus(willXP,      Template.baseWill        + (overworldFeats != null ? overworldFeats.BonusWill        : 0));
    public int XpBonusWisdom      => ComputeXpBonus(wisdomXP,    Template.baseWisdom      + (overworldFeats != null ? overworldFeats.BonusWisdom      : 0));
    public int XpBonusAgility     => ComputeXpBonus(agilityXP,   Template.baseAgility     + (overworldFeats != null ? overworldFeats.BonusAgility     : 0));
    public int XpBonusPerception  => ComputeXpBonus(perceptionXP,Template.basePerception  + (overworldFeats != null ? overworldFeats.BonusPerception  : 0));

    /// <summary>
    /// Считает, сколько бонусных очков дало накопленное XP при прогрессивном пороге.
    /// Каждое следующее очко стоит (basePlusFeatValue + bonus) * 10 XP.
    /// </summary>
    public static int ComputeXpBonus(int xp, int basePlusFeatValue)
    {
        int bonus = 0;
        int remaining = xp;
        while (true)
        {
            int cost = (basePlusFeatValue + bonus) * 10;
            if (remaining < cost) break;
            remaining -= cost;
            bonus++;
        }
        return bonus;
    }

    /// <summary>
    /// XP, потраченное на уже полученные бонусы (для отображения прогресса в UI).
    /// </summary>
    public static int XpSpentForBonus(int bonus, int basePlusFeatValue)
    {
        int total = 0;
        for (int i = 0; i < bonus; i++)
            total += (basePlusFeatValue + i) * 10;
        return total;
    }

    /// <summary>Стоимость следующего бонусного очка в XP.</summary>
    public static int XpCostForNextPoint(int bonus, int basePlusFeatValue)
        => (basePlusFeatValue + bonus) * 10;

    public int TotalStrength    => Template.baseStrength    + XpBonusStrength    + (overworldFeats != null ? overworldFeats.BonusStrength    : 0);
    public int TotalEndurance   => Template.baseEndurance   + XpBonusEndurance   + (overworldFeats != null ? overworldFeats.BonusEndurance   : 0);
    public int TotalWill        => Template.baseWill        + XpBonusWill        + (overworldFeats != null ? overworldFeats.BonusWill        : 0);
    public int TotalWisdom      => Template.baseWisdom      + XpBonusWisdom      + (overworldFeats != null ? overworldFeats.BonusWisdom      : 0);
    public int TotalAgility     => Template.baseAgility     + XpBonusAgility     + (overworldFeats != null ? overworldFeats.BonusAgility     : 0);
    public int TotalPerception  => Template.basePerception  + XpBonusPerception  + (overworldFeats != null ? overworldFeats.BonusPerception  : 0);
    // Динамические лимиты ресурсов персонажа в мире
    public int MaxEP => TotalEndurance;
    public int MaxMana => TotalWill; // К примеру, мана зависит от Воли

    [Header("ТЕКУЩЕЕ СОСТОЯНИЕ")]
    public int currentHealthyEP;
    public int currentTiredEP;
    public int currentWoundedEP;
    public int currentMana;

    public UnitProgress(UnitData template)
    {
        Template = template;
        activeFeats = new List<FeatData>(template.startingFeats);
        equippedWeapon = template.startingWeapon;
        equippedArmor = template.startingArmor;
        equippedAccessory = template.startingAccessory;

        // При создании персонаж полностью здоров и полон маны
        currentHealthyEP = MaxEP;
        currentTiredEP = 0;
        currentWoundedEP = 0;
        currentMana = MaxMana;
    }

    // Универсальный внутренний метод, чтобы не писать кучу дублирующегося кода для каждой вещи
    // (Предполагается, что в ItemData у тебя есть поля бонусов, например public int strengthBonus;)




    // ==========================================
    // ОПЫТ (XP)
    // ==========================================

    /// <summary>
    /// Начисляет XP к указанной характеристике за успешное использование.
    /// Возвращает true, если произошёл прирост стата (level-up).
    /// </summary>
    public bool AddXP(CharacterStatType stat, int amount)
    {
        if (amount <= 0) return false;
        int before;
        switch (stat)
        {
            case CharacterStatType.Strength:
                before = XpBonusStrength;   strengthXP   += amount; return XpBonusStrength   > before;
            case CharacterStatType.Endurance:
                before = XpBonusEndurance;  enduranceXP  += amount; return XpBonusEndurance  > before;
            case CharacterStatType.Will:
                before = XpBonusWill;       willXP       += amount; return XpBonusWill       > before;
            case CharacterStatType.Wisdom:
                before = XpBonusWisdom;     wisdomXP     += amount; return XpBonusWisdom     > before;
            case CharacterStatType.Agility:
                before = XpBonusAgility;    agilityXP    += amount; return XpBonusAgility    > before;
            case CharacterStatType.Perception:
                before = XpBonusPerception; perceptionXP += amount; return XpBonusPerception > before;
            default: return false;
        }
    }

    /// <summary>
    /// Возвращает текущий XP для указанной характеристики.
    /// </summary>
    public int GetXP(CharacterStatType stat)
    {
        switch (stat)
        {
            case CharacterStatType.Strength:    return strengthXP;
            case CharacterStatType.Endurance:   return enduranceXP;
            case CharacterStatType.Will:        return willXP;
            case CharacterStatType.Wisdom:      return wisdomXP;
            case CharacterStatType.Agility:     return agilityXP;
            case CharacterStatType.Perception:  return perceptionXP;
            default: return 0;
        }
    }




    /// <summary>
    /// Собирает все фиты героя: врожденные, классовые и от экипировки.
    /// Это нужно вызывать при старте боя или проверке статов.
    /// </summary>
    public List<FeatData> GetAllActiveFeats()
    {
        List<FeatData> allFeats = new List<FeatData>();

        // 1. Базовые фиты героя
        if (activeFeats != null)
            allFeats.AddRange(activeFeats);

        // 2. ДОБАВЛЕНО: Классовый фит (Бродяга и т.д.)
        if (classFeat != null && !allFeats.Contains(classFeat))
            allFeats.Add(classFeat);

        // 3. Фиты от экипировки
        if (equippedWeapon != null && equippedWeapon.grantedFeats != null)
            allFeats.AddRange(equippedWeapon.grantedFeats);

        if (equippedArmor != null && equippedArmor.grantedFeats != null)
            allFeats.AddRange(equippedArmor.grantedFeats);

        if (equippedAccessory != null && equippedAccessory.grantedFeats != null)
            allFeats.AddRange(equippedAccessory.grantedFeats);

        // 4. Финальная зачистка: удаляем все возможные null, 
        // которые могли случайно просочиться из пустых слотов в Unity
        allFeats.RemoveAll(feat => feat == null);

        return allFeats;
    }

    /// <summary>
    /// Возвращает тайлы территории для генератора карты. 
    /// Если у персонажа есть фит-класса и в нем настроены кастомные тайлы — берет их. 
    /// Если нет — откатывается на дефолтные тайлы из HeroData.
    /// </summary>
    public UnityEngine.Tilemaps.TileBase[] GetCurrentTerritoryTiles()
    {
        // 1. Проверяем настройки класса (приоритет)
        if (classFeat != null && classFeat is ClassFeatData classConfig)
        {
            if (classConfig.classTerritoryVoidTiles != null && classConfig.classTerritoryVoidTiles.Length > 0)
            {
                return classConfig.classTerritoryVoidTiles;
            }
        }

        // 2. Если это обычный моб или нет тайлов — возвращаем пустоту
        return null;
    }
    /// <summary>
    /// Возвращает тайл центральной дороги героя на основе его класса (Бродяга, Воин и т.д.).
    /// Если у класса нет своей дороги, пытается взять базовую из шаблона.
    /// </summary>
    public UnityEngine.Tilemaps.TileBase GetCurrentRoadTile()
    {
        // 1. Ищем кастомную дорогу в классе лидера (наивысший приоритет)
        if (classFeat != null && classFeat is ClassFeatData classConfig)
        {
            // Проверяем, что массив не пустой и в нем лежит хотя бы один RuleTile
            if (classConfig.classTerritoryRoadTiles != null && classConfig.classTerritoryRoadTiles.Length > 0)
            {
                return classConfig.classTerritoryRoadTiles[0];
            }
        }

        // Если дошли сюда — дороги нет вообще
        return null;
    }

    /// <summary>
    /// Вычисляет бонусы кубиков (d10) от экипировки по каждой характеристике.
    /// Возвращает словарь: {CharacterStatType -> количество кубиков}
    /// </summary>
    public Dictionary<CharacterStatType, int> GetEquipmentDiceBonuses()
    {
        var bonuses = new Dictionary<CharacterStatType, int>
        {
            { CharacterStatType.Strength, 0 },
            { CharacterStatType.Endurance, 0 },
            { CharacterStatType.Will, 0 },
            { CharacterStatType.Wisdom, 0 },
            { CharacterStatType.Agility, 0 },
            { CharacterStatType.Perception, 0 }
        };

        // Собираем фиты из всей экипировки
        List<FeatData> equipmentFeats = new List<FeatData>();

        if (equippedWeapon != null && equippedWeapon.grantedFeats != null)
            equipmentFeats.AddRange(equippedWeapon.grantedFeats);

        if (equippedArmor != null && equippedArmor.grantedFeats != null)
            equipmentFeats.AddRange(equippedArmor.grantedFeats);

        if (equippedAccessory != null && equippedAccessory.grantedFeats != null)
            equipmentFeats.AddRange(equippedAccessory.grantedFeats);

        // Суммируем бонусы кубиков по характеристикам
        foreach (var feat in equipmentFeats)
        {
            if (feat == null) continue;

            if (feat.bonusStrength > 0) bonuses[CharacterStatType.Strength] += feat.bonusStrength;
            if (feat.bonusEndurance > 0) bonuses[CharacterStatType.Endurance] += feat.bonusEndurance;
            if (feat.bonusWill > 0) bonuses[CharacterStatType.Will] += feat.bonusWill;
            if (feat.bonusWisdom > 0) bonuses[CharacterStatType.Wisdom] += feat.bonusWisdom;
            if (feat.bonusAgility > 0) bonuses[CharacterStatType.Agility] += feat.bonusAgility;
            if (feat.bonusPerception > 0) bonuses[CharacterStatType.Perception] += feat.bonusPerception;
        }

        return bonuses;
    }

    /// <summary>
    /// Форматирует бонусы в строку "(+2d10 Agility, +1d10 Strength)"
    /// Показывает только те характеристики, у которых есть бонусы
    /// </summary>
    public string GetFormattedEquipmentBonuses()
    {
        var bonuses = GetEquipmentDiceBonuses();
        List<string> bonusStrings = new List<string>();

        foreach (var kvp in bonuses)
        {
            if (kvp.Value > 0)
            {
                bonusStrings.Add($"+{kvp.Value}d10 {kvp.Key}");
            }
        }

        if (bonusStrings.Count == 0)
            return "Нет бонусов";

        return string.Join(", ", bonusStrings);
    }

    /// <summary>
    /// Получить бонус d10 от экипировки для конкретной характеристики
    /// (ТОЛЬКО от предметов, не от класса!)
    /// </summary>
    public int GetEquipmentDiceBonusByStatType(CharacterStatType statType)
    {
        int bonus = 0;

        List<FeatData> equipmentFeats = new List<FeatData>();

        if (equippedWeapon != null && equippedWeapon.grantedFeats != null)
            equipmentFeats.AddRange(equippedWeapon.grantedFeats);

        if (equippedArmor != null && equippedArmor.grantedFeats != null)
            equipmentFeats.AddRange(equippedArmor.grantedFeats);

        if (equippedAccessory != null && equippedAccessory.grantedFeats != null)
            equipmentFeats.AddRange(equippedAccessory.grantedFeats);

        foreach (var feat in equipmentFeats)
        {
            if (feat == null) continue;

            switch (statType)
            {
                case CharacterStatType.Strength:
                    if (feat.bonusStrength > 0) bonus += feat.bonusStrength;
                    break;
                case CharacterStatType.Endurance:
                    if (feat.bonusEndurance > 0) bonus += feat.bonusEndurance;
                    break;
                case CharacterStatType.Will:
                    if (feat.bonusWill > 0) bonus += feat.bonusWill;
                    break;
                case CharacterStatType.Wisdom:
                    if (feat.bonusWisdom > 0) bonus += feat.bonusWisdom;
                    break;
                case CharacterStatType.Agility:
                    if (feat.bonusAgility > 0) bonus += feat.bonusAgility;
                    break;
                case CharacterStatType.Perception:
                    if (feat.bonusPerception > 0) bonus += feat.bonusPerception;
                    break;
            }
        }

        return bonus;
    }

    /// <summary>
    /// Получить бонус от класса (ClassFeatData) для конкретной характеристики
    /// </summary>
    public int GetClassBonusByStatType(CharacterStatType statType)
    {
        if (classFeat == null || overworldFeats == null)
            return 0;

        switch (statType)
        {
            case CharacterStatType.Strength: return overworldFeats.BonusStrength;
            case CharacterStatType.Endurance: return overworldFeats.BonusEndurance;
            case CharacterStatType.Will: return overworldFeats.BonusWill;
            case CharacterStatType.Wisdom: return overworldFeats.BonusWisdom;
            case CharacterStatType.Agility: return overworldFeats.BonusAgility;
            case CharacterStatType.Perception: return overworldFeats.BonusPerception;
            default: return 0;
        }
    }

}