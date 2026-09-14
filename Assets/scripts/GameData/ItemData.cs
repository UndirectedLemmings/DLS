using UnityEngine;
using System.Collections.Generic;

public enum ItemSlotType
{
    Weapon,
    Armor,
    Accessory,
}

public enum WeaponClass
{
    None,
    Bow,
    Crossbow,
    Sword,
    Spear,
    Dagger,
    Axe,
    Mace
}

public enum ArmorClass
{
    None,
    Light,
    Medium,
    Heavy
}

public enum AccessoryClass
{
    None,
    Shield,
    Fetish,
    Dagger
}

public enum EquipmentOrigin
{
    Neutral,
    HeroFaction,
    EnemyFaction
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Combat/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Базовая информация")]
    public string itemName;
    [TextArea] public string description;
    public Sprite itemIcon;

    [Header("Тип слота")]
    public ItemSlotType slotType; // Куда именно надевается предмет

    [Header("Класс экипировки")]
    public WeaponClass weaponClass = WeaponClass.None;
    public ArmorClass armorClass = ArmorClass.None;
    public AccessoryClass accessoryClass = AccessoryClass.None;

    [Header("Происхождение и распад")]
    public EquipmentOrigin origin = EquipmentOrigin.Neutral;
    public FactionData sourceFaction;
    [Min(0)] public int dismantleResourceValue = 1;

    [Header("Особенности от предмета")]
    // Фиты, которые предмет передает герою, пока надет (например, "Кровотечение" у топора)
    public List<FeatData> grantedFeats = new List<FeatData>();
}