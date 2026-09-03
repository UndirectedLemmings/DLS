using UnityEngine;
using UnityEngine.EventSystems;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public CardData myCardData;

    // Инициализировать пустыми строками не обязательно, 
    // string.IsNullOrEmpty отлично переварит и null по умолчанию.
    private string tooltipTitle;
    private string tooltipDescription;

    private void Start()
    {
        // Оптимизация: TryGetComponent работает быстрее обычного GetComponent 
        // и не создает "мусора" (garbage) в памяти, если компонент не найден.
        if (TryGetComponent<CardDrag>(out var dragScript))
        {
            myCardData = dragScript.myCardData;
        }
    }

    public void SetupTooltip(string title, string description)
    {
        tooltipTitle = title;
        tooltipDescription = description;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Мышь наведена на: " + gameObject.name);
        if (TooltipManager.Instance != null && !string.IsNullOrEmpty(tooltipTitle))
        {
            Debug.Log("Вызываю ShowTooltip");
            TooltipManager.Instance.ShowTooltip(tooltipTitle, tooltipDescription);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }

    private void OnDisable()
    {
        HideTooltipSafe();
    }

    // Вынесли повторяющийся код в отдельный метод (принцип DRY - Don't Repeat Yourself)
    private void HideTooltipSafe()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
}