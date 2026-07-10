using UnityEngine;
using UnityEngine.EventSystems;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public CardData myCardData;

    private void Start()
    {
        // Автоматически берем данные карты из скрипта CardDrag, 
        // который висит на этом же объекте карточки.
        CardDrag dragScript = GetComponent<CardDrag>();
        if (dragScript != null)
        {
            myCardData = dragScript.myCardData;
        }
    }

    // Срабатывает, когда мышка входит в границы карточки
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Проверяем, что менеджер существует и данные карты загружены
        if (TooltipManager.Instance != null && myCardData != null)
        {
            TooltipManager.Instance.ShowTooltip(myCardData.cardName, myCardData.description);
        }
    }

    // Срабатывает, когда мышка уходит с карточки
    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    // Защита от зависания тултипа, если карточку уничтожили (например, разыграли или сбросили)
    private void OnDisable()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}