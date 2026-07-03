using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardItemUI : MonoBehaviour
{
    [Header("UI Ссылки")]
    public TextMeshProUGUI nameText;
    public Image artImage;
    // Если есть рамка или иконка "Лидер"
    //public GameObject leaderIndicator;

    public void Setup(CardData card, bool isLeaderCard = false)
    {
        if (nameText != null) nameText.text = card.cardName;

        if (artImage != null && card.cardArt != null)
            artImage.sprite = card.cardArt;

       // if (leaderIndicator != null)
       //     leaderIndicator.SetActive(isLeaderCard);

        // Настройка тултипа и drag-n-drop (если скрипты на этом же объекте)
        var hover = GetComponent<CardHover>();
        if (hover != null) hover.SetupTooltip(card.description);

        var drag = GetComponent<CardDrag>();
        if (drag != null) drag.myCardData = card;
    }
}