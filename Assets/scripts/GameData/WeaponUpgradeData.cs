using UnityEngine;

/// <summary>
/// Апгрейд предмета в Арсенале. Добавляет фит к оружию при покупке.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponUpgrade", menuName = "Game Data/Weapon Upgrade")]
public class WeaponUpgradeData : ScriptableObject
{
    [Header("Основная информация")]
    public string upgradeName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Цель апгрейда")]
    [Tooltip("Предмет, который улучшается. Null = применяется ко всем предметам этого слота.")]
    public ItemData targetItem;

    [Tooltip("Фит, который добавляется предмету после покупки апгрейда.")]
    public FeatData grantedFeat;

    [Header("Требования и стоимость")]
    [Tooltip("Фракция, ресурсы которой тратятся на покупку.")]
    public FactionData requiredFaction;

    [Tooltip("Стоимость в ресурсах фракции.")]
    public int resourceCost = 5;

    [Tooltip("Минимальная стадия прогресса фракции для доступности апгрейда.")]
    public FactionProgressStage requiredFactionStage = FactionProgressStage.Start;
}
