using System.Collections.Generic;
using UnityEngine;
public class EquipmentController
{
    // Строгие слоты
    public ItemData equippedWeapon { get; private set; }
    public ItemData equippedArmor { get; private set; }
    public ItemData equippedAccessory { get; private set; }

    private FeatController unitFeats;
    private CombatUnit ownerUnit; // Добавили поле
    public EquipmentController(FeatController featController, CombatUnit owner)
    {
        this.unitFeats = featController;
        this.ownerUnit = owner; // Инициализируем
    }

    // Универсальный метод надевания предмета
    public void EquipItem(ItemData newItem)
    {
        if (newItem == null) return;

        // Снимаем старый предмет, если есть
        UnequipSlot(newItem.slotType);

        // Надеваем новый
        switch (newItem.slotType)
        {
            case ItemSlotType.Weapon: equippedWeapon = newItem; break;
            case ItemSlotType.Armor: equippedArmor = newItem; break;
            case ItemSlotType.Accessory: equippedAccessory = newItem; break;
        }

        foreach (var feat in newItem.grantedFeats)
        {
            unitFeats.AddEquipmentFeat(feat);
        }
        unitFeats.RequestRecalculation();
        UpdateUnitBonuses(); // Обновляем
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
        UpdateUnitBonuses();
    }
    private void UpdateUnitBonuses()
    {
        // 1. Сбрасываем старые бонусы
        // 2. Устанавливаем новые
        // ВСЯ МАТЕМАТИКА УДАЛЕНА ОТСЮДА! 
        // Теперь все бонусы (включая кубы урона) считает FeatController 
        // автоматически при вызове unitFeats.RequestRecalculation() выше.

        Debug.Log("[Equipment] Экипировка обновлена, статы пересчитаны контроллером фитов.");
    }

    // Внутри EquipmentController.cs

    public void InitializeStartingEquipmentFeats()
    {
        var progress = ownerUnit.Progress;

        // Вспомогательный метод для добавления списка фитов
        void AddFeatsFromList(List<FeatData> feats)
        {
            if (feats == null) return;
            foreach (var feat in feats)
            {
                if (feat != null)
                    unitFeats.AddEquipmentFeat(feat);
            }
        }

        // Теперь просто вызываем этот помощник для каждого слота
        AddFeatsFromList(progress.equippedWeapon?.grantedFeats);
        AddFeatsFromList(progress.equippedArmor?.grantedFeats);
        AddFeatsFromList(progress.equippedAccessory?.grantedFeats);

        // В конце запрашиваем один общий пересчет бонусов, 
        // чтобы не пересчитывать статы после каждого добавленного фита
        unitFeats.RequestRecalculation();
    }

}