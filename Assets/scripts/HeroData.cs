using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Hero", menuName = "Game Data/Hero Design")]
public class HeroData : ScriptableObject
{
    public string heroName;
    public TileBase heroRoadTile; // Используется, только если он Лидер

    [Header("Колоды карт")]
    public List<CardData> heroMainCards;    // ГЛАВНЫЕ КАРТЫ (активны, только если он Лидер)
    public List<CardData> heroSupportCards; // НЕГЛАВНЫЕ КАРТЫ (добавляются, если он просто в отряде)

    [Header("Модификаторы лидера")]
    public int bonusFoundations; // Используется, только если он Лидер

    [Header("Территория (Фон вокруг дороги)")]
    public UnityEngine.Tilemaps.TileBase[] territoryVoidTiles;
}