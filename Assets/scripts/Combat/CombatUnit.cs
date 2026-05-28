using UnityEngine;

public class CombatUnit
{
    public UnitData BaseData { get; private set; }

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
    }

    public void TakeWounds(int woundsAmount)
    {
        if (woundsAmount <= 0 || IsDead) return;

        for (int i = 0; i < woundsAmount; i++)
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