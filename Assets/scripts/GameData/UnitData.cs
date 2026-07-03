using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LootEntry
{
    public ItemData item;
    [Range(0, 100)]
    public int dropChance; // Шанс выпадения (0 - 100%)
    public int minAmount;  // Минимальное количество 
    public int maxAmount;  // Максимальное количество
}

public enum UnitSide { Hero, Enemy, Neutral }

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Game Data/Unit")]
public class UnitData : ScriptableObject
{
    [Header("Базовая информация")]
    public string unitName;
    public Sprite portrait;
    public UnitSide defaultSide = UnitSide.Enemy;

    [Header("Базовые характеристики (Стартовые)")]
    [Range(1, 10)] public int baseStrength = 1;
    [Range(1, 10)] public int baseEndurance = 1;
    [Range(1, 10)] public int baseWill = 1;
    [Range(1, 10)] public int baseWisdom = 1;
    [Range(1, 10)] public int baseAgility = 1;
    [Range(1, 10)] public int basePerception = 1;

    [Header("Стартовая экипировка (По умолчанию)")]
    public ItemData startingWeapon;
    public ItemData startingArmor;
    public ItemData startingAccessory;

    [Header("Стартовые таланты")]
    public List<FeatData> startingFeats = new List<FeatData>();

    // 2. ДОБАВЛЯЕМ СПИСОК ЛУТА
    [Header("Базовый лут монстра")]
    public List<LootEntry> baseLoot;
}