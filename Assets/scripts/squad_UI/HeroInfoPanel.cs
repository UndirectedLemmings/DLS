using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HeroInfoPanel : MonoBehaviour
{
    [Header("Основные данные (Индивидуальная часть)")]
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text classText;
    public Slider healthyEPBar;
    public TMP_Text healthyEPText;

    [Header("Характеристики (Двухколоночная таблица)")]
    public TMP_Text enduranceValueText;
    public TMP_Text strengthValueText;
    public TMP_Text perceptionValueText;
    public TMP_Text agilityValueText;
    public TMP_Text wisdomValueText;
    public TMP_Text willValueText;

    [Header("Экипировка")]
    public HeroEquipmentSlotUI[] equipmentSlots;

    [Header("Контейнеры Доменов Фитов (Внутри Scroll View)")]
    public GameObject featPrefab;
    public Transform historicalContainer;
    public Transform militaryContainer;
    public Transform survivalContainer;
    public Transform knowledgeContainer;

    [Header("Deck Section")]
    public Transform deckPreviewContainer; // Сюда перетащишь свой DeckPreviewContainer
    public GameObject cardPrefab; // Префаб карточки
    private void Start()
    {
        // Автоматически закрываем панель при старте сцены.
        // Теперь можно оставлять окно включенным в редакторе Unity — игра сама его спрячет!
        ClosePanel();
    }
    public void OpenPanel(UnitProgress hero)
    {
        if (hero == null) return;

        gameObject.SetActive(true);

        // --- БЕЗОПАСНАЯ ИНИЦИАЛИЗАЦИЯ КОНТРОЛЛЕРА ---
        // Если у героя еще нет контроллера (например, он только создан), создаем его!
        if (hero.overworldFeats == null)
        {
            hero.overworldFeats = new FeatController(hero.GetAllActiveFeats(), null);
        }

        // ==========================================
        // --- 1. ИНДИВИДУАЛЬНАЯ ЧАСТЬ ---
        // ==========================================
        if (hero.Template != null && hero.Template.portrait != null)
        {
            portraitImage.sprite = hero.Template.portrait;
            portraitImage.color = Color.white;
        }
        else
        {
            portraitImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }

        nameText.text = hero.Template != null ? hero.heroName : "Неизвестный герой";

        // Временно ставим дефолтный класс, ниже он перезапишется, если есть фит
        classText.text = "Искатель приключений";

        if (healthyEPBar != null)
        {
            healthyEPBar.maxValue = hero.MaxEP;
            healthyEPBar.value = hero.currentHealthyEP;
            if (healthyEPText != null)
            {
                healthyEPText.text = $"{hero.currentHealthyEP} / {hero.MaxEP}";
            }
        }

        // ==========================================
        // --- 2. ХАРАКТЕРИСТИКИ И D10 КУБЫ ---
        // ==========================================
        RefreshStatsTable(hero);

        // ==========================================
        // --- 3. ОЧИСТКА И ЗАПОЛНЕНИЕ ФИТОВ ---
        // ==========================================
        ClearAllFeatContainers();

        // ИСПРАВЛЕНО: Берем динамический список фитов прямо из прогресса
        var featsList = hero.GetAllActiveFeats();

        if (featsList != null)
        {
            foreach (var feat in featsList)
            {
                if (feat == null) continue;

                if (feat.category == PropertyCategory.Class)
                {
                    classText.text = feat.featName;
                    continue; // Класс выводим в заголовок, а не в список
                }

                Transform targetContainer = GetContainerForDomain(feat.domain);

                // ИСПРАВЛЕНО 2: Защита от null контейнера (если домен None)
                if (targetContainer == null)
                {
                    targetContainer = militaryContainer; // Страховка: кидаем в "Войну"
                }

                if (featPrefab != null)
                {
                    GameObject featObj = Instantiate(featPrefab, targetContainer);
                    FeatItemUI featUI = featObj.GetComponent<FeatItemUI>();

                    if (featUI != null)
                    {
                        featUI.Setup(feat);
                    }
                    else
                    {
                        Debug.LogError($"❌ На префабе {featPrefab.name} забыли повесить скрипт FeatItemUI!");
                    }
                }
            }
        }

        // ==========================================
        // --- 4. ЭКИПИРОВКА ---
        // ==========================================
        if (equipmentSlots != null)
        {
            foreach (var slot in equipmentSlots)
            {
                if (slot != null)
                {
                    slot.targetHero = hero;
                    slot.RefreshSlotVisual();
                }
            }
        }

        // Включаем/отключаем видимость блоков доменов, чтобы пустые не занимали место
        if (historicalContainer != null) historicalContainer.gameObject.SetActive(historicalContainer.childCount > 0);
        if (militaryContainer != null) militaryContainer.gameObject.SetActive(militaryContainer.childCount > 0);
        if (survivalContainer != null) survivalContainer.gameObject.SetActive(survivalContainer.childCount > 0);
        if (knowledgeContainer != null) knowledgeContainer.gameObject.SetActive(knowledgeContainer.childCount > 0);
        // ==========================================
        // --- 5. КОЛОДА ---
        // ==========================================
        HeroData heroTemplate = hero.Template as HeroData;
        ClearDeck();

        if (heroTemplate != null)
        {
            if (heroTemplate.heroMainCards != null)
            {
                foreach (CardData card in heroTemplate.heroMainCards)
                    SpawnCardIcon(card, isMain: true);
            }

            if (heroTemplate.heroSupportCards != null)
            {
                foreach (CardData card in heroTemplate.heroSupportCards)
                    SpawnCardIcon(card, isMain: false);
            }
        }
    }

    private void SpawnCardIcon(CardData card, bool isMain)
    {
        GameObject cardObj = Instantiate(cardPrefab, deckPreviewContainer);
        CardItemUI cardUI = cardObj.GetComponent<CardItemUI>();

        if (cardUI != null)
        {
            Debug.Log($"✅ Отрисовываю карту: {card.cardName}"); // <-- ЛОГ
            cardUI.Setup(card, isMain);
        }
        else
        {
            Debug.LogError("❌ ОШИБКА: На объекте cardPrefab НЕТ скрипта CardItemUI!");
        }
    }



    private void ClearDeck()
    {
        foreach (Transform child in deckPreviewContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void RefreshStatsTable(UnitProgress hero)
    {
        if (enduranceValueText != null) enduranceValueText.text = FormatStatString(hero, CharacterStatType.Endurance);
        if (strengthValueText != null) strengthValueText.text = FormatStatString(hero, CharacterStatType.Strength);
        if (perceptionValueText != null) perceptionValueText.text = FormatStatString(hero, CharacterStatType.Perception);
        if (agilityValueText != null) agilityValueText.text = FormatStatString(hero, CharacterStatType.Agility);
        if (wisdomValueText != null) wisdomValueText.text = FormatStatString(hero, CharacterStatType.Wisdom);
        if (willValueText != null) willValueText.text = FormatStatString(hero, CharacterStatType.Will);
    }

    private string FormatStatString(UnitProgress hero, CharacterStatType statType)
    {
        if (hero == null || hero.Template == null)
            return "?";

        // Получить базовое значение из шаблона
        int baseValue = GetBaseStatValue(hero, statType);

        // Получить классовый бонус (от ClassFeatData)
        int classBonus = hero.GetClassBonusByStatType(statType);

        // Итоговое значение = базовое + классовый бонус
        int totalValue = baseValue + classBonus;

        // Базовые кубики = базовое значение (это количество d10)
        int baseDiceCount = baseValue;

        // Бонус d10 от предметов (только от экипировки)
        int equipmentDiceBonus = hero.GetEquipmentDiceBonusByStatType(statType);

        // Форматируем: "2 (+1) [(2+1)d10]"
        string classText = classBonus > 0 ? $" (+{classBonus})" : "";
        string diceText = $"({baseDiceCount}+{equipmentDiceBonus})d10";

        return $"{totalValue}{classText}   [{diceText}]";
    }

    /// <summary>
    /// Получить базовое значение характеристики из шаблона
    /// </summary>
    private int GetBaseStatValue(UnitProgress hero, CharacterStatType statType)
    {
        switch (statType)
        {
            case CharacterStatType.Strength: return hero.Template.baseStrength;
            case CharacterStatType.Endurance: return hero.Template.baseEndurance;
            case CharacterStatType.Will: return hero.Template.baseWill;
            case CharacterStatType.Wisdom: return hero.Template.baseWisdom;
            case CharacterStatType.Agility: return hero.Template.baseAgility;
            case CharacterStatType.Perception: return hero.Template.basePerception;
            default: return 1;
        }
    }

    private void ClearAllFeatContainers()
    {
        if (historicalContainer != null) foreach (Transform child in historicalContainer) Destroy(child.gameObject);
        if (militaryContainer != null) foreach (Transform child in militaryContainer) Destroy(child.gameObject);
        if (survivalContainer != null) foreach (Transform child in survivalContainer) Destroy(child.gameObject);
        if (knowledgeContainer != null) foreach (Transform child in knowledgeContainer) Destroy(child.gameObject);
    }

    private Transform GetContainerForDomain(FeatDomain domain)
    {
        switch (domain)
        {
            case FeatDomain.Historical: return historicalContainer;
            case FeatDomain.Military: return militaryContainer;
            case FeatDomain.Survival: return survivalContainer;
            case FeatDomain.Knowledge: return knowledgeContainer;
            default: return militaryContainer; // Возвращаем Военный по умолчанию, чтобы фит не пропадал!
        }
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}