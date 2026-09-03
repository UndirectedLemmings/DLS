using UnityEngine;

/// <summary>
/// Запись о враге в Разведбюро. Покупается за ресурсы фракции и раскрывает информацию о юните.
/// </summary>
[CreateAssetMenu(fileName = "NewScoutEntry", menuName = "Game Data/Scout Unlock")]
public class ScoutUnlockData : ScriptableObject
{
    [Header("Юнит")]
    [Tooltip("Враг, о котором открывается информация.")]
    public UnitData targetUnit;

    [Header("Раскрываемая информация")]
    [TextArea(3, 8)]
    public string revealedInfo;

    [Header("Требования и стоимость")]
    [Tooltip("Фракция, ресурсы которой тратятся на покупку.")]
    public FactionData requiredFaction;

    [Tooltip("Стоимость в ресурсах фракции.")]
    public int resourceCost = 3;
}
