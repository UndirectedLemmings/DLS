using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Строка одной характеристики в Тренировочном зале.
/// Prefab: statNameText, currentValueText, xpText, progressBar (Slider), trainButton.
/// </summary>
public class TrainingStatEntryUI : MonoBehaviour
{
    public TextMeshProUGUI statNameText;
    public TextMeshProUGUI currentValueText;  // "3 (+1 XP)"
    public TextMeshProUGUI xpText;            // "XP: 7 / 10"
    public Slider xpProgressBar;             // 0..1 заполнение до следующего уровня
    public Button trainButton;               // +1 XP вручную (для тестов / активной тренировки)

    private UnitProgress hero;
    private CharacterStatType stat;
    private TrainingHallPanel panel;

    public void Setup(UnitProgress heroProgress, CharacterStatType statType, TrainingHallPanel ownerPanel)
    {
        hero = heroProgress;
        stat = statType;
        panel = ownerPanel;

        if (trainButton != null) trainButton.onClick.AddListener(OnTrainClicked);

        RefreshView();
    }

    private void OnTrainClicked()
    {
        // Ручная тренировка: тратим 1 XP-очко (уже накопленный из экспедиций)
        // Здесь XP уже есть — кнопка просто «вкладывает» 1 очко в данный стат из общего пула.
        // Для упрощения: AddXP напрямую (+1), в будущем можно сделать пул "свободных XP".
        if (hero == null) return;
        bool levelUp = hero.AddXP(stat, 1);
        if (levelUp)
            Debug.Log($"[Тренировочный зал] {hero.heroName}: +1 к {TranslateStat(stat)}!");
        panel.OnStatUpdated();
    }

    private void RefreshView()
    {
        if (hero == null) return;

        int xp = hero.GetXP(stat);
        int baseVal;
        switch (stat)
        {
            case CharacterStatType.Strength:    baseVal = hero.Template.baseStrength;    break;
            case CharacterStatType.Endurance:   baseVal = hero.Template.baseEndurance;   break;
            case CharacterStatType.Will:        baseVal = hero.Template.baseWill;        break;
            case CharacterStatType.Wisdom:      baseVal = hero.Template.baseWisdom;      break;
            case CharacterStatType.Agility:     baseVal = hero.Template.baseAgility;     break;
            case CharacterStatType.Perception:  baseVal = hero.Template.basePerception;  break;
            default: baseVal = 1; break;
        }

        int featBonus = hero.overworldFeats != null ? hero.overworldFeats.GetBonusForStat(stat) : 0;
        int basePlusFeat = baseVal + featBonus;

        int bonus    = UnitProgress.ComputeXpBonus(xp, basePlusFeat);
        int spent    = UnitProgress.XpSpentForBonus(bonus, basePlusFeat);
        int remainder = xp - spent;
        int nextCost = UnitProgress.XpCostForNextPoint(bonus, basePlusFeat);

        if (statNameText != null)     statNameText.text = TranslateStat(stat);
        if (currentValueText != null) currentValueText.text = bonus > 0
            ? $"{basePlusFeat + bonus} (база {basePlusFeat} +{bonus})"
            : $"{basePlusFeat}";
        if (xpText != null)           xpText.text = $"XP: {remainder} / {nextCost}";
        if (xpProgressBar != null)    xpProgressBar.value = nextCost > 0 ? (float)remainder / nextCost : 0f;
    }

    private static string TranslateStat(CharacterStatType s)
    {
        switch (s)
        {
            case CharacterStatType.Strength:    return "Сила";
            case CharacterStatType.Endurance:   return "Выносливость";
            case CharacterStatType.Will:        return "Воля";
            case CharacterStatType.Wisdom:      return "Мудрость";
            case CharacterStatType.Agility:     return "Ловкость";
            case CharacterStatType.Perception:  return "Восприятие";
            default: return s.ToString();
        }
    }
}
