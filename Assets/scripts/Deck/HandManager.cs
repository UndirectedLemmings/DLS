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
        List<CardData> sessionPool = new List<CardData>();

        // 1. Добавляем карты Лидера отряда (Главные и Вспомогательные)
        if (mapGenerator.activeLeader != null)
        {
            if (mapGenerator.activeLeader.heroMainCards != null)
                sessionPool.AddRange(mapGenerator.activeLeader.heroMainCards);

            if (mapGenerator.activeLeader.heroSupportCards != null)
                sessionPool.AddRange(mapGenerator.activeLeader.heroSupportCards);
        }

        // 2. Добавляем Вспомогательные карты остальных членов отряда
        if (mapGenerator.activeSquad != null && mapGenerator.activeSquad.Count > 0)
        {
            foreach (HeroData companion in mapGenerator.activeSquad)
            {
                if (companion != null && companion.heroSupportCards != null)
                {
                    sessionPool.AddRange(companion.heroSupportCards);
                }
            }
        }

        // 3. Добавляем карты всех активных вражеских Фракций
        if (mapGenerator.activeFactions != null && mapGenerator.activeFactions.Count > 0)
        {
            foreach (FactionData faction in mapGenerator.activeFactions)
            {
                if (faction != null && faction.factionCards != null)
                {
                    sessionPool.AddRange(faction.factionCards);
                }
            }
        }

        // 4. Проверяем, удалось ли хоть что-то собрать
        if (sessionPool.Count == 0)
        {
            Debug.LogWarning("DLS: Пул карт пуст! У Лидера, Отряда и Фракций нет карт для выдачи.");
            return;
        }

        // 5. Выдаем случайную карту из собранного пула
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