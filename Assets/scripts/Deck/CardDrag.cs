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

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        BuildManager buildManager = FindFirstObjectByType<BuildManager>();

        // 1. ЕСЛИ ЭТО ПОСТРОЙКА
        if (myCardData.type == CardType.Building)
        {
            if (buildManager != null && buildManager.TryBuildFromDrag(myCardData, Input.mousePosition))
            {
                HandManager.Instance.RemoveCard(myCardData);
                Destroy(gameObject);
            }
            else
            {
                transform.SetParent(originalParent, false);
                transform.SetSiblingIndex(originalSiblingIndex);
            }
        }
        // 2. ЕСЛИ ЭТО КАРТА-ЭФФЕКТ (Лут / Чертежи / Золото)
        else if (myCardData.type == CardType.Effect)
        {
            // ВАЖНО: Вот здесь мы наконец-то дергаем твой менеджер!
            if (CardPlayManager.Instance != null)
            {
                CardPlayManager.Instance.ApplyCardEffect(myCardData);
            }
            else
            {
                Debug.LogError("На сцене нет CardPlayManager!");
            }

            HandManager.Instance.RemoveCard(myCardData); // Удаляем сыгранную карту из руки
            Destroy(gameObject); // Уничтожаем UI карточки
        }
    }
}