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

    // --- НОВЫЙ ФУНКЦИОНАЛ ДЛЯ ВРЕМЕННЫХ ФИТОВ ---

    public void AddTemporaryFeat(FeatData feat, int duration, FeatDurationType type)
    {
        temporaryFeats.Add(new ActiveFeat(feat, duration, type));

        // Если бафф дает плюс/минус к здоровью, применяем мгновенно
        if (feat.triggerType == FeatType.PassiveStats && feat.bonusEndurance != 0)
        {
            unit.ApplyEnduranceModifier(feat.bonusEndurance);
        }

        RecalculateCombatBonuses();
    }

    // Вызывать из CombatManager.cs в конце каждого раунда боя
    public void TickCombatRounds()
    {
        TickFeats(FeatDurationType.CombatRounds);
    }

    // Вызывать на глобальной карте после битвы или ивента
    public void TickAdventureEvents()
    {
        TickFeats(FeatDurationType.AdventureEvents);
    }

    private void TickFeats(FeatDurationType type)
    {
        bool statsChanged = false;

        // Идем с конца списка, так как можем удалять элементы
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
        // Когда эффект заканчивается, нужно "откатить" изменение здоровья
        if (activeFeat.Feat.triggerType == FeatType.PassiveStats && activeFeat.Feat.bonusEndurance != 0)
        {
            // Отнимаем то, что добавили
            unit.ApplyEnduranceModifier(-activeFeat.Feat.bonusEndurance);
        }
    }

    // Метод пересчитывает Броню и Урон, суммируя базу и временные эффекты
    private void RecalculateCombatBonuses()
    {
        CurrentBonusDamage = 0;
        CurrentDamageReduction = 0;

        foreach (var feat in baseFeats)
        {
            if (feat.triggerType == FeatType.PassiveStats)
            {
                CurrentBonusDamage += feat.bonusDamage;
                CurrentDamageReduction += feat.damageReduction;
            }
        }

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