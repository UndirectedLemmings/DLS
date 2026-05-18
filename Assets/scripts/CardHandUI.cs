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
            GameObject newCardUI = Instantiate(cardUIPrefab, handContainer);

            // Настройка Имени
            TextMeshProUGUI nameText = newCardUI.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null) nameText.text = card.cardName;

            // НОВОЕ: Настройка Картинки
            Transform artObj = newCardUI.transform.Find("CardArt"); // Ищем тот самый дочерний объект
            if (artObj != null && card.cardArt != null)
            {
                UnityEngine.UI.Image artImage = artObj.GetComponent<UnityEngine.UI.Image>();
                if (artImage != null) artImage.sprite = card.cardArt;
            }

            // Настройка Тултипа
            CardHover hoverScript = newCardUI.GetComponent<CardHover>();
            if (hoverScript != null) hoverScript.SetupTooltip(card.description);

            // Настройка Drag & Drop
            CardDrag dragScript = newCardUI.GetComponent<CardDrag>();
            if (dragScript != null) dragScript.myCardData = card;
        }
    }
}