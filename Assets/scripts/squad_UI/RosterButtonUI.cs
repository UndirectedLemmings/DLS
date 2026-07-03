using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RosterButtonUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image iconImage;
    public Button myButton;

    private UnitProgress heroData;
    private FactionData factionData;
    private ExpeditionSetupPanel mainPanel;

    // Настройка для кнопки Героя
    public void SetupHero(UnitProgress hero, ExpeditionSetupPanel panel)
    {
        heroData = hero;
        mainPanel = panel;

        if (nameText != null) nameText.text = hero.heroName;
        if (iconImage != null && hero.runtimePortrait != null) iconImage.sprite = hero.runtimePortrait;

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(() => mainPanel.TryAddHeroToSquad(heroData));
    }

    // Настройка для кнопки Фракции
    public void SetupFaction(FactionData faction, ExpeditionSetupPanel panel)
    {
        factionData = faction;
        mainPanel = panel;

        if (nameText != null) nameText.text = faction.name;
        // Если у фракции есть иконка, можно добавить её сюда:
        // if (iconImage != null && faction.icon != null) iconImage.sprite = faction.icon;

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(() => mainPanel.TryAddFaction(factionData));
    }
}