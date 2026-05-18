using UnityEngine;

// Создаем список возможных типов карт
public enum CardType
{
    Building, // Здание (ставится строго на фундамент)
    Effect    // Заклинание/Бафф (применяется сразу)
}

[CreateAssetMenu(fileName = "NewCard", menuName = "Game Data/Card")]
public class CardData : ScriptableObject
{
    [Header("Визуал карты")]
    public string cardName = "Новая карта";
    [TextArea(3, 5)]
    public string description = "Описание эффекта...";
    public Sprite cardArt; // НОВОЕ: Изображение карты

    [Header("Тип и Логика")]
    public CardType type = CardType.Building; // Выбор типа карты в инспекторе

    [Header("Для типа Building")]
    public GameObject buildingPrefab; // Читается, только если это постройка

    [Header("Для типа Effect")]
    public int effectPower; // Задел на будущее (например, сколько урона нанесет метеорит или сколько хила даст)
}