using UnityEngine;

public class CombatUnit
{
    public UnitData BaseData { get; private set; }

    // Динамические параметры для конкретного боя

    [Header("СОСТОЯНИЯ ВЫНОСЛИВОСТИ (EP)")]
    public int HealthyEP { get; private set; }
    public int TiredEP { get; private set; }
    public int WoundedEP { get; private set; }

    [Header("ДИНАМИЧЕСКИЕ БОНУСЫ (Экипировка, Баффы)")]
    public int bonusStrength;
    public int bonusEndurance;
    public int bonusWill;
    public int bonusWisdom;
    public int bonusAgility;
    public int bonusPerception;

    // Итоговые пулы для бросков (База + Бонусы)
    public int TotalStrength => Mathf.Max(1, BaseData.strength + bonusStrength);
    public int TotalEndurance => Mathf.Max(1, BaseData.endurance + bonusEndurance);
    public int TotalWill => Mathf.Max(1, BaseData.will + bonusWill);
    public int TotalWisdom => Mathf.Max(1, BaseData.wisdom + bonusWisdom);
    public int TotalAgility => Mathf.Max(1, BaseData.agility + bonusAgility);
    public int TotalPerception => Mathf.Max(1, BaseData.perception + bonusPerception);

    // Здоровые EP вычисляются на лету
   // public int HealthyEP => TotalEndurance - TiredEP - WoundedEP;

    // Смерть наступает, когда ранения равны или превышают максимальную выносливость
    public bool IsDead => WoundedEP >= TotalEndurance;
    // инициатива
    public int InitiativeRoll { get; private set; }

    // Параметры для разрешения спорных ситуаций
    public bool IsAttacker { get; private set; }
    public int SlotIndex { get; private set; }

    public CombatUnit(UnitData data, bool isAttacker, int slotIndex)
    {
        BaseData = data;
        TiredEP = 0;
        WoundedEP = 0;
        IsAttacker = isAttacker;
        SlotIndex = slotIndex;
    }

    public void TakeWounds(int woundsAmount)
    {
        if (woundsAmount <= 0) return;

        for (int i = 0; i < woundsAmount; i++)
        {
            if (IsDead) break; // Защита от лишних циклов, если юнит уже погиб

            if (TiredEP > 0)
            {
                // Усталость переходит в ранение
                TiredEP--;
                WoundedEP++;
            }
            else if (HealthyEP > 0)
            {
                // Здоровое очко становится ранением
                WoundedEP++;
            }
        }
    }

    // Трата EP на действия (становится "усталым")
    public bool TryExhaustEP(int cost)
    {
        if (HealthyEP >= cost)
        {
            TiredEP += cost;
            return true;
        }
        return false;
    }

    // Тот самый бросок d10 + Восприятие
    public void RollInitiative()
    {
        // Random.Range для int исключает верхнюю границу, поэтому пишем 11, чтобы выпадало 1-10
        InitiativeRoll = BaseData.perception + Random.Range(1, 11);
    }
}