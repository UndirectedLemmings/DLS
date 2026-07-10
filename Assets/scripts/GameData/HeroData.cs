using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Hero", menuName = "Game Data/Hero Design")]
public class HeroData : UnitData
{
    [Header("Колоды карт")]
    public List<CardData> heroMainCards;    // ГЛАВНЫЕ КАРТЫ (активны, только если он Лидер)
    public List<CardData> heroSupportCards; // НЕГЛАВНЫЕ КАРТЫ (добавляются, если он просто в отряде)

    [Header("Модификаторы лидера")]
    public int bonusFoundations; // Используется, только если он Лидер
}