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
        foreach (Transform child in handContainer) Destroy(child.gameObject);

        foreach (CardData card in currentHand)
        {
            GameObject newCardUI = Instantiate(cardUIPrefab, handContainer);
            newCardUI.GetComponent<CardItemUI>().Setup(card);
        }
    }
}