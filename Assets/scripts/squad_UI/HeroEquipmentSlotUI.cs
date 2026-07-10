using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// УБРАЛИ ЛОКАЛЬНЫЙ enum SlotType, теперь используем общий ItemSlotType из ItemData!

public class HeroEquipmentSlotUI : MonoBehaviour, IDropHandler
{
    [Header("Настройки слота")]
    public ItemSlotType slotType; // Выбираем в инспекторе: Weapon, Armor или Accessory

    // Слот работает с живым UnitProgress, а не со статичным UnitData
    [HideInInspector] public UnitProgress targetHero;

    private Image slotImage;

    private void Awake()
    {
        // Слот сам находит свой Image на этом же объекте!
        slotImage = GetComponent<Image>();
    }

    // Метод для затемнения слота, если он пустой (вызывается при открытии панели)
    public void RefreshSlotVisual()
    {
        if (slotImage == null) slotImage = GetComponent<Image>();
        if (targetHero == null) return;

        // Определяем, какой предмет надет, обращаясь к слотам внутри UnitProgress
        ItemData equippedItem = null;
        switch (slotType)
        {
            case ItemSlotType.Weapon: equippedItem = targetHero.equippedWeapon; break;
            case ItemSlotType.Armor: equippedItem = targetHero.equippedArmor; break;
            case ItemSlotType.Accessory: equippedItem = targetHero.equippedAccessory; break;
        }

        // Если вещь есть — рисуем её. Если нет — сбрасываем в серую заглушку
        if (equippedItem != null)
        {
            slotImage.sprite = equippedItem.itemIcon;
            slotImage.color = Color.white;
        }
        else
        {
            slotImage.sprite = null;
            slotImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Возвращаем затемнение
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null) return;

        DraggableItemUI draggableItem = droppedObject.GetComponent<DraggableItemUI>();

        if (draggableItem != null)
        {
            if (targetHero == null) return;

            ItemData itemToEquip = draggableItem.myItemData;

            if (itemToEquip == null) return;

            // --- ЗАЩИТА СЛОТОВ АКТИВИРОВАНА ---
            if (itemToEquip.slotType != this.slotType)
            {
                Debug.LogWarning($"[UI Экипировки] Нельзя надеть {itemToEquip.itemName} типа {itemToEquip.slotType} в ячейку {this.slotType}!");
                return; // Прерываем выполнение, вещь возвращается на место
            }

            // Записываем шмотку в память UnitProgress
            switch (slotType)
            {
                case ItemSlotType.Weapon: targetHero.equippedWeapon = itemToEquip; break;
                case ItemSlotType.Armor: targetHero.equippedArmor = itemToEquip; break;
                case ItemSlotType.Accessory: targetHero.equippedAccessory = itemToEquip; break;
            }

            // Имя вытягиваем безопасно через Template
            string heroName = (targetHero.Template != null) ? targetHero.Template.unitName : "Неизвестный Герой";
            Debug.Log($"DLS: Успешно надели {itemToEquip.itemName} в слот {slotType} героя {heroName}");

            RefreshSlotVisual();

            // Обновляем UI отряда (SquadPanel) чтобы отобразить новые бонусы
            if (SquadUIManager.Instance != null)
            {
                SquadUIManager.Instance.UpdateSquadUI();
            }

            // Если на герое (или на префабе карты) в этот момент работает FeatController, 
            // здесь можно вызвать пересчет пассивных фитов от экипировки:
            // targetHero.UpdateEquipmentFeats(); 

            // Удаляем из инвентаря и уничтожаем UI-строчку
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RemoveLootFromInventory(itemToEquip);
            }

            Destroy(droppedObject);
        }
    }
}