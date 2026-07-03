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
        if (enduranceValueText != null) enduranceValueText.text = FormatStatString(hero.TotalEndurance, hero.Template.baseEndurance);
        if (strengthValueText != null) strengthValueText.text = FormatStatString(hero.TotalStrength, hero.Template.baseStrength);
        if (perceptionValueText != null) perceptionValueText.text = FormatStatString(hero.TotalPerception, hero.Template.basePerception);
        if (agilityValueText != null) agilityValueText.text = FormatStatString(hero.TotalAgility, hero.Template.baseAgility);
        if (wisdomValueText != null) wisdomValueText.text = FormatStatString(hero.TotalWisdom, hero.Template.baseWisdom);
        if (willValueText != null) willValueText.text = FormatStatString(hero.TotalWill, hero.Template.baseWill);
    }

    private string FormatStatString(int totalValue, int baseValue)
    {
        int modifier = Mathf.FloorToInt((totalValue - 10) / 2f);
        int diceCount = Mathf.Max(0, 1 + modifier);

        int equipmentBonus = totalValue - baseValue;
        string bonusText = equipmentBonus > 0 ? $" (+{equipmentBonus})" : "";

        return $"{totalValue}{bonusText}   [{diceCount}d10]";
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