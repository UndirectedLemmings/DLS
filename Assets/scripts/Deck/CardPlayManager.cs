using UnityEngine;
using System.Collections.Generic; // Для работы со списками
public class CardPlayManager : MonoBehaviour
{
    // Глобальная ссылка (Синглтон), чтобы другие скрипты могли легко к нему обращаться
    public static CardPlayManager Instance { get; private set; }

    private void Awake()
    {
        // Настраиваем синглтон при старте
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Уничтожаем дубликаты, если они случайно появятся
        }
    }

    // Основной метод активации эффектов
    public void ApplyCardEffect(CardData card)
    {
        switch (card.effectType)
        {
            case CardEffectType.GainGold:
                GameManager.Instance.Gold += card.effectAmount;
                Debug.Log($"[Эффект] Получено {card.effectAmount} золота.");
                break;

            case CardEffectType.GiveBuildingCard:
                if (card.buildingBlueprint != null)
                {
                    // ВАЖНО: вызываем добавление в HandManager
                    HandManager.Instance.AddCardToHand(card.buildingBlueprint);
                    Debug.Log($"[Эффект] Карта {card.cardName} выдала {card.buildingBlueprint.cardName}");
                }
                else
                {
                    Debug.LogWarning($"[Эффект] Ошибка: Карта {card.cardName} имеет тип GiveBuildingCard, но слот buildingBlueprint пуст!");
                }
                break;

            case CardEffectType.LootBox:
                ProcessLootBox(card);
                break;
        }
    }

    private void ProcessLootBox(CardData card)
    {
        int roll = Random.Range(0, 101); // Бросаем кубик от 0 до 100

        // Проверяем, выпало ли золото
        if (roll <= card.goldChance)
        {
            int goldFound = Random.Range(card.minGold, card.maxGold + 1);
            GameManager.Instance.Gold += goldFound;
            Debug.Log($"[Поиск] Вы нашли тайник! Получено {goldFound} золота. (Выпало на кубике: {roll})");
        }
        // Если золото не выпало, выдаем случайный чертеж
        else
        {
            if (card.possibleBlueprints != null && card.possibleBlueprints.Count > 0)
            {
                // Выбираем случайную карту из пула
                int randomIndex = Random.Range(0, card.possibleBlueprints.Count);
                CardData foundBlueprint = card.possibleBlueprints[randomIndex];

                HandManager.Instance.AddCardToHand(foundBlueprint);
                Debug.Log($"[Поиск] Вы нашли древние знания! Получен чертеж: {foundBlueprint.cardName}. (Выпало на кубике: {roll})");
            }
            else
            {
                // Защита от ошибки, если ты забыл добавить чертежи в список
                Debug.LogWarning($"[Поиск] Карта '{card.cardName}' хотела выдать чертеж, но список 'possibleBlueprints' пуст! Выдаем утешительное золото.");
                GameManager.Instance.Gold += card.minGold;
            }
        }
    }
}