using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Faction", menuName = "Game Data/Faction Design")]
public class FactionData : ScriptableObject
{
    [Header("Базовые настройки")]
    public string factionName;
    public TileBase factionRoadTile;

    [Header("Модификаторы фракции")]
    public int extraFoundations;

    [Header("Карты зданий фракции")]
    public List<CardData> factionCards = new List<CardData>();

    [Header("Территория (Фон вокруг дороги)")]
    public TileBase[] territoryVoidTiles;

    // --- НОВЫЙ БЛОК: ДАННЫЕ ДЛЯ СПАВНА И ЭСКАЛАЦИИ ---

    [Header("⚙️ Боевой контент фракции")]
    [Tooltip("Список базовых мобов фракции (например, Болванчик, Лукванчик), которые спавнятся на начальных кругах")]
    public List<UnitData> baseFactionMobs = new List<UnitData>();

    [Tooltip("Главный фракционный фит контроля, который автоматически вкалывается всем мобам этой фракции при спавне и управляет их мутациями (Escalation)")]
    public FactionEscalationFeatData factionEscalationFeat;
}