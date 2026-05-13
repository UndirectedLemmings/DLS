using UnityEngine;
using UnityEngine.EventSystems; // Обязательно для событий UI

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string tooltipMessage;

    // Метод для записи текста (его будет вызывать CardHandUI)
    public void SetupTooltip(string text)
    {
        tooltipMessage = text;
    }

    // Срабатывает, когда мышка входит в границы карточки
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null && !string.IsNullOrEmpty(tooltipMessage))
        {
            TooltipManager.Instance.ShowTooltip(tooltipMessage);
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

    // Защита от зависания тултипа, если карточку уничтожили (например, вытеснили лимитом)
    private void OnDisable()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}