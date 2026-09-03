using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum FactionProgressStage
{
    Start = 0,
    AfterFirstBoss = 1,
    AfterSecondBoss = 2,
    Completed = 3
}

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

    [Header("⚙️ Боевой контент фракции")]
    [Tooltip("Обычный пул мобов на старте фракции")]
    public List<UnitData> baseFactionMobs = new List<UnitData>();

    [Tooltip("Обычный пул мобов после победы над первым боссом")]
    public List<UnitData> mobsAfterFirstBoss = new List<UnitData>();

    [Tooltip("Обычный пул мобов после победы над вторым боссом")]
    public List<UnitData> mobsAfterSecondBoss = new List<UnitData>();

    [Header("⚙️ Боссы прогрессии")]
    public UnitData firstProgressBoss;
    public UnitData secondProgressBoss;
    public UnitData finalBoss;

    [Tooltip("Главный фракционный фит контроля, который автоматически вкалывается всем мобам этой фракции при спавне и управляет их мутациями (Escalation)")]
    public FactionEscalationFeatData factionEscalationFeat;

    public IReadOnlyList<UnitData> GetAvailableMobs(FactionProgressStage stage)
    {
        if (stage >= FactionProgressStage.AfterSecondBoss && mobsAfterSecondBoss != null && mobsAfterSecondBoss.Count > 0)
            return mobsAfterSecondBoss;

        if (stage >= FactionProgressStage.AfterFirstBoss && mobsAfterFirstBoss != null && mobsAfterFirstBoss.Count > 0)
            return mobsAfterFirstBoss;

        return baseFactionMobs;
    }
}
