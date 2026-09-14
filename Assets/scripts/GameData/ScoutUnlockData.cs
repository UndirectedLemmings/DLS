using UnityEngine;

/// <summary>
/// Запись о враге в Разведбюро. Покупается за ресурсы фракции и раскрывает информацию о юните.
/// </summary>
public enum ScoutUnlockType
{
    EnemyInfo,
    DwellingTier
}

[CreateAssetMenu(fileName = "NewScoutEntry", menuName = "Game Data/Scout Unlock")]
public class ScoutUnlockData : ScriptableObject
{
    [Header("Тип разблокировки")]
    public ScoutUnlockType unlockType = ScoutUnlockType.EnemyInfo;

    [Header("Юнит")]
    [Tooltip("Враг, о котором открывается информация.")]
    public UnitData targetUnit;

    [Header("Раскрываемая информация")]
    [TextArea(3, 8)]
    public string revealedInfo;

    [Header("Разблокировка жилища")]
    [Range(2, 3)] public int dwellingTierToUnlock = 2;

    [Header("Требования и стоимость")]
    [Tooltip("Фракция, ресурсы которой тратятся на покупку.")]
    public FactionData requiredFaction;

    [Tooltip("Стоимость в ресурсах фракции.")]
    public int resourceCost = 3;
}
