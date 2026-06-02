using UnityEngine;
using System.Collections.Generic;

public enum ItemSlotType
{
    Weapon,
    Armor,
    Accessory,
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

    [Header("Особенности от предмета")]
    // Фиты, которые предмет передает герою, пока надет
    public List<FeatData> grantedFeats = new List<FeatData>();
}