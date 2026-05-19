using UnityEngine;

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


[CreateAssetMenu(fileName = "NewCard", menuName = "Game Data/Card")]
public class CardData : ScriptableObject
{
    [Header("Визуал карты")]
    public string cardName = "Новая карта";
    [TextArea(3, 5)]
    public string description = "Описание эффекта...";
    public Sprite cardArt; // Изображение карты

    [Header("Тип и Принадлежность")]
    public CardType type = CardType.Building; // Здание или Эффект
    public CardAlignment alignment = CardAlignment.Hero; // Кто может это строить

    [Tooltip("Только для фракционных карт! Перетащите сюда файл FactionData. Для Героев и Универсальных оставьте пустым.")]
    public ScriptableObject specificOwner;

    [Header("Для типа Building")]
    public GameObject buildingPrefab; // Читается, только если это постройка

    [Header("Для типа Effect")]
    public int effectPower; // Урон, хил или другой числовой модификатор
}