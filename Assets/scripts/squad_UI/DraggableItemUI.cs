using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Обязательные интерфейсы для Drag-and-Drop в Unity
public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Данные")]
    public ItemData myItemData;

    [Header("UI Элементы")]
    public Image itemIcon;
    public TMP_Text itemNameText; // Или TMP_Text, если используешь TextMeshPro
    public Transform parentAfterDrag; // Запоминаем, куда вернуть вещь, если бросили мимо
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // CanvasGroup нужен, чтобы мышка "пробивала" предмет насквозь во время перетаскивания
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(ItemData item)
    {
        myItemData = item;
        if (itemIcon != null) itemIcon.sprite = item.itemIcon;
        if (itemNameText != null) itemNameText.text = item.itemName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parentAfterDrag = transform.parent;

        // Переносим объект в корень Canvas, чтобы он рисовался ПОВЕРХ всех других окон
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();

        // Отключаем блокировку лучей, чтобы ячейка под предметом могла "почувствовать" бросок
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Предмет следует за мышкой
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Возвращаем настройки
        transform.SetParent(parentAfterDrag);
        canvasGroup.blocksRaycasts = true;
    }
}