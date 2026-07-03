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

    public int TotalStrength => Template.baseStrength + (overworldFeats != null ? overworldFeats.BonusStrength : 0);
    public int TotalEndurance => Template.baseEndurance + (overworldFeats != null ? overworldFeats.BonusEndurance : 0);
    public int TotalWill => Template.baseWill + (overworldFeats != null ? overworldFeats.BonusWill : 0);
    public int TotalWisdom => Template.baseWisdom + (overworldFeats != null ? overworldFeats.BonusWisdom : 0);
    public int TotalAgility => Template.baseAgility + (overworldFeats != null ? overworldFeats.BonusAgility : 0);
    public int TotalPerception => Template.basePerception + (overworldFeats != null ? overworldFeats.BonusPerception : 0);
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


    // Метод для смены экипировки прямо на глобальной карте (в меню или у сокровищницы)
    public void EquipItem(ItemData newItem, ItemSlotType slot)
    {
        ItemData oldItem = null;

        // 1. Снимаем старую вещь
        switch (slot)
        {
            case ItemSlotType.Weapon:
                oldItem = equippedWeapon;
                equippedWeapon = newItem;
                break;
            case ItemSlotType.Armor:
                oldItem = equippedArmor;
                equippedArmor = newItem;
                break;
            case ItemSlotType.Accessory:
                oldItem = equippedAccessory;
                equippedAccessory = newItem;
                break;
        }

        // 2. Отключаем фиты старой вещи (если она была)
        if (oldItem != null && oldItem.grantedFeats != null && overworldFeats != null)
        {
            foreach (var feat in oldItem.grantedFeats)
            {
                overworldFeats.RemoveEquipmentFeat(feat);
            }
        }

        // 3. Включаем фиты новой вещи
        if (newItem != null && newItem.grantedFeats != null && overworldFeats != null)
        {
            foreach (var feat in newItem.grantedFeats)
            {
                overworldFeats.AddEquipmentFeat(feat);
            }
        }

        // 4. Корректируем текущее здоровье, чтобы оно не превысило новый (возможно уменьшившийся) MaxEP
        currentHealthyEP = Mathf.Clamp(currentHealthyEP, 0, MaxEP - currentTiredEP - currentWoundedEP);
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

        // 2. ИСПРАВЛЕНО: Безопасно проверяем, является ли этот юнит Героем
        if (Template is HeroData heroTemplate)
        {
            return heroTemplate.territoryVoidTiles;
        }

        // 3. Если это обычный моб или нет тайлов — возвращаем пустоту
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

        // 2. Страховочный откат (если ты еще не удалил поле heroRoadTile из кода HeroData)
        if (Template is HeroData heroTemplate && heroTemplate.heroRoadTile != null)
        {
            return heroTemplate.heroRoadTile;
        }

        // Если дошли сюда — дороги нет вообще
        return null;
    }

    // Добавь этот метод внутрь UnitProgress.cs

    public void RegisterEquipmentFeats(FeatController controller)
    {
        if (controller == null) return;

        if (equippedWeapon != null && equippedWeapon.grantedFeats != null)
            foreach (var feat in equippedWeapon.grantedFeats) controller.AddEquipmentFeat(feat);

        if (equippedArmor != null && equippedArmor.grantedFeats != null)
            foreach (var feat in equippedArmor.grantedFeats) controller.AddEquipmentFeat(feat);

        if (equippedAccessory != null && equippedAccessory.grantedFeats != null)
            foreach (var feat in equippedAccessory.grantedFeats) controller.AddEquipmentFeat(feat);
    }
}