using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Hero", menuName = "Game Data/Hero Design")]
public class HeroData : ScriptableObject
{
    public string heroName;
    public TileBase heroRoadTile;

    [Header("Модификаторы лидера")]
    public int bonusFoundations;

    [Header("Карты зданий героя")]
    public List<CardData> heroCards = new List<CardData>();
}