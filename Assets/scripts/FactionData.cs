using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Faction", menuName = "Game Data/Faction Design")]
public class FactionData : ScriptableObject
{
    public string factionName;
    public TileBase factionRoadTile;

    [Header("Модификаторы фракции")]
    public int extraFoundations;

    [Header("Карты зданий фракции")]
    public List<CardData> factionCards = new List<CardData>();
}  