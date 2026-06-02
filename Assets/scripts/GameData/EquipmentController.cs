using System.Collections.Generic;

public class EquipmentController
{
    // Строгие слоты
    public ItemData equippedWeapon { get; private set; }
    public ItemData equippedArmor { get; private set; }
    public ItemData equippedAccessory { get; private set; }

    private FeatController unitFeats;

    public EquipmentController(FeatController featController)
    {
        this.unitFeats = featController;
    }

    // Универсальный метод надевания предмета
    public void EquipItem(ItemData newItem)
    {
        if (newItem == null) return;

        // Сначала снимаем предмет из того же слота (если там что-то было)
        UnequipSlot(newItem.slotType);

        // Распределяем по слотам и надеваем новый
        switch (newItem.slotType)
        {
            case ItemSlotType.Weapon: equippedWeapon = newItem; break;
            case ItemSlotType.Armor: equippedArmor = newItem; break;
            case ItemSlotType.Accessory: equippedAccessory = newItem; break;
        }

        // Передаем фиты предмета в контроллер фитов
        foreach (var feat in newItem.grantedFeats)
        {
            // Здесь предполагается, что в FeatController есть метод AddFeat для постоянных фитов
            unitFeats.AddEquipmentFeat(feat);
        }
    }

    // Снять предмет по типу слота
    public void UnequipSlot(ItemSlotType slot)
    {
        ItemData itemToRemove = null;

        // Определяем, что снимаем, и очищаем слот
        switch (slot)
        {
            case ItemSlotType.Weapon:
                itemToRemove = equippedWeapon;
                equippedWeapon = null;
                break;
            case ItemSlotType.Armor:
                itemToRemove = equippedArmor;
                equippedArmor = null;
                break;
            case ItemSlotType.Accessory:
                itemToRemove = equippedAccessory;
                equippedAccessory = null;
                break;
        }

        // Если в слоте что-то было, забираем фиты обратно
        if (itemToRemove != null)
        {
            foreach (var feat in itemToRemove.grantedFeats)
            {
                unitFeats.RemoveEquipmentFeat(feat); // Метод удаления базового фита
            }
        }
    }
}