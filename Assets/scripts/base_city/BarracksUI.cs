using UnityEngine;

public class BarracksUI : MonoBehaviour
{
    public Transform listContainer; // Сюда будут спавниться префабы
    public GameObject heroEntryPrefab; // Твой префаб из Шага 1
    public HeroInfoPanel infoPanel; // Ссылка на ту панель, которую мы уже сделали

    private void Start()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        // 1. Очистить список
        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Берем список героев из CityManager
        // Убедись, что в CityManager есть публичный список allAvailableHeroes 
        // или метод для его получения.
        var allHeroes = CityManager.Instance.allAvailableHeroes;

        if (allHeroes == null || allHeroes.Count == 0)
        {
            Debug.LogWarning("⚠️ [BarracksUI] Список героев в CityManager пуст!");
            return;
        }

        // 3. Создать кнопки для каждого
        foreach (var hero in allHeroes)
        {
            GameObject entryObj = Instantiate(heroEntryPrefab, listContainer);
            HeroListEntryUI entryUI = entryObj.GetComponent<HeroListEntryUI>();

            if (entryUI != null)
            {
                // ✅ ИСПРАВЛЕНО: Передаем ссылку на саму панель, а не метод
                entryUI.Setup(hero, infoPanel);
            }
        }
    }

    private void OnHeroSelected(UnitProgress selectedHero)
    {
        // При клике открываем "Личное дело"
        infoPanel.OpenPanel(selectedHero);
    }
}