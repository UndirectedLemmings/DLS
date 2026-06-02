using System.Collections.Generic;

public enum FeatDurationType
{
    CombatRounds,    // Спадает в бою (в конце каждого раунда)
    AdventureEvents  // Спадает на глобальной карте (после события/битвы)
}

public class ActiveFeat
{
    public FeatData Feat { get; private set; }
    public int Duration { get; set; }
    public FeatDurationType DurationType { get; private set; }

    public ActiveFeat(FeatData feat, int duration, FeatDurationType durationType)
    {
        Feat = feat;
        Duration = duration;
        DurationType = durationType;
    }
}

public class FeatController
{
    private CombatUnit unit;
    private List<FeatData> baseFeats; // Врожденные фиты (из UnitData)

    // Список для временных баффов/дебаффов
    public List<ActiveFeat> temporaryFeats = new List<ActiveFeat>();

    // НОВОЕ: Список для фитов от экипировки (без таймеров)
    public List<FeatData> equipmentFeats = new List<FeatData>();

    public int CurrentBonusDamage { get; private set; }
    public int CurrentDamageReduction { get; private set; }

    // Конструктор контроллера
    public FeatController(List<FeatData> activeFeats, CombatUnit unit)
    {
        this.unit = unit;
        this.baseFeats = activeFeats != null ? activeFeats : new List<FeatData>();

        // Первичный подсчет постоянных бонусов и добавление базового здоровья
        int totalBonusEndurance = 0;
        foreach (var feat in baseFeats)
        {
            if (feat.triggerType == FeatType.PassiveStats)
            {
                totalBonusEndurance += feat.bonusEndurance;
            }
        }
        if (totalBonusEndurance != 0)
        {
            unit.ApplyEnduranceModifier(totalBonusEndurance);
        }

        RecalculateCombatBonuses();
    }

    // --- НОВЫЙ ФУНКЦИОНАЛ ДЛЯ ЭКИПИРОВКИ ---

    public void AddEquipmentFeat(FeatData feat)
    {
        if (feat == null || equipmentFeats.Contains(feat)) return;

        equipmentFeats.Add(feat);

        // Сразу применяем бонус к здоровью, если он есть
        if (feat.triggerType == FeatType.PassiveStats && feat.bonusEndurance != 0)
        {
            unit.ApplyEnduranceModifier(feat.bonusEndurance);
        }

        RecalculateCombatBonuses();
    }

    public void RemoveEquipmentFeat(FeatData feat)
    {
        if (feat == null || !equipmentFeats.Contains(feat)) return;

        equipmentFeats.Remove(feat);

        // Откатываем бонус к здоровью
        if (feat.triggerType == FeatType.PassiveStats && feat.bonusEndurance != 0)
        {
            unit.ApplyEnduranceModifier(-feat.bonusEndurance);
        }

        RecalculateCombatBonuses();
    }

    // --- ФУНКЦИОНАЛ ДЛЯ ВРЕМЕННЫХ ФИТОВ (Без изменений) ---

    public void AddTemporaryFeat(FeatData newFeat, int duration, FeatDurationType type)
    {
        bool isMutuallyDestroyed = false;

        // Идем с конца, так как можем удалять элементы из списка
        for (int i = temporaryFeats.Count - 1; i >= 0; i--)
        {
            var existingFeat = temporaryFeats[i].Feat;

            // Проверяем, есть ли тег существующего фита в списке "отменяемых" у нового фита
            if (!string.IsNullOrEmpty(existingFeat.effectTag) &&
                newFeat.cancelsTags.Contains(existingFeat.effectTag))
            {
                RemoveTemporaryFeat(temporaryFeats[i]);
                temporaryFeats.RemoveAt(i);
                isMutuallyDestroyed = true;
            }
        }

        if (isMutuallyDestroyed)
        {
            RecalculateCombatBonuses();
            return;
        }

        temporaryFeats.Add(new ActiveFeat(newFeat, duration, type));

        if (newFeat.triggerType == FeatType.PassiveStats && newFeat.bonusEndurance != 0)
        {
            unit.ApplyEnduranceModifier(newFeat.bonusEndurance);
        }

        RecalculateCombatBonuses();
    }

    public void TickCombatRounds()
    {
        TickFeats(FeatDurationType.CombatRounds);
    }

    public void TickAdventureEvents()
    {
        TickFeats(FeatDurationType.AdventureEvents);
    }

    private void TickFeats(FeatDurationType type)
    {
        bool statsChanged = false;

        for (int i = temporaryFeats.Count - 1; i >= 0; i--)
        {
            var activeFeat = temporaryFeats[i];

            if (activeFeat.DurationType == type)
            {
                activeFeat.Duration--;

                if (activeFeat.Duration <= 0)
                {
                    RemoveTemporaryFeat(activeFeat);
                    temporaryFeats.RemoveAt(i);
                    statsChanged = true;
                }
            }
        }

        if (statsChanged)
        {
            RecalculateCombatBonuses();
        }
    }

    private void RemoveTemporaryFeat(ActiveFeat activeFeat)
    {
        if (activeFeat.Feat.triggerType == FeatType.PassiveStats && activeFeat.Feat.bonusEndurance != 0)
        {
            unit.ApplyEnduranceModifier(-activeFeat.Feat.bonusEndurance);
        }
    }

    // --- ОБНОВЛЕННЫЙ ПЕРЕСЧЕТ ---

    private void RecalculateCombatBonuses()
    {
        CurrentBonusDamage = 0;
        CurrentDamageReduction = 0;

        // 1. Считаем базу
        foreach (var feat in baseFeats)
        {
            if (feat.triggerType == FeatType.PassiveStats)
            {
                CurrentBonusDamage += feat.bonusDamage;
                CurrentDamageReduction += feat.damageReduction;
            }
        }

        // 2. Считаем экипировку (НОВОЕ)
        foreach (var feat in equipmentFeats)
        {
            if (feat.triggerType == FeatType.PassiveStats)
            {
                CurrentBonusDamage += feat.bonusDamage;
                CurrentDamageReduction += feat.damageReduction;
            }
        }

        // 3. Считаем временные баффы
        foreach (var activeFeat in temporaryFeats)
        {
            if (activeFeat.Feat.triggerType == FeatType.PassiveStats)
            {
                CurrentBonusDamage += activeFeat.Feat.bonusDamage;
                CurrentDamageReduction += activeFeat.Feat.damageReduction;
            }
        }
    }
}