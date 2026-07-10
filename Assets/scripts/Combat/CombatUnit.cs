using System.Collections.Generic;
using UnityEngine;

public class CombatUnit
{
    public UnitProgress Progress { get; private set; }
    public FeatController featController;
    // EquipmentController боевому юниту теперь, возможно, даже не нужен, 
    // если он занимался только статами. Но оставляем, если он управляет боевыми эффектами оружия.

    [Header("ВРЕМЕННЫЕ БОЕВЫЕ БОНУСЫ (Магия, боевые стойки и т.д.)")]
    public int combatBonusStrength;
    public int combatBonusEndurance;
    public int combatBonusWill;
    public int combatBonusWisdom;
    public int combatBonusAgility;
    public int combatBonusPerception;

    // Итоговые БОЕВЫЕ характеристики: Статы из мира (База+Шмот) + Боевые баффы
    public int BattleStrength => Mathf.Max(1, Progress.TotalStrength + combatBonusStrength);
    public int BattleEndurance => Mathf.Max(1, Progress.TotalEndurance + combatBonusEndurance);
    public int BattleWill => Mathf.Max(1, Progress.TotalWill + combatBonusWill);
    public int BattleWisdom => Mathf.Max(1, Progress.TotalWisdom + combatBonusWisdom);
    public int BattleAgility => Mathf.Max(1, Progress.TotalAgility + combatBonusAgility);
    public int BattlePerception => Mathf.Max(1, Progress.TotalPerception + combatBonusPerception);

    // Живые ссылки на динамическое состояние из прогресса
    public int HealthyEP { get => Progress.currentHealthyEP; private set => Progress.currentHealthyEP = value; }
    public int TiredEP { get => Progress.currentTiredEP; private set => Progress.currentTiredEP = value; }
    public int WoundedEP { get => Progress.currentWoundedEP; private set => Progress.currentWoundedEP = value; }
    public int CurrentMana { get => Progress.currentMana; private set => Progress.currentMana = value; }

    public bool IsDead => WoundedEP >= BattleEndurance;
    public int InitiativeRoll { get; private set; }
    public bool IsAttacker { get; private set; }
    public int SlotIndex { get; private set; }


    // --- НОВАЯ, ЧИСТАЯ ЛОГИКА КУБОВ УРОНА ---
    // Базовое количество кубов для атаки (обычно 1, если без оружия бьют кулаками)
    private const int BASE_DICE = 0;

    // CombatUnit просто забирает уже посчитанные бонусные кубы из контроллера фитов!
    public int CurrentWeaponBonusDice => BASE_DICE + (featController != null ? featController.BonusDiceCount : 0);

    // МЕТОДЫ UpdateEquipmentBonuses И GetEquipmentBonusDice УДАЛЕНЫ ПОЛНОСТЬЮ!
    // Они больше не нужны, так как при надевании предмета (в UnitProgress)
    // фит автоматически попадает в FeatController и прибавляется к BonusDiceCount.

    public void ResetBonuses()
    {
        // Здесь сбрасываем только ВРЕМЕННЫЕ боевые баффы (если есть), 
        // кубы шмота сбрасывать не нужно — они постоянны, пока вещь надета.
        combatBonusStrength = 0;
        combatBonusEndurance = 0;
        combatBonusWill = 0;
        combatBonusWisdom = 0;
        combatBonusAgility = 0;
        combatBonusPerception = 0;
    }

    /// <summary>
    /// Возвращает итоговое боевое значение характеристики на основе перечисления CharacterStatType
    /// </summary>
    public int GetBattleStatValue(CharacterStatType statType)
    {
        switch (statType)
        {
            case CharacterStatType.Strength: return BattleStrength;
            case CharacterStatType.Endurance: return BattleEndurance;
            case CharacterStatType.Will: return BattleWill;
            case CharacterStatType.Wisdom: return BattleWisdom;
            case CharacterStatType.Agility: return BattleAgility;
            case CharacterStatType.Perception: return BattlePerception;
            default: return BattleAgility; // Страховочный вариант
        }
    }

    // Внутри CombatUnit.cs
    public CombatUnit(UnitProgress progress, bool isAttacker, int slotIndex)
    {
        Progress = progress;
        IsAttacker = isAttacker;
        SlotIndex = slotIndex;

        // 1. Создаем контроллер (передаем только начальные активные фиты)
        featController = new FeatController(progress.activeFeats, this);

        // 2. ДОБАВЛЯЕМ фиты от стартовой экипировки напрямую
        if (progress.equippedWeapon != null && progress.equippedWeapon.grantedFeats != null)
            foreach (var feat in progress.equippedWeapon.grantedFeats)
                featController.AddEquipmentFeat(feat);

        if (progress.equippedArmor != null && progress.equippedArmor.grantedFeats != null)
            foreach (var feat in progress.equippedArmor.grantedFeats)
                featController.AddEquipmentFeat(feat);

        if (progress.equippedAccessory != null && progress.equippedAccessory.grantedFeats != null)
            foreach (var feat in progress.equippedAccessory.grantedFeats)
                featController.AddEquipmentFeat(feat);

        // 3. Теперь контроллер всё знает (и бонусы, и лут с фитов)
    }

    public void SetActiveVisual(bool isActive)
    {
        // Ищем соответствующий слот в UIManager и дергаем его
        // У нас есть SlotIndex и IsAttacker
        var ui = CombatUIManager.Instance;
        if (IsAttacker) ui.heroSlots[SlotIndex].SetActive(isActive);
        else ui.enemySlots[SlotIndex].SetActive(isActive);
    }

    public void SetTargetVisual(bool isTargeted)
    {
        var ui = CombatUIManager.Instance;
        if (IsAttacker) ui.heroSlots[SlotIndex].SetTargeted(isTargeted);
        else ui.enemySlots[SlotIndex].SetTargeted(isTargeted);
    }
    public string UnitName
    {
        get
        {
            // Если есть сгенерированное имя (герои) — используем его
            if (Progress != null && !string.IsNullOrEmpty(Progress.heroName))
                return Progress.heroName;

            // Иначе берем имя из шаблона (монстры)
            return Progress?.Template?.unitName ?? "Неизвестный боец";
        }
    }

    public void ApplyEnduranceModifier(int bonusAmount)
    {
        combatBonusEndurance += bonusAmount;
        HealthyEP += bonusAmount;
        // Ограничиваем с учётом БОЕВЫХ лимитов выносливости
        HealthyEP = Mathf.Clamp(HealthyEP, 0, BattleEndurance - TiredEP - WoundedEP);
    }

    public void TakeWounds(int incomingDamage)
    {
        int reduction = featController != null ? featController.CurrentDamageReduction : 0;
        int finalDamage = Mathf.Max(0, incomingDamage - reduction);

        if (finalDamage <= 0 || IsDead) return;

        for (int i = 0; i < finalDamage; i++)
        {
            if (TiredEP > 0) { TiredEP--; WoundedEP++; }
            else if (HealthyEP > 0) { HealthyEP--; WoundedEP++; }

            if (IsDead)
            {
                GenerateLootOnDeath();
                break;
            }
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
        // Инициатива теперь зависит от мирового Восприятия (с учётом шмота) + рандом
        InitiativeRoll = BattlePerception + Random.Range(1, 11);
    }

    public void GenerateLootOnDeath()
    {
        List<ItemData> droppedItems = new List<ItemData>();

        // 1. БАЗОВЫЙ ЛУТ (кости, шкуры из UnitData)
        if (Progress.Template.baseLoot != null)
        {
            foreach (var loot in Progress.Template.baseLoot)
            {
                if (Random.Range(1, 101) <= loot.dropChance)
                {
                    int amountToDrop = Random.Range(loot.minAmount, loot.maxAmount + 1);
                    for (int i = 0; i < amountToDrop; i++) droppedItems.Add(loot.item);
                }
            }
        }

        // 2. ЛУТ СО ВСЕХ ФИТОВ (Вампиризм, Классы, и ЭКИПИРОВКА!)
        // Используем метод, который собирает базу + временные + ЭКИПИРОВКУ
        var allFeats = Progress.GetAllActiveFeats();
        if (allFeats != null)
        {
            foreach (var feat in allFeats)
            {
                if (feat != null && feat.dropsLoot && Random.Range(1, 101) <= feat.featLoot.dropChance)
                {
                    int amountToDrop = Random.Range(feat.featLoot.minAmount, feat.featLoot.maxAmount + 1);
                    for (int i = 0; i < amountToDrop; i++) droppedItems.Add(feat.featLoot.item);
                }
            }
        }

        // 3. ОТПРАВЛЯЕМ ЛУТ В СКЛАД ЧЕРЕЗ ТВОЙ СТАРЫЙ МЕТОД
        if (droppedItems.Count > 0)
        {
            foreach (var item in droppedItems)
            {
                // Отправляем вещь в инвентарь (тут же сработает твой лог и обновление UI!)
                GameManager.Instance.AddLootToInventory(item);

                // Пишем в лог боя чисто для красоты
                if (CombatUIManager.Instance != null)
                {
                    CombatUIManager.Instance.AddLogMessage($"{Progress.Template.unitName} роняет: {item.itemName}");
                }
            }
        }
    }
}