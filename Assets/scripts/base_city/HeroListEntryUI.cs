using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroListEntryUI : MonoBehaviour
{
    [Header("UI Элементы")]
    public TextMeshProUGUI nameText;

    public Image portraitImage;
    public Button entryButton;

    private UnitProgress myHero;
    private HeroInfoPanel infoPanel;

    // Метод для инициализации плашки (вызывается из CityManager)
    public void Setup(UnitProgress hero, HeroInfoPanel panel)
    {
        myHero = hero;
        infoPanel = panel;

        // 1. Заполняем текст
        if (nameText != null) nameText.text = hero.heroName;

        // 2. Заполняем портрет (если есть)
        if (portraitImage != null && hero.runtimePortrait != null)
        {
            portraitImage.sprite = hero.runtimePortrait;
        }

        // 3. Настраиваем кнопку
        if (entryButton != null)
        {
            entryButton.onClick.RemoveAllListeners();
            entryButton.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        if (myHero != null)
        {
            // Обращаемся к CityManager, чтобы он правильно закрыл другие окна!
            CityManager.Instance.OpenHeroInfoPanel(myHero);
        }
    }
}