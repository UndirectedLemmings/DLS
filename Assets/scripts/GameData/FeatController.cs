using System.Collections.Generic;
using static FeatData;

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

    public int BonusDiceCount { get; private set; }
    public int BonusStrength { get; private set; }
    public int BonusEndurance { get; private set; }
    public int BonusWill { get; private set; }
    public int BonusWisdom { get; private set; }
    public int BonusAgility { get; private set; }
    public int BonusPerception { get; private set; }

    // Конструктор контроллера
    public FeatController(List<FeatData> activeFeats, CombatUnit unit)
    {
        this.unit = unit;

        // БЕЗОПАСНАЯ инициализация: исключаем попадание null-элементов в список
        this.baseFeats = new List<FeatData>();
        if (activeFeats != null)
        {
            foreach (var feat in activeFeats)
            {
                if (feat != null) this.baseFeats.Add(feat);
            }
        }

        /* Первичный подсчет постоянных бонусов
        int totalBonusEndurance = 0;
        foreach (var feat in baseFeats)
        {
            if (feat.triggerType == FeatType.PassiveStats)
            {
                totalBonusEndurance += feat.bonusEndurance;
            }
        }

        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Проверяем, существует ли unit!
        // Вне боя (на глобальной карте) unit равен null, и мы просто пропускаем этот шаг.
        if (totalBonusEndurance != 0 && this.unit != null)
        {
            this.unit.ApplyEnduranceModifier(totalBonusEndurance);
        }*/ //удваивает бонусы (т.е. конфликтует с UnitProgress)

        RecalculateCombatBonuses();
    }

    public CharacterStatType CurrentAttackStat { get; private set; } = CharacterStatType.Agility;
    public CharacterStatType CurrentDefenseStat { get; private set; } = CharacterStatType.Agility;

    public int GetBonusForStat(CharacterStatType stat)
    {
        switch (stat)
        {
            case CharacterStatType.Strength:    return BonusStrength;
            case CharacterStatType.Endurance:   return BonusEndurance;
            case CharacterStatType.Will:        return BonusWill;
            case CharacterStatType.Wisdom:      return BonusWisdom;
            case CharacterStatType.Agility:     return BonusAgility;
            case CharacterStatType.Perception:  return BonusPerception;
            default: return 0;
        }
    }

    public List<FeatData> GetEquipmentFeats()
    {
        return equipmentFeats;
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

    public void ExecuteTriggers(FeatType triggerType)
    {
        ExecuteTriggers(triggerType, null);
    }

    public void ExecuteTriggers(FeatType triggerType, CombatTriggerContext context)
    {
        if (unit == null) return;

        foreach (var feat in baseFeats)
        {
            if (feat != null && feat.triggerType == triggerType)
            {
                feat.ExecuteEffect(unit, context);
            }
        }

        foreach (var feat in equipmentFeats)
        {
            if (feat != null && feat.triggerType == triggerType)
            {
                feat.ExecuteEffect(unit, context);
            }
        }

        foreach (var activeFeat in temporaryFeats)
        {
            if (activeFeat?.Feat != null && activeFeat.Feat.triggerType == triggerType)
            {
                activeFeat.Feat.ExecuteEffect(unit, context);
            }
        }
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
    public void RequestRecalculation()
    {
        RecalculateCombatBonuses();
    }
    public TargetPriority GetTargetPriority()
    {
        // 1. Сначала проверяем экипировку (Оружие меняет роль)
        if (equipmentFeats != null)
        {
            foreach (var feat in equipmentFeats)
            {
                if (feat.targetPriority != TargetPriority.Frontline)
                    return feat.targetPriority;
            }
        }

        // 2. Если оружие ничего не меняет, проверяем врожденные/классовые фиты
        if (baseFeats != null)
        {
            foreach (var feat in baseFeats)
            {
                if (feat.targetPriority != TargetPriority.Frontline)
                    return feat.targetPriority;
            }
        }

        // 3. По умолчанию все бьют того, кто ближе
        return TargetPriority.Frontline;
    }

    // --- ВЫЗОВ ГЛОБАЛЬНЫХ ТРИГГЕРОВ ---
    public void TriggerAdventureStartFeats(UnitProgress progress, bool isLeader)
    {
        UnityEngine.Debug.Log($"<color=orange>[ДИАГНОСТИКА]</color> Запуск фитов для: {progress.heroName}. Лидер: {isLeader}. Всего фитов в базе: {baseFeats.Count}");

        foreach (var feat in baseFeats)
        {
            if (feat == null) continue;

            UnityEngine.Debug.Log($"<color=orange>[ДИАГНОСТИКА]</color> Вижу фит: {feat.name} | Тип: {feat.triggerType} | LeaderOnly: {feat.leaderOnly}");

            if (feat.triggerType == FeatType.OnAdventureStart)
            {
                if (feat.leaderOnly && !isLeader)
                {
                    UnityEngine.Debug.Log($"<color=yellow>[ПРОПУСК]</color> Фит {feat.name} пропущен (герой не является лидером).");
                    continue;
                }

                UnityEngine.Debug.Log($"<color=lime>[ЗАПУСК]</color> ВЫПОЛНЯЕМ ЭФФЕКТ ФИТА: {feat.name}!");
                feat.ExecuteAdventureStartEffect(progress);
            }
        }
    }
    private void RemoveTemporaryFeat(ActiveFeat activeFeat)
    {
        if (activeFeat.Feat.triggerType == FeatType.PassiveStats && activeFeat.Feat.bonusEndurance != 0)
        {
            unit.ApplyEnduranceModifier(-activeFeat.Feat.bonusEndurance);
        }
    }

    // --- ПОИСК ФИТОВ ПО ТЕГУ ---
    public bool HasFeatWithTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;

        // Добавлена проверка на null для каждого элемента (feat != null)
        foreach (var feat in baseFeats)
        {
            if (feat != null && feat.effectTag == tag) return true;
        }

        foreach (var feat in equipmentFeats)
        {
            if (feat != null && feat.effectTag == tag) return true;
        }

        foreach (var activeFeat in temporaryFeats)
        {
            if (activeFeat != null && activeFeat.Feat != null && activeFeat.Feat.effectTag == tag) return true;
        }

        return false;
    }

    public FeatData FindFirstFeatByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return null;

        foreach (var feat in baseFeats)
            if (feat != null && feat.effectTag == tag) return feat;

        foreach (var feat in equipmentFeats)
            if (feat != null && feat.effectTag == tag) return feat;

        foreach (var activeFeat in temporaryFeats)
            if (activeFeat?.Feat != null && activeFeat.Feat.effectTag == tag) return activeFeat.Feat;

        return null;
    }

    // --- ОБНОВЛЕННЫЙ ПЕРЕСЧЕТ ---

    private void RecalculateCombatBonuses()
    {
        CurrentBonusDamage = 0;
        CurrentDamageReduction = 0;
        BonusDiceCount = 0;
        BonusStrength = 0;
        BonusEndurance = 0;
        BonusWill = 0;
        BonusWisdom = 0;
        BonusAgility = 0;
        BonusPerception = 0;

        // По умолчанию выбираем атакующую характеристику в зависимости от позиции в формации:
        // — первые два слота (0 и 1) используют Силу, задние два слота (2 и 3) — Ловкость
        if (unit != null)
        {
            if (unit.SlotIndex == 2 || unit.SlotIndex == 3)
                CurrentAttackStat = CharacterStatType.Agility;
            else
                CurrentAttackStat = CharacterStatType.Strength;
        }
        else
        {
            CurrentAttackStat = CharacterStatType.Strength;
        }

        CurrentDefenseStat = CharacterStatType.Endurance;

        void ApplyFeatStats(FeatData feat)
        {
            if (feat != null && feat.triggerType == FeatType.PassiveStats)
            {
                // Старые боевые бонусы и условное применение бонусов по позиции
                bool applyBonuses = true;
                if (feat.bonusesOnlyForBackline)
                {
                    applyBonuses = (unit != null && (unit.SlotIndex == 2 || unit.SlotIndex == 3));
                }

                if (applyBonuses)
                {
                    CurrentBonusDamage += feat.bonusDamage;
                    CurrentDamageReduction += feat.damageReduction;
                    BonusDiceCount += feat.bonusDiceCount;

                    // Новые глобальные бонусы
                    BonusStrength += feat.bonusStrength;
                    BonusEndurance += feat.bonusEndurance;
                    BonusWill += feat.bonusWill;
                    BonusWisdom += feat.bonusWisdom;
                    BonusAgility += feat.bonusAgility;
                    BonusPerception += feat.bonusPerception;
                }

                // Переопределение характеристик боя от оружия/экипировки
                if (feat.overridesCombatStats)
                {
                    CurrentAttackStat = feat.attackStat;
                    CurrentDefenseStat = feat.defenseStat;
                }
            }
        }

        // 2. Пробегаемся по всем спискам и суммируем
        foreach (var feat in baseFeats) ApplyFeatStats(feat);
        foreach (var feat in equipmentFeats) ApplyFeatStats(feat);

        foreach (var activeFeat in temporaryFeats)
        {
            if (activeFeat != null) ApplyFeatStats(activeFeat.Feat);
        }
    }
}