using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardItemUI : MonoBehaviour
{
    [Header("UI Ссылки")]
    public TextMeshProUGUI nameText;
    public Image artImage;
    // Если есть рамка или иконка "Лидер"
    // public GameObject leaderIndicator;

    // Кэшируем ссылки на компоненты для максимальной производительности
    private CardHover cardHover;
    private CardDrag cardDrag;

    private void Awake()
    {
        // Оптимизация: ищем компоненты всего один раз при рождении объекта (спавне префаба).
        // TryGetComponent работает быстрее обычного GetComponent.
        TryGetComponent(out cardHover);
        TryGetComponent(out cardDrag);
    }

    public void Setup(CardData card, bool isLeaderCard = false)
    {
        // Защита от дурака: если передали пустую карту, просто прерываем метод, чтобы избежать NullReferenceException
        if (card == null) return;

        if (nameText != null)
            nameText.text = card.cardName;

        if (artImage != null && card.cardArt != null)
            artImage.sprite = card.cardArt;

        // if (leaderIndicator != null)
        //     leaderIndicator.SetActive(isLeaderCard);

        // ИСПРАВЛЕНИЕ: Передаем два аргумента (имя и описание), как мы прописали в CardHover
        if (cardHover != null)
            cardHover.SetupTooltip(card.cardName, card.description);

        if (cardDrag != null)
            cardDrag.myCardData = card;
    }
}