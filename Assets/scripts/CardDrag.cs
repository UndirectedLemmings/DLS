using UnityEngine;
using UnityEngine.EventSystems; // Обязательно для событий перетаскивания
using UnityEngine.UI;

// Подключаем нужные интерфейсы
public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public CardData myCardData; // Какая именно это карта (заполним через код)

    private Transform originalParent; // Где карта лежала до перетаскивания (Панель)
    private int originalSiblingIndex; // Её порядковый номер в руке
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();

        // CanvasGroup нужен, чтобы делать карту "прозрачной" для кликов, 
        // иначе мы не сможем прокликать сквозь нее на фундамент
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Запоминаем, откуда взяли карту
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        // 2. Вырываем карту из Layout Group и кидаем в корень Canvas, чтобы она свободно летала
        transform.SetParent(rootCanvas.transform, true);

        // 3. Отключаем физику мыши для этой карточки, чтобы луч бил сквозь неё в игровой мир
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Карта просто следует за курсором
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Возвращаем физику мыши
        canvasGroup.blocksRaycasts = true;

        // Ищем нашего прораба
        BuildManager buildManager = FindFirstObjectByType<BuildManager>();

        // Просим BuildManager попробовать построить здание там, где мы отпустили мышь
        if (buildManager != null && buildManager.TryBuildFromDrag(myCardData, Input.mousePosition))
        {
            // Если постройка удалась, визуальная карточка нам больше не нужна
            Destroy(gameObject);
        }
        else
        {
            // Если мы бросили карту мимо фундамента или передумали, возвращаем её в руку
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
    }
}
