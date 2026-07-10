using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public CardData myCardData;

    private Transform originalParent;
    private int originalSiblingIndex;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // Отрываем от Layout Group, сохраняя позицию под мышкой
        transform.SetParent(rootCanvas.transform, true);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    private bool isProcessing = false;
    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // 1. ПОСТРОЙКИ
        if (myCardData.type == CardType.Building)
        {
            BuildManager buildManager = FindFirstObjectByType<BuildManager>();

            if (buildManager != null && buildManager.TryBuildFromDrag(myCardData, Input.mousePosition))
            {
                Destroy(gameObject);
            }
            else
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
        }
        // 2. ЭФФЕКТЫ
        else if (myCardData.type == CardType.Effect)
        {
            // ВАЖНО: Мы просто вызываем эффект. 
            // Если внутри ApplyCardEffect он сработает, менеджер сам удалит карту.
            if (CardPlayManager.Instance != null)
            {
                CardPlayManager.Instance.ApplyCardEffect(myCardData, this.gameObject);
            }
            else
            {
                // Если менеджера нет, просто возвращаем карту в руку
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
        }
    }
}