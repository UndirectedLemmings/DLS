using UnityEngine;

[CreateAssetMenu(fileName = "NewGlobalSettings", menuName = "Game Data/Global Settings")]
public class GlobalSettingsData : ScriptableObject
{
    [Header("Настройки генерации карты")]
    [Tooltip("Ширина и высота логической сетки карты")]
    public Vector2Int mapSize = new Vector2Int(40, 40);

    [Tooltip("Количество стартовых локаций/комнат (если используем)")]
    public int baseLocationsCount = 5;

    [Header("Настройки перемещения")]
    [Tooltip("Скорость передвижения фишки героя по карте")]
    [Range(1f, 20f)]
    public float heroMoveSpeed = 5f;

    [Header("Настройки событий (Шансы)")]
    [Tooltip("Базовый шанс нарваться на бой при перемещении (от 0 до 1)")]
    [Range(0f, 1f)]
    public float baseCombatEncounterChance = 0.2f;

    [Tooltip("Базовый шанс найти сундук/лут (от 0 до 1)")]
    [Range(0f, 1f)]
    public float baseLootChance = 0.15f;
}