using UnityEngine;

public class CombatUnit
{
    public UnitData BaseData { get; private set; }
    public FeatController featController;
    public EquipmentController equipmentController;

    [Header("СОСТОЯНИЯ ВЫНОСЛИВОСТИ (EP)")]
    public int HealthyEP { get; private set; }
    public int TiredEP { get; private set; }
    public int WoundedEP { get; private set; }

    [Header("ДИНАМИЧЕСКИЕ БОНУСЫ")]
    public int bonusStrength;
    public int bonusEndurance;
    public int bonusWill;
    public int bonusWisdom;
    public int bonusAgility;
    public int bonusPerception;

    public int TotalStrength => Mathf.Max(1, BaseData.strength + bonusStrength);
    public int TotalEndurance => Mathf.Max(1, BaseData.endurance + bonusEndurance);
    public int TotalWill => Mathf.Max(1, BaseData.will + bonusWill);
    public int TotalWisdom => Mathf.Max(1, BaseData.wisdom + bonusWisdom);
    public int TotalAgility => Mathf.Max(1, BaseData.agility + bonusAgility);
    public int TotalPerception => Mathf.Max(1, BaseData.perception + bonusPerception);

    // Выносливость — это сумма всех состояний
    public bool IsDead => WoundedEP >= TotalEndurance;
    public int InitiativeRoll { get; private set; }
    public bool IsAttacker { get; private set; }
    public int SlotIndex { get; private set; }

    public CombatUnit(UnitData data, bool isAttacker, int slotIndex)
    {
        BaseData = data;
        IsAttacker = isAttacker;
        SlotIndex = slotIndex;

        // При старте боя все очки выносливости — "здоровые"
        HealthyEP = Mathf.Max(1, data.endurance);
        TiredEP = 0;
        WoundedEP = 0;

        // 1. Создаем контроллер фитов (ты это уже сделал ранее)
        featController = new FeatController(data.activeFeats, this);

        // 2. Создаем контроллер экипировки, передавая ему FeatController
        equipmentController = new EquipmentController(featController);

        // 3. Считываем предметы из UnitData и надеваем их
        if (data.weaponSlot != null) equipmentController.EquipItem(data.weaponSlot);
        if (data.armorSlot != null) equipmentController.EquipItem(data.armorSlot);
        if (data.accessorySlot != null) equipmentController.EquipItem(data.accessorySlot);
    }

    public void ApplyEnduranceModifier(int bonusAmount)
    {
        bonusEndurance += bonusAmount;

        // Прибавляем или отнимаем здоровье
        HealthyEP += bonusAmount;

        // Защита от ухода в минус при снятии баффов
        if (HealthyEP < 0) HealthyEP = 0;
    }

    public void TakeWounds(int incomingDamage)
    {
        int reduction = featController != null ? featController.CurrentDamageReduction : 0;
        int finalDamage = Mathf.Max(0, incomingDamage - reduction);

        if (finalDamage <= 0 || IsDead) return;

        for (int i = 0; i < finalDamage; i++)
        {
            if (TiredEP > 0)
            {
                TiredEP--;
                WoundedEP++;
            }
            else if (HealthyEP > 0)
            {
                HealthyEP--;
                WoundedEP++;
            }

            if (IsDead) break;
        }
    }

    public bool TryExhaustEP(int cost)
    {
        if (HealthyEP >= cost)
        {
            HealthyEP -= cost;
            TiredEP += cost;
            return true;
        }
        return false;
    }

    public void RollInitiative()
    {
        InitiativeRoll = TotalPerception + Random.Range(1, 11);
    }
}