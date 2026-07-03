using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    // НОВОЕ: Создаем глобальную ссылку на этот скрипт
    public static HandManager Instance { get; private set; }

    [Header("Настройки руки")]
    public int maxHandSize = 10; // Лимит карт в руке
    public List<CardData> playerHand = new List<CardData>(); // Сама рука

    [Header("Ссылки")]
    public GameManager GameManager; // Чтобы брать пул карт
    public CardHandUI handUI; // Ссылка на скрипт интерфейса

    // Этот метод теперь вызывается из Character_move

    // НОВОЕ: Инициализируем Синглтон при старте игры
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Защита от дубликатов
        }
    }
    public void GiveRandomCardFromPool()
    {
        // 1. Проверяем, есть ли откуда брать карты
        if (GameManager.Instance == null || GameManager.Instance.sessionCardPool == null || GameManager.Instance.sessionCardPool.Count == 0)
        {
            Debug.LogWarning("[HandManager] Пул карт пуст или GameManager не найден! Карта не выдана.");
            return;
        }

        // 2. Просто берем ГОТОВЫЙ пул из нашего синглтона!
        List<CardData> sessionPool = GameManager.Instance.sessionCardPool;

        // 3. Выбираем случайную карту
        int randomIndex = Random.Range(0, sessionPool.Count);
        CardData drawnCard = sessionPool[randomIndex];

        Debug.Log($"[HandManager] Вытянута случайная карта: {drawnCard.cardName}");
        AddCardToHand(drawnCard);
    }

    public void AddCardToHand(CardData newCard)
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