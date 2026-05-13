using System.Collections.Generic;
using UnityEngine;
using TMPro; // Обязательно для работы с текстом TextMeshPro

public class CardHandUI : MonoBehaviour
{
    [Header("Ссылки на интерфейс")]
    public Transform handContainer; // Panel с Horizontal Layout Group, куда падают карты
    public GameObject cardUIPrefab; // Префаб самой кнопки/картинки (CardUI_Prefab)

    // Тот самый метод, который ищет HandManager
    public void UpdateUI(List<CardData> currentHand)
    {
        // 1. Сначала удаляем все старые карточки с экрана, чтобы не было дублей
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Рисуем актуальную руку
        foreach (CardData card in currentHand)
        {
            // Спавним префаб UI карточки внутри панели
            GameObject newCardUI = Instantiate(cardUIPrefab, handContainer);

            // Ищем текст внутри сгенерированной карточки
            TextMeshProUGUI nameText = newCardUI.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null) nameText.text = card.cardName;

            // НОВОЕ: Передаем описание для тултипа
            CardHover hoverScript = newCardUI.GetComponent<CardHover>();
            if (hoverScript != null)
            {
                hoverScript.SetupTooltip(card.description);
            }

            CardDrag dragScript = newCardUI.GetComponent<CardDrag>();
            if (dragScript != null)
            {
                dragScript.myCardData = card;
            }
        }
    }
}