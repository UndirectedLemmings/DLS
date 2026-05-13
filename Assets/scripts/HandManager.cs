using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Настройки руки")]
    public int maxHandSize = 10; // Лимит карт в руке
    public List<CardData> playerHand = new List<CardData>(); // Сама рука

    [Header("Ссылки")]
    public FILL_MAP_v4 mapGenerator; // Чтобы брать пул карт
    public CardHandUI handUI; // Ссылка на скрипт интерфейса

    // Этот метод теперь вызывается из Character_move
    public void GiveRandomCardFromPool()
    {
        // 1. Собираем карты из выбранного героя и фракции
        List<CardData> sessionPool = new List<CardData>();

        if (mapGenerator.selectedHero != null && mapGenerator.selectedHero.heroCards != null)
            sessionPool.AddRange(mapGenerator.selectedHero.heroCards);

        if (mapGenerator.selectedFaction != null && mapGenerator.selectedFaction.factionCards != null)
            sessionPool.AddRange(mapGenerator.selectedFaction.factionCards);

        // 2. Проверяем, есть ли что выдавать
        if (sessionPool.Count == 0)
        {
            Debug.LogWarning("DLS: Пул карт пуст! У Героя и Фракции нет карт для выдачи.");
            return;
        }

        // 3. Выбираем случайную
        CardData randomCard = sessionPool[Random.Range(0, sessionPool.Count)];
        AddCardToHand(randomCard);
    }

    private void AddCardToHand(CardData newCard)
    {
        // Вытеснение старых карт, если превышен лимит
        if (playerHand.Count >= maxHandSize)
        {
            playerHand.RemoveAt(0);
        }

        playerHand.Add(newCard);
        Debug.Log($"DLS: Выдана карта {newCard.name}. Всего карт в руке: {playerHand.Count}");

        // Обновляем UI, только если он назначен (защита от краша)
        if (handUI != null)
        {
            handUI.UpdateUI(playerHand);
        }
        else
        {
            Debug.LogWarning("DLS: CardHandUI не назначен в HandManager, интерфейс не обновится.");
        }
    }

    // Метод для BuildManager, чтобы забирать карту для строительства
    public CardData GetCard(int index)
    {
        if (index >= 0 && index < playerHand.Count)
            return playerHand[index];
        return null;
    }

    public void RemoveCard(CardData card)
    {
        if (playerHand.Contains(card))
        {
            playerHand.Remove(card);
            if (handUI != null) handUI.UpdateUI(playerHand);
        }
    }
}