using UnityEngine;
using System.Collections.Generic;

// Возможные типы карт
public enum CardType
{
    Building, // Здание (ставится строго на фундамент)
    Effect    // Заклинание/Бафф (применяется сразу)
}

// НОВОЕ: Система территориальной принадлежности
public enum CardAlignment
{
    Hero,      // Строится только на фундаменте центральной дороги (Дорога А)
    Faction,   // Строится только на фундаменте своей фракции (Дорога Б)
    Universal  // Игнорирует правила территорий (особые карты)
}

public enum CardEffectType { None, GainGold, GiveBuildingCard, LootBox }

[CreateAssetMenu(fileName = "NewCard", menuName = "Game Data/Card")]
public class CardData : ScriptableObject
{
    [Header("Визуал карты")]
    public string cardName = "Новая карта";
    [TextArea(3, 5)]
    public string description = "Описание эффекта...";
    public Sprite cardArt; // Изображение карты

    [Header("Тип и Принадлежность")]
    public CardType type = CardType.Building;
    public CardAlignment alignment = CardAlignment.Hero;
    public ScriptableObject specificOwner;

    [Header("Для типа Building")]
    public GameObject buildingPrefab; // Читается, только если это постройка

    [Header("Эффекты (Обычные)")]
    public CardEffectType effectType;
    public int effectAmount; // Для жестко заданного золота (GainGold)
    public CardData buildingBlueprint; // Для жестко заданного чертежа (GiveBuildingCard)

    [Header("Настройки LootBox (Поиск)")]
    [Range(0, 100)] public int goldChance = 50; // Шанс выпадения золота (в %)
    public int minGold = 10; // Минимум золота
    public int maxGold = 30; // Максимум золота
    public List<CardData> possibleBlueprints; // Пул чертежей, из которого берем случайный
}